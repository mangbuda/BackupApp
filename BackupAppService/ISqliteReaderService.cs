using System.Data;
using System.Data.SQLite;

namespace BackupAppService
{
    public interface ISqliteReaderService
    {
        int ExecuteQuery(SQLiteCommand sqlite_cmd);
        int ExecuteQuery(string CommandText);
        DataTable ReadAsDataTable(string sqlComand);
    }
}