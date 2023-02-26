using System;
using System.Data;
using System.Data.SQLite;

namespace BackupAppService
{
    public interface ISqliteReaderService
    {
        Tuple<int, long> ExecuteQuery(SQLiteCommand sqlite_cmd);
        Tuple<int, long> ExecuteQuery(string commandText);
        DataTable ReadAsDataTable(string sqlComand);
        DataTable ReadAsDataTable(SQLiteCommand cmd);
    }
}