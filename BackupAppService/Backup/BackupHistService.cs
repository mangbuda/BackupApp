using BackupAppService.Model;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

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
            INSERT INTO BACKUP_HIST 
            (
            BACKUP_TASK_ID ,--SBG UUIDNYA
            SETTING_ID ,
            HOSTNAME_DB_SVR , 
            DB_NAME , 
            BACKUP_SIZE, 
            BACKUP_START_TIME , 
            BACKUP_FINISIH_TIME , 
            BACKUP_FILENAME ,
            BACKUP_STATUS )
            VALUES (
            @BackupHistId,
            @BackupTaskId,
            @SettingId,
            @HostnameDbSvr,
            @DbName,
            @BackupSize,
            @BackupStartTime,
            @BackupFinishTime,
            @BackupFilename,
            @BackupStatus)
            ";

            for (int i = 0; i < backupHists.Count; i++)
            {
                SQLiteCommand sqlite_cmd = new SQLiteCommand();
                sqlite_cmd.CommandText = cmdInsertLog;
                sqlite_cmd.Parameters.AddWithValue("@BackupTaskId", backupHists[i].BackupTaskId);
                sqlite_cmd.Parameters.AddWithValue("@SettingId", backupHists[i].SettingId);
                sqlite_cmd.Parameters.AddWithValue("@HostnameDbSvr", backupHists[i].HostnameDbSvr);
                sqlite_cmd.Parameters.AddWithValue("@DbName", backupHists[i].DbName);
                sqlite_cmd.Parameters.AddWithValue("@BackupSize", backupHists[i].BackupSize);
                sqlite_cmd.Parameters.AddWithValue("@BackupStartTime", backupHists[i].BackupStartTime);
                sqlite_cmd.Parameters.AddWithValue("@BackupFinishTime", backupHists[i].BackupFinishTime);
                sqlite_cmd.Parameters.AddWithValue("@BackupFilename", backupHists[i].BackupFilename);
                sqlite_cmd.Parameters.AddWithValue("@BackupStatus)", backupHists[i].BackupStatus);
                sqliteReaderService.ExecuteQuery(sqlite_cmd);
            }
        }
    }
}
