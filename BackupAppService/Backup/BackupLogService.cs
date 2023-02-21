using BackupAppService.Model;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BackupAppService.BackupService
{
    public class BackupLogService : IBackupLogService
    {
        private readonly ISqliteReaderService sqliteReaderService;

        public BackupLogService(ISqliteReaderService sqliteReaderService)
        {
            this.sqliteReaderService = sqliteReaderService;
        }

        public void CreateLog(BackupLog backupLog)
        {
            string cmdInsertLog = @"
            INSERT INTO BACKUP_LOG
                    (
                    BACKUP_HISTORY_ID,
                    LOG_TYPE,
                    LOG_MESSAGE,
                    LOG_TIME
                    )VALUES(@backupHistoryId,@logType,@logMessage,@logTime)
            ";

            SQLiteCommand sqlite_cmd = new SQLiteCommand();
            sqlite_cmd.CommandText = cmdInsertLog;
            sqlite_cmd.Parameters.AddWithValue("@backupHistoryId", backupLog.BackupHistoryId);
            sqlite_cmd.Parameters.AddWithValue("@logType", backupLog.LogType);
            sqlite_cmd.Parameters.AddWithValue("@logMessage", backupLog.LogMessage);
            sqlite_cmd.Parameters.AddWithValue("@logTime", backupLog.LogTime);

            sqliteReaderService.ExecuteQuery(sqlite_cmd);
        }
    }
}
