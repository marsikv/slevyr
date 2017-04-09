using System;
using System.Data;
using System.Data.SQLite;
using System.Globalization;
using System.IO;
using System.Text;
using NLog;
using Slevyr.DataAccess.Model;

namespace Slevyr.DataAccess.DAO
{
    public static class SqlliteDao
    {
        #region consts

        const string DbFileName = @"slevyr.sqlite";
        const string DefaultDbFolder = @"Sqlite";

        //SQL CREATE TABLE

        const string SqlCreateObservationsTable = @"CREATE TABLE `observations` (
    `id` INTEGER PRIMARY KEY AUTOINCREMENT,
	`obTime`	TIMESTAMP  DEFAULT CURRENT_TIMESTAMP,
	`cmd`	INTEGER  NOT NULL,
	`unitId`	INTEGER NOT NULL,
	`isPrestavka`	bool,
	`cilOk`	INTEGER,
	`pocetOk`	INTEGER,
	`casPoslednihoOk`	REAL,
	`prumCasVyrobyOk`	REAL,
	`cilDefect`	REAL,
	`pocetNg`	INTEGER,
	`casPoslednihoNg`	REAL,
	`prumCasVyrobyNg`	REAL,
	`rozdil`	INTEGER,
	`atualniDefectivita`	REAL,
	`stavLinky`	INTEGER,
    `operator`	TEXT,
    `rezerva`	INTEGER,
    `isFinal`	bool DEFAULT false
);";

        const string SqlCreateStavLinkyTable = @"CREATE TABLE `ProductionLineStatus` 
                (`id`	INTEGER,	`name`	TEXT);";

        const string SqlCreateMetaTable = @"CREATE TABLE `SchemaVersion` 
                (`version`	TEXT ,
                 `createTime`	TIMESTAMP  DEFAULT CURRENT_TIMESTAMP
                );";

        //SQL update

        const string SqlInsertIntoMeta = @"insert into SchemaVersion (version) values ('1.2');";

        const string SqlInsertStatusIntoObservation = @"insert into observations 
                (cmd,unitId,isPrestavka,cilOk,pocetOk,casPoslednihoOk,prumCasVyrobyOk,cilDefect,pocetNg,casPoslednihoNg,prumCasVyrobyNg,rozdil,atualniDefectivita,stavLinky) values ";

        const string SqlInsertLastStatusIntoObservations = @"insert into observations 
                (obTime,cmd,unitId,pocetOk,casPoslednihoOk,pocetNg,casPoslednihoNg,rozdil,atualniDefectivita,stavLinky,isFinal) values ";

        const string SqlUpdateStatusIntoObservations =
                @"update observations set casPoslednihoOk=@casOk, casPoslednihoNg=@casNg where id=@id";

        //SQL select 

        const string SqlExportToCsv = "select datetime(obTime,'localtime') as time,cmd,unitId,isPrestavka,cilOk,pocetOk,casPoslednihoOk," +
                              "prumCasVyrobyOk,cilDefect,pocetNg,casPoslednihoNg,prumCasVyrobyNg," +
                              "rozdil,atualniDefectivita,stavLinky,isFinal  from observations " +
        "where obTime between @timeFrom and @timeTo";


        static readonly string[] SqlExportToCsvFieldNames =
        {
            "Čas","Cmd","UnitId","Přestávka","Cíl OK","Počet OK","Čas posl OK","Prům čas OK","Cíl defektivity","Počet NG",
            "Čas posl NG","Prům čas NG","Rozdíl","Atuální defectivita","Stav linky","Konec směny"
        };

        const string SqlCreateIndex = @"CREATE INDEX `observations_time_idx` ON `observations` (`obTime` ASC)";

        #endregion

        #region fields

        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private static SQLiteConnection _dbConnection;

        private static string _dbFilePath;

        private static void MakeDbFilePath(string dbFolder)
        {
            if (String.IsNullOrEmpty(dbFolder)) dbFolder = DefaultDbFolder;
            _dbFilePath = Path.Combine(dbFolder, DbFileName);
        }

        #endregion

        private static void CreateDatabase(string dbFolder)
        {
            Logger.Info($"*** Create Db - folder:{dbFolder} ***");

            MakeDbFilePath(dbFolder);

            if (!Directory.Exists(dbFolder))
            {
                Directory.CreateDirectory(dbFolder);
            }
            else
            {
                if (File.Exists(_dbFilePath))
                {
                    File.Delete(_dbFilePath + "0");
                    File.Move(_dbFilePath, _dbFilePath + "0");   //uchovam jednu kopii, ale je to asi zbytecne
                }
            }

            try
            {
                SQLiteConnection.CreateFile(_dbFilePath);

                using (var connection = new SQLiteConnection($"Data Source={_dbFilePath};Version=3;"))
                {
                    connection.Open();

                    var command = new SQLiteCommand(SqlCreateObservationsTable, connection);
                    command.ExecuteNonQuery();

                    command = new SQLiteCommand(SqlCreateStavLinkyTable, connection);
                    command.ExecuteNonQuery();

                    command = new SQLiteCommand(SqlCreateMetaTable, connection);
                    command.ExecuteNonQuery();

                    command = new SQLiteCommand(SqlInsertIntoMeta, connection);
                    command.ExecuteNonQuery();

                    command = new SQLiteCommand(SqlCreateIndex, connection);
                    command.ExecuteNonQuery();

                    connection.Close();
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                throw;
            }
            
        }

        public static void OpenConnection(bool checkDbExists, string dbFolder)
        {
            MakeDbFilePath(dbFolder);

            Logger.Info($"data file:{_dbFilePath}");

            if (checkDbExists)
            {
                if (!File.Exists(_dbFilePath)) CreateDatabase(dbFolder);
            }
            _dbConnection = new SQLiteConnection($"Data Source={_dbFilePath};Version=3;");
            _dbConnection.Open();
        }

        public static void CloseConnection()
        {
            Logger.Info("");
            _dbConnection.Close();
        }

        public static long AddUnitState(int addr, UnitStatus u)
        {
            //(cmd,unitId,isPrestavka,cilOk,pocetOk,casPoslednihoOk,prumCasVyrobyOk,cilDefect,pocetNg,casPoslednihoNg,prumCasVyrobyNg,rozdil,atualniDefectivita,stavLinky) 
            string sql = SqlInsertStatusIntoObservation +
                         $"(4,{addr},{(u.Tabule.IsPrestavkaTabule ? 1 : 0)},{u.Tabule.CilKusuTabule},{u.Ok},{u.CasOkStr}," +
                         $"{u.PrumCasVyrobyOkStr},{u.Tabule.CilDefectTabuleStr}," +
                         $"{u.Ng},{u.CasNgStr},{u.PrumCasVyrobyNgStr}," +
                         $"{u.Tabule.RozdilTabule},{u.Tabule.AktualDefectTabuleStr},{(int)u.Tabule.MachineStatus})";


            Logger.Debug(sql);

            try
            {
                SQLiteCommand command = new SQLiteCommand(sql, _dbConnection);
                command.ExecuteNonQuery();

                sql = @"select last_insert_rowid()";
                command = new SQLiteCommand(sql, _dbConnection);

                return (long)command.ExecuteScalar();
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                throw;
            }

        }

        public static void AddUnitKonecSmenyState(byte addr, byte cmd, UnitStatus u, TimeSpan zacatekNoveSmeny, int cilSmeny )
        {
            //potrebuji ziskat konec predchozi smeny, posunume o sekundu vzad
            var konecSmeny = zacatekNoveSmeny.Subtract(new TimeSpan(0, 0, 1));
            DateTime dt = DateTime.Today.AddSeconds(konecSmeny.TotalSeconds);

            //SQLite pozaduje format: yyyy - MM - dd HH: mm: ss
            string timeDateStr = dt.ToString("yyyy-MM-dd HH':'mm':'ss");

            //TODO pouzit LastSmenaResults
            //u.LastSmenaResults

            //spocitam i defektivitu a rozdil pro posledni ok a ng
            var defectivitaStr = (u.FinalOk == 0) ? "null" : Math.Round(((float)u.FinalNg / (float)u.FinalOk) * 100, 2).ToString(CultureInfo.InvariantCulture);
            var rozdil = u.FinalOk - cilSmeny;

            string sql = SqlInsertLastStatusIntoObservations +
                       $"('{timeDateStr}', {cmd},{addr}," +
                       $"{u.FinalOk},null," +  //posledni cas ok neznam
                       $"{u.FinalNg},null," +  //posledni cas ng neznam
                       $"{rozdil},{defectivitaStr}," +  
                       $"{(int)u.Tabule.MachineStatus}," +
                       "'true')";   //isFinal = posledni hodnota pred ukoncenim smeny

            Logger.Debug(sql);
            Logger.Info($"konec smeny adr:{addr}");

            try
            {
                SQLiteCommand command = new SQLiteCommand(sql, _dbConnection);
                command.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                throw;
            }

        }

        public static void UpdateUnitStateCasOk(byte addr, long lastId, UnitStatus u)
        {
            try
            {
                SQLiteCommand command = new SQLiteCommand(SqlUpdateStatusIntoObservations, _dbConnection)
                {
                    CommandType = CommandType.Text
                };

                command.Parameters.Add(new SQLiteParameter("@casOk", u.CasOk));
                command.Parameters.Add(new SQLiteParameter("@casNg", u.CasNg));
                command.Parameters.Add(new SQLiteParameter("@id", lastId));

                //Logger.Info(sql);

                command.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                throw;
            }
        }

        //takto lze vypsat cas v lokalnim formatovani:
        // select time(timeStamp,'localtime'),time(timeStamp,'utc'),date(timeStamp,'localtime'), datetime(timeStamp,'localtime'), unitId from observations order by timeStamp

        public static int ExportToCsv(IntervalExportDef exportDef, char decimalSeparator, MemoryStream memStream)
        {
            string sql = SqlExportToCsv;

            if (!exportDef.ExportAll)
            {
                sql += " and unitId = @unitId";
            }

            using (SQLiteCommand command = new SQLiteCommand(sql, _dbConnection))
            {                
                command.Parameters.Add(new SQLiteParameter("@timeFrom", exportDef.TimeFrom.ToUniversalTime()));
                command.Parameters.Add(new SQLiteParameter("@timeTo", exportDef.TimeTo.ToUniversalTime()));
                if (!exportDef.ExportAll)
                {
                    command.Parameters.Add(new SQLiteParameter("@unitId", exportDef.UnitId));
                }

                DataTable data = new DataTable();
                SQLiteDataAdapter myAdapter = new SQLiteDataAdapter(command);
                //myAdapter.SelectCommand = myCommand;

                myAdapter.Fill(data);

                if (memStream != null)
                {
                    return CreateCsvStream(data, exportDef.FileName, decimalSeparator, memStream);
                }
                else
                {
                    return CreateCsvFile(data, exportDef.FileName, decimalSeparator);
                }
            }

        }
        
        private static int CreateCsvFile(DataTable dt, string strFilePath, char decimalSeparator)
        {
            int cnt = 0;
            using (var sw = new StreamWriter(new FileStream(strFilePath, FileMode.OpenOrCreate, FileAccess.Write), Encoding.GetEncoding("ISO-8859-2")))
            {
                // First we will write the headers.
                int iColCount = dt.Columns.Count;
                for (int i = 0; i < iColCount; i++)
                {
                    //sw.Write(dt.Columns[i]); -- takto by zapsal nazvy sloupcu dle tabulky
                    sw.Write(SqlExportToCsvFieldNames[i]);

                    if (i < iColCount - 1)
                    {
                        sw.Write(";");
                    }
                }
                sw.Write(sw.NewLine);

                // Now write all the rows.

                foreach (DataRow dr in dt.Rows)
                {
                    for (int i = 0; i < iColCount; i++)
                    {
                        if (!Convert.IsDBNull(dr[i]))
                        {
                            if (dr[i] is float || dr[i] is double)
                            {
                                var s = ((double)dr[i]).ToString("0.##", CultureInfo.InvariantCulture);
                                //s = String.Format("{0:0.00}", dr[i]);
                                if (decimalSeparator != '.') s = s.Replace('.', decimalSeparator);
                                sw.Write(s);
                            }
                            else
                            { sw.Write(dr[i].ToString()); }
                        }
                        if (i < iColCount - 1)
                        {
                            sw.Write(";");
                        }
                    }
                    sw.Write(sw.NewLine);
                    cnt++;
                }
                sw.Close();

                return cnt;
            }
        }

        private static int CreateCsvStream(DataTable dt, string strFilePath, char decimalSeparator, MemoryStream memStream)
        {
            int cnt = 0;
            var sw = new StreamWriter(memStream, Encoding.GetEncoding("ISO-8859-2"));
            //using (var sw = new StreamWriter(memStream))
            {
                // First we will write the headers.
                int iColCount = dt.Columns.Count;
                for (int i = 0; i < iColCount; i++)
                {
                    //sw.Write(dt.Columns[i]); -- takto by zapsal nazvy sloupcu dle tabulky
                    sw.Write(SqlExportToCsvFieldNames[i]);

                    if (i < iColCount - 1)
                    {
                        sw.Write(";");
                    }
                }
                sw.Write(sw.NewLine);

                // Now write all the rows.

                foreach (DataRow dr in dt.Rows)
                {
                    for (int i = 0; i < iColCount; i++)
                    {
                        if (!Convert.IsDBNull(dr[i]))
                        {
                            if (dr[i] is float || dr[i] is double)
                            {
                                var s = ((double)dr[i]).ToString("0.##", CultureInfo.InvariantCulture);
                                //s = String.Format("{0:0.00}", dr[i]);
                                if (decimalSeparator != '.') s = s.Replace('.', decimalSeparator);
                                sw.Write(s);
                            }
                            else
                            { sw.Write(dr[i].ToString()); }
                        }
                        if (i < iColCount - 1)
                        {
                            sw.Write(";");
                        }
                    }
                    sw.Write(sw.NewLine);
                    cnt++;
                }

                sw.Flush();
                //sw.Close();

                return cnt;
            }
        }
    }
}
