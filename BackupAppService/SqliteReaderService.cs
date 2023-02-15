using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;
using System.Data.SQLite;

namespace BackupAppService
{
    public class SqliteReaderService
    {
        public string SqlConnection = "Data Source=" + Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "BackupApp", "Resource", "Data.db") + ";";

        public SqliteReaderService()
        {
            SQLiteConnection sqlite_conn = new SQLiteConnection(SqlConnection);
            sqlite_conn.Open();
            sqlite_conn.Close();
        }

        public void ExecuteQuery(string sqlComand)
        {
            SQLiteConnection sqlite_conn = new SQLiteConnection(SqlConnection);
            //try
            //{
            sqlite_conn.Open();
            SQLiteCommand sqlite_cmd;
            sqlite_cmd = sqlite_conn.CreateCommand();
            sqlite_cmd.CommandText = sqlComand;
            sqlite_cmd.ExecuteNonQuery();
            sqlite_conn.Close();
            //}
            //catch (Exception e)
            //{
            //    log.Info(e.Message + " " + e.InnerException + " -> " + sqlComand);
            //}
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
