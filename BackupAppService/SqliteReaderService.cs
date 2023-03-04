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
using System.Security.Cryptography;

namespace BackupAppService
{
    public class SqliteReaderService : ISqliteReaderService
    {
        //public static string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        public static string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
        public static string dbFolderPath = Path.Combine(appDataPath, "BackupApp", "Resource");
        public static string dbFilePath = Path.Combine(dbFolderPath, "Data.db");
        public static string SqlConnection = $"Data Source={dbFilePath};Version=3;";
        public SqliteReaderService()
        {
            SQLiteConnection sqlite_conn = new SQLiteConnection(SqlConnection);
            sqlite_conn.Open();
            sqlite_conn.Close();
        }

        public Tuple<int, long> ExecuteQuery(string commandText)
        {
            long rowID;
            int result = -1;
            using (SQLiteConnection conn = new SQLiteConnection(SqlConnection))
            {
                conn.Open();
                using (var transaction = conn.BeginTransaction())
                {
                    try
                    {
                        using (SQLiteCommand cmd = new SQLiteCommand(conn))
                        {
                            cmd.CommandText = commandText;
                            result = cmd.ExecuteNonQuery();
                            rowID = conn.LastInsertRowId;
                        }
                        transaction.Commit();
                    }
                    catch (SQLiteException ex)
                    {
                        transaction.Rollback();
                        throw new Exception(ex.Message);
                    }
                    finally
                    {
                        transaction.Dispose();
                        conn.Close();
                    }
                }
            }
            return Tuple.Create(result, rowID);
        }

        public Tuple<int, long> ExecuteQuery(SQLiteCommand sqlite_cmd)
        {
            long rowID;
            int result = -1;
            using (SQLiteConnection conn = new SQLiteConnection(SqlConnection))
            {
                conn.Open();
                using (var transaction = conn.BeginTransaction())
                {
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
                            transaction.Commit();
                            rowID = conn.LastInsertRowId;
                        }
                        catch (SQLiteException ex)
                        {
                            transaction.Rollback();
                            throw new Exception(ex.Message);
                        }
                        finally
                        {
                            transaction.Dispose();
                            conn.Close();
                        }
                    }
                }
            }
            return Tuple.Create(result, rowID);
        }

        public DataTable ReadAsDataTable(SQLiteCommand cmd)
        {
            DataTable dt = new DataTable();
            SQLiteDataReader sqlite_datareader;
            SQLiteConnection sqlite_conn = new SQLiteConnection(SqlConnection);
            sqlite_conn.Open();
            //try
            //{
            SQLiteCommand sqlite_cmd;
            sqlite_cmd = sqlite_conn.CreateCommand();
            sqlite_cmd.CommandText = cmd.CommandText;
            if (cmd.Parameters != null)
            {
                for (int i = 0; i < cmd.Parameters.Count; i++)
                {
                    sqlite_cmd.Parameters.Add(cmd.Parameters[i]);
                }
            }

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
            DataTable dt2 = sqlite_datareader.GetSchemaTable();
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
