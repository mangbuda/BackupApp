using BackupAppService.Model;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BackupAppService.BackupService
{
    public class BackupTaskService : IBackupTaskService
    {

        private readonly ISqliteReaderService sqliteReaderService;
        private readonly IBackupLogService backupLogService;

        public BackupTaskService(
            ISqliteReaderService sqliteReaderService,
            IBackupLogService backupLogService)
        {
            this.sqliteReaderService = sqliteReaderService;
            this.backupLogService = backupLogService;
        }

        /// <summary>
        /// method untuk create backup task
        /// </summary>
        public void CreateBackupTask()
        {
            string cmdUpdNextBackupDate = @"
            UPDATE SETTING
            SET LAST_BACKUP_DATE = NEXT_BACKUP_DATE, -- FIX
            NEXT_BACKUP_DATE = strftime('%Y-%m-%dT%H:%M:%fZ', DATETIME((CASE WHEN NEXT_BACKUP_DATE IS NULL THEN BACKUP_TIME ELSE NEXT_BACKUP_DATE END), '+1 days'))
            WHERE EXISTS (
                SELECT S.*
                FROM SETTING S
                    LEFT JOIN BACKUP_TASK BT ON S.SETTING_ID = BT.SETTING_ID AND BT.TASK_STATUS IN ('N','R')
                    WHERE BT.BACKUP_TASK_ID IS NULL
                    AND S.SETTING_ID IN (
                        SELECT MAX(SETTING_ID) AS SETTING_ID FROM SETTING
                        WHERE IS_ACTIVE = 1
                        GROUP BY UUID
                     )
            )";
            Tuple<int, long> result = sqliteReaderService.ExecuteQuery(cmdUpdNextBackupDate);

            if (result.Item1 > 0)
            {
                string cmdInsBackupTask = @"
                INSERT INTO BACKUP_TASK 
                (
                SETTING_ID,
                TASK_STATUS,
                BACKUP_TIME,
                VERSION
                )
                SELECT S.SETTING_ID, 'N', S.NEXT_BACKUP_DATE, S.VERSION  FROM SETTING S
                LEFT JOIN BACKUP_TASK BT ON S.SETTING_ID = BT.SETTING_ID AND BT.TASK_STATUS IN ('N','R')
                WHERE BT.BACKUP_TASK_ID IS NULL
                AND S.SETTING_ID IN (
                    SELECT MAX(SETTING_ID) AS SETTING_ID FROM SETTING
                    WHERE IS_ACTIVE = 1
                    GROUP BY UUID
                 )";
                Tuple<int, long> resultAddBackupTask = sqliteReaderService.ExecuteQuery(cmdInsBackupTask);

                if (resultAddBackupTask.Item1 > 0)
                {
                    //add backup task setting
                    string cmdInsBackupTaskSetting = @"
                    INSERT INTO BACKUP_TASK_SETTING (
                                    BACKUP_TASK_ID,
                                    SETTING_ID,
                                    UUID,
                                    BACKUP_STORE_PERIOD_TYPE,
                                    BACKUP_STORE_NUM_DAYS,
                                    BACKUP_PATH_SVR,
                                    BACKUP_PATH_SVR_LOGIN_USERNAME,
                                    BACKUP_PATH_SVR_LOGIN_PWD,
                                    DB_SVR,
                                    DB_SVR_LOGIN_USERNAME,
                                    DB_SVR_LOGIN_PWD,
                                    SMTP_SVR,
                                    SMTP_PORT,
                                    IS_SSL_FOR_SMTP,
                                    SMTP_LOGIN_USERNAME,
                                    SMTP_LOGIN_PWD,
                                    BACKUP_TIME,
                                    SMTP_NOTIF_EMAIL_FROM,
                                    SMTP_NOTIF_EMAIL_TO,
                                    LAST_BACKUP_DATE,
                                    NEXT_BACKUP_DATE,
                                    BACKUP_STATUS,
                                    IS_ACTIVE,
                                    VERSION,
                                    DB_TYPE,
                                    IS_DELETE,
                                    DTM_CRT,
                                    DTM_UPD,
                                    USR_CRT,
                                    USR_UPD,
                                    IS_BACKUP_INPROCESS
                                )
                                SELECT 
                                   BT.BACKUP_TASK_ID,
                                   S.SETTING_ID,
                                   S.UUID,
                                   S.BACKUP_STORE_PERIOD_TYPE,
                                   S.BACKUP_STORE_NUM_DAYS,
                                   S.BACKUP_PATH_SVR,
                                   S.BACKUP_PATH_SVR_LOGIN_USERNAME,
                                   S.BACKUP_PATH_SVR_LOGIN_PWD,
                                   S.DB_SVR,
                                   S.DB_SVR_LOGIN_USERNAME,
                                   S.DB_SVR_LOGIN_PWD,
                                   S.SMTP_SVR,
                                   S.SMTP_PORT,
                                   S.IS_SSL_FOR_SMTP,
                                   S.SMTP_LOGIN_USERNAME,
                                   S.SMTP_LOGIN_PWD,
                                   S.BACKUP_TIME,
                                   S.SMTP_NOTIF_EMAIL_FROM,
                                   S.SMTP_NOTIF_EMAIL_TO,
                                   S.LAST_BACKUP_DATE,
                                   S.NEXT_BACKUP_DATE,
                                   S.BACKUP_STATUS,
                                   S.IS_ACTIVE,
                                   S.VERSION,
                                   S.DB_TYPE,
                                   S.IS_DELETE,
                                   S.DTM_CRT,
                                   S.DTM_UPD,
                                   S.USR_CRT,
                                   S.USR_UPD,
                                   S.IS_BACKUP_INPROCESS
                              FROM SETTING S
                              JOIN BACKUP_TASK BT ON BT.SETTING_ID = S.SETTING_ID
                              WHERE BT.BACKUP_TASK_ID = @backupTaskId
                    ";
                    SQLiteCommand sqlite_cmd = new SQLiteCommand();
                    sqlite_cmd.CommandText = cmdInsBackupTaskSetting;
                    sqlite_cmd.Parameters.AddWithValue("@backupTaskId", resultAddBackupTask.Item2);
                    sqliteReaderService.ExecuteQuery(sqlite_cmd);
                }
            }
        }

        /// <summary>
        /// method untuk get backup task kemudian untuk di proses
        /// </summary>
        public DataTable GetBackupTask()
        {
            List<string> cmds = new List<string>
            {
            @"SELECT 1 FROM BACKUP_TASK BT WHERE TASK_STATUS IN('R')",
            @"DROP TABLE IF EXISTS TEMP_BACKUP_TASK",
            @"CREATE TABLE TEMP_BACKUP_TASK AS
            SELECT BACKUP_TASK_ID AS backupTaskId
            ,TASK_STATUS AS TaskStatus
            ,BACKUP_STORE_PERIOD_TYPE AS BackupStorePriodType
            ,BACKUP_STORE_NUM_DAYS AS BackupStoreNumDays
            ,BACKUP_PATH_SVR AS BackupPathSvr
            ,BACKUP_PATH_SVR_LOGIN_USERNAME AS BackupPathSvrLoginUsername
            ,BACKUP_PATH_SVR_LOGIN_PWD AS BackupPathSvrLoginPwd
            ,DB_SVR AS DbSvr
            ,DB_SVR_LOGIN_USERNAME AS DbSvrLoginUsername
            ,DB_SVR_LOGIN_PWD AS DbSvrLoginPwd
            ,SMTP_SVR AS SmtpSvr
            ,SMTP_PORT AS SmtpPort
            ,IS_SSL_FOR_SMTP AS IsSslForSmtp
            ,SMTP_LOGIN_USERNAME AS SmtpLoginUsername
            ,SMTP_LOGIN_PWD AS SmtpLoginPwd
            ,BT.BACKUP_TIME AS BackupTime
            ,SMTP_NOTIF_EMAIL_FROM AS SmtpNotifEmailFrom
            ,SMTP_NOTIF_EMAIL_TO AS SmtpNotifEmailTo
            ,IS_ACTIVE AS IsActive
            ,LAST_BACKUP_DATE AS LastBackupDate
            ,NEXT_BACKUP_DATE AS NextBackupDate
            ,BACKUP_STATUS AS BackupStatus
            ,S.VERSION AS Version
            ,S.SETTING_ID AS SettingId
            FROM BACKUP_TASK BT
            JOIN BACKUP_TASK_SETTING S ON BT.SETTING_ID = S.SETTING_ID
            WHERE TASK_STATUS IN('N')
            AND DATETIME(BT.BACKUP_TIME) >= DATETIME('now')
            LIMIT 1
            ",//untuk keperluan debugging dibuat >=, harusnya <=

            @"UPDATE BACKUP_TASK
            SET TASK_STATUS = 'R'
            WHERE BACKUP_TASK_ID IN
            (
                SELECT BACKUP_TASK_ID FROM TEMP_BACKUP_TASK
            )",
            @"SELECT * FROM TEMP_BACKUP_TASK"
            };

            DataTable dtInp = sqliteReaderService.ReadAsDataTable(cmds[0]);
            DataTable dtRes = null;
            if (dtInp.Rows.Count == 0)
            {
                sqliteReaderService.ExecuteQuery(cmds[1]);
                sqliteReaderService.ExecuteQuery(cmds[2]);
                sqliteReaderService.ExecuteQuery(cmds[3]);
                dtRes = sqliteReaderService.ReadAsDataTable(cmds[4]);
            }
            return dtRes;
        }

        /// <summary>
        /// method untuk update backup task
        /// </summary>
        /// <param name="backupTask"></param>
        public void UpdateBackupTask(BackupTask backupTask)
        {
            string cmdUpdBackupTask =
                @"UPDATE BACKUP_TASK 
                SET TASK_END_TIME = @taskEndTime, TASK_STATUS = @taskStatus 
                WHERE BACKUP_TASK_ID = @backupTaskId";
            SQLiteCommand sqlite_cmd = new SQLiteCommand();
            sqlite_cmd.CommandText = cmdUpdBackupTask;
            sqlite_cmd.Parameters.AddWithValue("@taskEndTime", backupTask.TaskEndTime);
            sqlite_cmd.Parameters.AddWithValue("@taskStatus", backupTask.TaskStatus);
            sqlite_cmd.Parameters.AddWithValue("@backupTaskId", backupTask.BackupTaskId);
            sqliteReaderService.ExecuteQuery(sqlite_cmd);
        }
    }
}
