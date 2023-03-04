using BackupAppService.Model;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Security.Cryptography;

namespace BackupAppService.BackupService
{
    public class BackupHistService : IBackupHistService
    {
        private readonly ISqliteReaderService sqliteReaderService;

        public BackupHistService(ISqliteReaderService sqliteReaderService)
        {
            this.sqliteReaderService = sqliteReaderService;
        }

        public void CreateBackupHist(List<BackupHist> backupHists)
        {
            string cmdInsertLog = @"
            INSERT INTO BACKUP_HIST (
                           BACKUP_TASK_ID,
                           SETTING_ID,
                           HOSTNAME_DB_SVR,
                           DB_NAME,
                           BACKUP_SIZE,
                           BACKUP_START_TIME,
                           BACKUP_FINISIH_TIME,
                           BACKUP_FILENAME,
                           BACKUP_STATUS
                       )
                       VALUES (
                           @BackupTaskId,
						@SettingId,
						@HostnameDbSvr,
						@DbName,
						@BackupSize,
						@BackupStartTime,
						@BackupFinishTime,
						@BackupFilename,
						@BackupStatus
                       )
            ";

            for (int i = 0; i < backupHists.Count; i++)
            {
                SQLiteCommand sqlite_cmd = new SQLiteCommand();
                sqlite_cmd.CommandText = cmdInsertLog;
                sqlite_cmd.Parameters.AddWithValue("@backupTaskId", backupHists[i].BackupTaskId);
                sqlite_cmd.Parameters.AddWithValue("@settingId", backupHists[i].SettingId);
                sqlite_cmd.Parameters.AddWithValue("@hostnameDbSvr", backupHists[i].HostnameDbSvr);
                sqlite_cmd.Parameters.AddWithValue("@dbName", backupHists[i].DbName);
                sqlite_cmd.Parameters.AddWithValue("@backupSize", backupHists[i].BackupSize);
                sqlite_cmd.Parameters.AddWithValue("@backupStartTime", backupHists[i].BackupStartTime.ToString("yyyy-MM-dd HH:mm:ss"));
                sqlite_cmd.Parameters.AddWithValue("@backupFinishTime", backupHists[i].BackupFinishTime.ToString("yyyy-MM-dd HH:mm:ss"));
                sqlite_cmd.Parameters.AddWithValue("@backupFilename", backupHists[i].BackupFilename);
                sqlite_cmd.Parameters.AddWithValue("@backupStatus", backupHists[i].BackupStatus);
                sqliteReaderService.ExecuteQuery(sqlite_cmd);
            }
        }

        public DataTable GetSummaryBackupHistByBackupHistId(long backupTaskId)
        {
            string cmdGetSummBackupHist = @"
SELECT 
    BH.BACKUP_TASK_ID AS BackupTaskId,
    MIN(BH.BACKUP_START_TIME) AS BackupStartTime,
    MAX(BH.BACKUP_FINISIH_TIME) AS BackupFinishTime,
    SUM(BH.BACKUP_SIZE) AS BackupSize,
    MAX(BH.BACKUP_STATUS) AS BackupStatus,
    MAX(BH.HOSTNAME_DB_SVR) AS HostnameDbSvr
FROM BACKUP_HIST BH
JOIN SETTING S ON BH.SETTING_ID = S.SETTING_ID
                WHERE BH.BACKUP_TASK_ID = @backupTaskId 
GROUP BY BH.BACKUP_TASK_ID

            "
            ;

            DataTable dtRes = null;
            SQLiteCommand sqlite_cmd = new SQLiteCommand();
            sqlite_cmd.CommandText = cmdGetSummBackupHist;
            sqlite_cmd.Parameters.AddWithValue("@backupTaskId", backupTaskId);

            dtRes = sqliteReaderService.ReadAsDataTable(sqlite_cmd);
            return dtRes;
        }
    }
}
