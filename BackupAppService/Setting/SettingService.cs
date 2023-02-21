using BackupAppService.Model;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BackupAppService.Setting_
{
    public class SettingService : ISettingService
    {
        private readonly ISqliteReaderService sqliteReaderService;

        public SettingService(ISqliteReaderService sqliteReaderService)
        {
            this.sqliteReaderService = sqliteReaderService;
        }

        public void UpdateSetting(Setting setting)
        {
            string cmdUpdBackupTask =
                @"UPDATE SETTING 
                SET BACKUP_STATUS = @backupStatus 
                WHERE SETTING_ID = @settingId";
            SQLiteCommand sqlite_cmd = new SQLiteCommand();
            sqlite_cmd.CommandText = cmdUpdBackupTask;
            sqlite_cmd.Parameters.AddWithValue("@backupStatus", setting.BackupStatus);
            sqlite_cmd.Parameters.AddWithValue("@settingId", setting.SettingId);
            sqliteReaderService.ExecuteQuery(sqlite_cmd);
        }
    }
}
