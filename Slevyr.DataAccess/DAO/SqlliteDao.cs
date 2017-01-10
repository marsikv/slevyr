﻿using System;
using System.Data;
using System.Data.SQLite;
using System.IO;
using NLog;
using Slevyr.DataAccess.Model;
using Slevyr.DataAccess.Services;

namespace Slevyr.DataAccess.DAO
{
    //TODO predelat na EF6 - code first

    public static class SqlliteDao
    {
        #region consts

        const string DbFileName = @"slevyr.sqlite";
        const string DefaultDbFolder = @"Sqlite";
        const string SqlCreateUnitstatusTable = @"CREATE TABLE `observations` (
    `id` INTEGER PRIMARY KEY AUTOINCREMENT,
	`obTime`	TIMESTAMP  DEFAULT CURRENT_TIMESTAMP,
	`cmd`	INTEGER  NOT NULL,
	`unitId`	INTEGER NOT NULL,
	`isPrestavka`	bool,
	`cilOk`	INTEGER,
	`pocetOk`	INTEGER,
	`casPoslednihoOk`	INTEGER,
	`prumCasVyrobyOk`	REAL,
	`cilNg`	INTEGER,
	`pocetNg`	INTEGER,
	`casPoslednihoNg`	INTEGER,
	`prumCasVyrobyNg`	REAL,
	`rozdil`	INTEGER,
	`atualniDefectivita`	REAL,
	`stavLinky`	INTEGER
);";

        const string SqlInsertStatusIntoObservation = @"insert into observations 
(cmd,unitId,isPrestavka,cilOk,pocetOk,casPoslednihoOk,prumCasVyrobyOk,cilNg,pocetNg,casPoslednihoNg,prumCasVyrobyNg,rozdil,atualniDefectivita,stavLinky) values ";

        const string SqlInsertLastStatusIntoObservations = @"insert into observations 
(cmd,unitId,pocetOk,casPoslednihoOk,pocetNg,casPoslednihoNg,stavLinky) values ";

        const string SqlUpdateStatusIntoObservations =
            @"update observations set casPoslednihoOk=@casOk, casPoslednihoNg=@casNg where id=@id";

        const string SqlCreateStavLinkyTable = @"CREATE TABLE `ProductionLineStatus` 
(`id`	INTEGER,	`name`	TEXT);";

        const string SqlExportToCsv = "select datetime(obTime,'localtime') as time,cmd,unitId,isPrestavka,cilOk,pocetOk,casPoslednihoOk,prumCasVyrobyOk,cilNg,pocetNg,casPoslednihoNg,prumCasVyrobyNg,rozdil,atualniDefectivita,stavLinky from observations " +
                "where obTime between @timeFrom and @timeTo";

        static readonly string[] SqlExportToCsvFieldNames =
        {
            "Čas","Cmd","UnitId","Přestávka","Cíl OK","Počet OK","Čas posl OK","Prům čas OK","Cíl NG","Počet NG",
            "Čas posl NG","Prům čas NG","Rozdíl","Atuální defectivita","Stav linky"
        };

        const string SqlCreateIndex = @"CREATE INDEX `observations_time_idx` ON `observations` (`obTime` ASC)";

        #endregion

        #region fields

        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private static SQLiteConnection _dbConnection;

        private static string _dbFilePath;

        private static void MakeDbFilePath(string dbFolder)
        {
            //if (Path.IsPathRooted(dbFolder))
            if (String.IsNullOrEmpty(dbFolder)) dbFolder = DefaultDbFolder;
            _dbFilePath = Path.Combine(dbFolder, DbFileName);
        }

        // => Path.Combine(dbFolder, DbFileName);
 
        #endregion

        private static void CreateDatabase(string dbFolder)
        {
            Logger.Info($"Db folder:{dbFolder}");

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

            SQLiteConnection.CreateFile(_dbFilePath);

            using (var connection = new SQLiteConnection($"Data Source={_dbFilePath};Version=3;"))
            {
                connection.Open();

                var command = new SQLiteCommand(SqlCreateUnitstatusTable, connection);
                command.ExecuteNonQuery();

                command = new SQLiteCommand(SqlCreateStavLinkyTable, connection);
                command.ExecuteNonQuery();

                command = new SQLiteCommand(SqlCreateIndex, connection);
                command.ExecuteNonQuery();

                connection.Close();
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
            //(cmd,unitId,isPrestavka,cilOk,pocetOk,casPoslednihoOk,prumCasVyrobyOk,cilNg,pocetNg,casPoslednihoNg,prumCasVyrobyNg,rozdil,atualniDefectivita,stavLinky) values ";
            string sql = SqlInsertStatusIntoObservation +
                         $"(4,{addr},{(u.IsPrestavkaTabule ? 1 : 0)},{u.Tabule.CilKusuTabule},{u.Ok},{u.CasOkStr}," +
                         $"{u.PrumCasVyrobyOkStr},{u.Tabule.CilDefectTabuleStr}," +
                         $"{u.Ng},{u.CasNgStr},{u.PrumCasVyrobyNgStr}," +
                         $"{u.Tabule.RozdilTabule},{u.Tabule.AktualDefectTabuleStr},{(int)u.MachineStatus})";


            Logger.Info(sql);

            SQLiteCommand command = new SQLiteCommand(sql, _dbConnection);
            command.ExecuteNonQuery();

            sql = @"select last_insert_rowid()";
            command = new SQLiteCommand(sql, _dbConnection);
            return (long) command.ExecuteScalar();
        }

        public static void AddUnitKonecSmenyState(byte addr, byte cmd, UnitStatus u)
        {            
            string sql = SqlInsertLastStatusIntoObservations +
                       $"({cmd},{addr}," +
                       $"{u.LastOk},null," +  //posledni cas ok neznam
                       $"{u.LastNg},null," +  //posledni cas ng neznam
                       $"{(int)u.MachineStatus})";

            Logger.Info(sql);

            SQLiteCommand command = new SQLiteCommand(sql, _dbConnection);
            command.ExecuteNonQuery();

        }

        public static void UpdateUnitStateCasOk(byte addr, long lastId, UnitStatus u)
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

        //takto lze vypsat cas v lokalnim formatovani:
        // select time(timeStamp,'localtime'),time(timeStamp,'utc'),date(timeStamp,'localtime'), datetime(timeStamp,'localtime'), unitId from observations order by timeStamp

        public static int ExportToCsv(IntervalExport export)
        {
            string sql = SqlExportToCsv;

            if (!export.ExportAll)
            {
                sql += " and unitId = @unitId";
            }

            using (SQLiteCommand command = new SQLiteCommand(sql, _dbConnection))
            {                
                command.Parameters.Add(new SQLiteParameter("@timeFrom", export.TimeFrom.ToUniversalTime()));
                command.Parameters.Add(new SQLiteParameter("@timeTo", export.TimeTo.ToUniversalTime()));
                if (!export.ExportAll)
                {
                    command.Parameters.Add(new SQLiteParameter("@unitId", export.UnitId));
                }

                DataTable data = new DataTable();
                SQLiteDataAdapter myAdapter = new SQLiteDataAdapter(command);
                //myAdapter.SelectCommand = myCommand;

                myAdapter.Fill(data);

                return CreateCsvFile(data, export.FileName);
            }

        }

        private static int CreateCsvFile(DataTable dt, string strFilePath)
        {
            int cnt = 0;
            StreamWriter sw = new StreamWriter(strFilePath, false);

            // First we will write the headers.
            int iColCount = dt.Columns.Count;
            for (int i = 0; i < iColCount; i++)
            {
                //sw.Write(dt.Columns[i]);
                sw.Write(SqlExportToCsvFieldNames[i]);
                //sw.Write(Helper.ConvertUtfToLatin2(SqlExportToCsvFieldNames[i]));

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
                        sw.Write(dr[i].ToString());
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
}
