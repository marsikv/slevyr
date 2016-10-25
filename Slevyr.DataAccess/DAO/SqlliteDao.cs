using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Slevyr.DataAccess.Model;

namespace Slevyr.DataAccess.DAO
{
    //TODO predelat na EF6 - code first

    public static class SqlliteDao
    {
        #region consts

        const string DbFileName = @"slevyr.sqlite";
        const string DbFolder = @"data";
        const string SqlCreateUnitstatusTable = @"CREATE TABLE `observations` (
	`timeStamp`	INTEGER  NOT NULL,
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

        const string SqlInsertIntoObservations = @"insert into observations 
(timeStamp,cmd,unitId,isPrestavka,cilOk,pocetOk,casPoslednihoOk,prumCasVyrobyOk,cilNg,pocetNg,casPoslednihoNg,prumCasVyrobyNg,rozdil,atualniDefectivita,stavLinky) values ";

        const string SqlCreateStavLinkyTable = @"CREATE TABLE `ProductionLineStatus` 
(`id`	INTEGER,	`name`	TEXT);";

        const string SqlCreateIndex = @"CREATE INDEX `observations_time_idx` ON `observations` (`timeStamp` ASC)";

        #endregion

        #region fields

        private static SQLiteConnection _dbConnection;

        private static string DbFilePath => Path.Combine(DbFolder, DbFileName);
 
        #endregion

        public static void CreateDatabase()
        {
            if (!Directory.Exists(DbFolder))
            {
                Directory.CreateDirectory(DbFolder);
            }
            else
            {
                if (File.Exists(DbFilePath))
                {
                    File.Delete(DbFilePath + "0");
                    File.Move(DbFilePath, DbFilePath + "0");   //uchovam jednu kopii, ale je to asi zbytecne
                }
            }

            SQLiteConnection.CreateFile(DbFilePath);

            using (var connection = new SQLiteConnection($"Data Source={DbFilePath};Version=3;"))
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

        public static void OpenConnection(bool checkDbExists)
        {
            if (checkDbExists)
            {
                if (!File.Exists(DbFilePath)) CreateDatabase();
            }
            _dbConnection = new SQLiteConnection($"Data Source={DbFilePath};Version=3;");
            _dbConnection.Open();
        }

        public static void CloseConnection()
        {
            _dbConnection.Close();
        }

        public static void AddUnitState(int addr, UnitStatus u)
        {
            //(timeStamp,cmd,unitId,isPrestavka, cilOk,pocetOk,casPoslednihoOk,prumCasVyrobyOk,cilNg,pocetNg,casPoslednihoNg,prumCasVyrobyNg,rozdil,atualniDefectivita,stavLinky) values ";

            string sql = SqlInsertIntoObservations +
                         $"(CURRENT_TIMESTAMP,4,{addr},{(u.IsPrestavkaTabule ? 1 : 0)},{u.CilKusuTabule},{u.Ok},{u.CasOk}," +
                         $"{u.PrumCasVyrobyOkStr},{u.CilDefectTabule},{u.Ng},{u.CasNg},{u.PrumCasVyrobyNgStr}," +
                         $"{u.RozdilTabule},{u.AktualDefectTabuleStr},{(int)u.MachineStatus})";

            SQLiteCommand command = new SQLiteCommand(sql, _dbConnection);
            command.ExecuteNonQuery();
        }

        //takto lze vypsat cas v lokalnim formatovani:
        // select time(timeStamp,'localtime'),time(timeStamp,'utc'),date(timeStamp,'localtime'), datetime(timeStamp,'localtime'), unitId from observations order by timeStamp

    }
}
