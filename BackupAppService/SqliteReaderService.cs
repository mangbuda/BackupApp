using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;
using System.Data.SQLite;
using BackupAppService.BackupService;
using BackupAppService.Model;

namespace BackupAppService
{
    public class SqliteReaderService : ISqliteReaderService
    {
        public string SqlConnection = "Data Source=" + Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "BackupApp", "Resource", "Data.db") + ";";

        public SqliteReaderService()
        {
            SQLiteConnection sqlite_conn = new SQLiteConnection(SqlConnection);
            sqlite_conn.Open();
            sqlite_conn.Close();
        }

        public int ExecuteQuery(string CommandText)
        {
            int result = -1;
            using (SQLiteConnection conn = new SQLiteConnection(SqlConnection))
            {
                conn.Open();
                using (SQLiteCommand cmd = new SQLiteCommand(conn))
                {
                    try
                    {
                        cmd.CommandText = CommandText;
                        result = cmd.ExecuteNonQuery();
                    }
                    catch (SQLiteException ex)
                    {
                        throw new Exception(ex.Message);
                    }
                }
                conn.Close();
            }
            return result;
        }

        public int ExecuteQuery(SQLiteCommand sqlite_cmd)
        {
            int result = -1;
            using (SQLiteConnection conn = new SQLiteConnection(SqlConnection))
            {
                conn.Open();
                using (SQLiteCommand cmd = new SQLiteCommand(conn))
                {
                    try
                    {
                        cmd.CommandText = sqlite_cmd.CommandText;
                        if (sqlite_cmd.Parameters != null)
                        {
                            for (int i = 0; i < sqlite_cmd.Parameters.Count; i++)
                            {
                                cmd.Parameters.Add(sqlite_cmd.Parameters[i]);
                            }
                        }
                        result = cmd.ExecuteNonQuery();
                    }
                    catch (SQLiteException ex)
                    {
                        throw new Exception(ex.Message);
                    }
                }
                conn.Close();
            }
            return result;
        }

        public DataTable ReadAsDataTable(string sqlComand)
        {
            DataTable dt = new DataTable();
            SQLiteDataReader sqlite_datareader;
            SQLiteConnection sqlite_conn = new SQLiteConnection(SqlConnection);
            sqlite_conn.Open();
            //try
            //{
            SQLiteCommand sqlite_cmd;
            sqlite_cmd = sqlite_conn.CreateCommand();
            sqlite_cmd.CommandText = sqlComand;
            sqlite_datareader = sqlite_cmd.ExecuteReader();
            dt.Load(sqlite_datareader);
            sqlite_conn.Close();
            //}
            //catch (Exception e)
            //{
            //    log.Info(e.Message + " " + e.InnerException + " -> " + sqlComand);
            //}
            return dt;

        }

    }
}
