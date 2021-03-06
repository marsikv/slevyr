﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NLog;
using Slevyr.DataAccess.Model;

namespace Slevyr.DataAccess.DAO
{
    //TODO predelat na EF6 - code first

    public static class SqlliteDao
    {
        #region consts

        const string DbFileName = @"slevyr.sqlite";
        const string DbFolder = @"Sqlite";
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

        const string SqlInsertIntoObservations = @"insert into observations 
(cmd,unitId,isPrestavka,cilOk,pocetOk,casPoslednihoOk,prumCasVyrobyOk,cilNg,pocetNg,casPoslednihoNg,prumCasVyrobyNg,rozdil,atualniDefectivita,stavLinky) values ";

        const string SqlCreateStavLinkyTable = @"CREATE TABLE `ProductionLineStatus` 
(`id`	INTEGER,	`name`	TEXT);";

        const string SqlCreateIndex = @"CREATE INDEX `observations_time_idx` ON `observations` (`obTime` ASC)";

        #endregion

        #region fields

        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private static SQLiteConnection _dbConnection;

        private static string DbFilePath => Path.Combine(DbFolder, DbFileName);
 
        #endregion

        public static void CreateDatabase()
        {
            Logger.Info($"Db folder:{DbFolder}");

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
            Logger.Info("");
            if (checkDbExists)
            {
                if (!File.Exists(DbFilePath)) CreateDatabase();
            }
            _dbConnection = new SQLiteConnection($"Data Source={DbFilePath};Version=3;");
            _dbConnection.Open();
        }

        public static void CloseConnection()
        {
            Logger.Info("");
            _dbConnection.Close();
        }

        public static void AddUnitState(int addr, UnitStatus u)
        {
            //(cmd,unitId,isPrestavka,cilOk,pocetOk,casPoslednihoOk,prumCasVyrobyOk,cilNg,pocetNg,casPoslednihoNg,prumCasVyrobyNg,rozdil,atualniDefectivita,stavLinky) values ";
            string sql = SqlInsertIntoObservations +
                         $"(4,{addr},{(u.IsPrestavkaTabule ? 1 : 0)},{u.CilKusuTabule},{u.Ok},{u.CasOkStr}," +
                         $"{u.PrumCasVyrobyOkStr},{u.CilDefectTabuleStr}," +
                         $"{u.Ng},{u.CasNgStr},{u.PrumCasVyrobyNgStr}," +
                         $"{u.RozdilTabule},{u.AktualDefectTabuleStr},{(int)u.MachineStatus})";


            Logger.Info(sql);

            SQLiteCommand command = new SQLiteCommand(sql, _dbConnection);
            command.ExecuteNonQuery();
        }

        //takto lze vypsat cas v lokalnim formatovani:
        // select time(timeStamp,'localtime'),time(timeStamp,'utc'),date(timeStamp,'localtime'), datetime(timeStamp,'localtime'), unitId from observations order by timeStamp

        public static int ExportToCsv(IntervalExport export)
        {
            string sql =
                "select datetime(obTime,'localtime') as time,cmd,unitId,isPrestavka,cilOk,pocetOk,casPoslednihoOk,prumCasVyrobyOk,cilNg,pocetNg,casPoslednihoNg,prumCasVyrobyNg,rozdil,atualniDefectivita,stavLinky from observations " +
                "where obTime between @timeFrom and @timeTo";
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
                sw.Write(dt.Columns[i]);
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
