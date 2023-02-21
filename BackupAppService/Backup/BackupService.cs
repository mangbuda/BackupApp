using BackupAppService.Model;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Runtime.InteropServices;
using System.Security.Policy;

namespace BackupAppService.BackupService
{
    public class BackupService : IBackupService
    {
        private readonly IBackupTaskService backupTaskService;
        private readonly IBackupHistService backupHistService;
        private readonly IBackupLogService backupLogService;

        public BackupService(
            IBackupTaskService backupTaskService, 
            IBackupHistService backupHistService, 
            IBackupLogService backupLogService)
        {
            this.backupTaskService = backupTaskService;
            this.backupHistService = backupHistService;
            this.backupLogService = backupLogService;
        }

        public void Backup()
        {
            DataTable dtTask = backupTaskService.GetBackupTask();
            if (dtTask != null)
            {
                foreach (DataRow row in dtTask.Rows)
                {
                    string DbSvr = row["DbSvr"].ToString();
                    string DbSvrLoginUsername = row["DbSvrLoginUsername"].ToString();
                    string DbSvrLoginPwd = row["DbSvrLoginPwd"].ToString();
                    string BackupPathSvr = row["BackupPathSvr"].ToString();
                    string BackupPathSvrLoginUsername = row["BackupPathSvrLoginUsername"].ToString();
                    string BackupPathSvrLoginPwd = row["BackupPathSvrLoginPwd"].ToString();
                    long BackupTaskId = Convert.ToInt64(row["BackupTaskId"].ToString());
                    long SettingId = Convert.ToInt64(row["SettingId"].ToString());

                    try
                    {
                        string connectionString = "Data Source=" + DbSvr + ";User ID=" + DbSvrLoginUsername + ";Password=" + DbSvrLoginPwd + "";

                        bool driveAdded = false;
                        string bPath = "x://";

                        if (string.IsNullOrEmpty(BackupPathSvrLoginUsername)
                            && string.IsNullOrEmpty(BackupPathSvrLoginPwd))
                        {
                            AddMappedDrive(
                                connectionString,
                                BackupPathSvr,
                                BackupPathSvrLoginUsername,
                                BackupPathSvrLoginPwd);
                        }

                        if (!driveAdded && !string.IsNullOrEmpty(BackupPathSvr))
                        {
                            bPath = BackupPathSvr;
                        }
                        else
                        {
                            bPath = "c://";
                        }

                        DataTable dtRes = BackupSql(connectionString, bPath);
                        List<BackupHist> backupHists = new List<BackupHist>();

                        for (int j = 0; j < dtRes.Rows.Count; j++)
                        {
                            BackupHist backupHist = new BackupHist()
                            {
                                BackupTaskId = BackupTaskId,
                                SettingId = SettingId,
                                HostnameDbSvr = dtRes.Rows[j]["HostnameDbServer"].ToString(),
                                DbName = dtRes.Rows[j]["HostnameDbServer"].ToString(),
                                BackupSize = float.Parse(dtRes.Rows[j]["HostnameDbServer"].ToString()),
                                BackupStartTime = DateTime.Parse(dtRes.Rows[j]["BackupStartDate"].ToString()),
                                BackupFinishTime = DateTime.Parse(dtRes.Rows[j]["BackupFinishDate"].ToString()),
                                BackupFilename = dtRes.Rows[j]["PhysicalDeviceName"].ToString(),
                                BackupStatus = dtRes.Rows[j]["BackupStat"].ToString(),
                            };
                            backupHists.Add(backupHist);
                        }
                        backupHistService.CreateBackupHist(backupHists);

                        BackupTask backupTask = new BackupTask()
                        {
                            BackupTaskId = BackupTaskId,
                            TaskEndTime = DateTime.Now,
                            TaskStatus = "S"
                        };

                        //send email

                        backupTaskService.UpdateBackupTask(backupTask);
                    }
                    catch (Exception ex)
                    {

                        BackupTask backupTask = new BackupTask()
                        {
                            BackupTaskId = BackupTaskId,
                            TaskEndTime = DateTime.Now,
                            TaskStatus = "E"
                        };

                        backupTaskService.UpdateBackupTask(backupTask);

                        string Extype = ex.GetType().ToString();
                        string Errormsg = ex.Message + "\n" + ex.StackTrace;

                        BackupLog backupLog = new BackupLog()
                        {
                            BackupHistoryId = BackupTaskId,
                            LogType = "Error",
                            LogMessage = Errormsg,
                            LogTime = DateTime.Now

                        };

                        backupLogService.CreateLog(backupLog);
                    }
                }
            }
        }

        #region private
        private DataTable BackupSql(string connString, string backupPath)
        {
            string cmdCountDbTobeProcess = @"
                SELECT
                    sd.name
                FROM sys.databases sd
                WHERE sd.state_desc != 'OFFLINE'
                    AND sd.name NOT IN ('master', 'model', 'msdb', 'tempdb')
            ";
            DataTable dtCompare = null;
            using (SqlConnection sqlCon = new SqlConnection(connString))
            {
                try
                {
                    sqlCon.Open();
                    SqlCommand Cmnd = new SqlCommand(cmdCountDbTobeProcess, sqlCon);
                    SqlDataReader rdr = Cmnd.ExecuteReader();
                    dtCompare.Load(rdr);
                    sqlCon.Close();
                }
                catch (Exception ex)
                {

                    throw ex;
                }
            }

            DataTable dtSql = null;
            if (dtCompare.Rows.Count > 0)
            {
                string cmdBackup = @"
                SET NOCOUNT ON;

            DECLARE @tempDb table (
                dbName NVARCHAR(256),
                pathName NVARCHAR(256),
                stat bit default 0,
                fileName NVARCHAR(256)
                )

            DECLARE 
                @FileName NVARCHAR(1024)
                , @DBName NVARCHAR(256)
                , @PathName NVARCHAR(256)
                , @Message NVARCHAR(2048)
                , @IsCompressed BIT

            SELECT 
                @PathName = @path_name
                , @IsCompressed = 0 

                insert into @tempDb (dbName, pathName, fileName)
                SELECT
                    sd.name
                    , file_path = @PathName + FileDate + '_' + name + '.bak'
                    , file_names = FileDate + '_' + name + '.bak'
                FROM sys.databases sd
                CROSS JOIN (
                    SELECT FileDate = 'ABT_' + (select replace(left(convert(char, getdate(), 120), 10) + '-' + replace(replace(right(getdate(), 8), ' ', ''), ':', '-'), ' ', ''))
                ) fd
                WHERE sd.state_desc != 'OFFLINE'
                    AND sd.name NOT IN ('master', 'model', 'msdb', 'tempdb')
                ORDER BY sd.name 

            DECLARE db CURSOR LOCAL READ_ONLY FAST_FORWARD FOR  
                select name = dbName, file_path = pathName from @tempDb

            OPEN db

            FETCH NEXT FROM db INTO 
                @DBName
                , @FileName  

            WHILE @@FETCH_STATUS = 0 BEGIN 

                DECLARE @SQL NVARCHAR(MAX)
                DECLARE @RESULT as INT,@DIR AS VARCHAR(8000)

                SELECT @Message = REPLICATE('-', 80) + CHAR(13) + CONVERT(VARCHAR(20), GETDATE(), 120) + N': ' + @DBName
                RAISERROR (@Message, 0, 1) WITH NOWAIT

                SELECT @SQL = 
                'BACKUP DATABASE [' + @DBName + ']
                TO DISK = N''' + @FileName + '''
                WITH FORMAT, ' + CASE WHEN @IsCompressed = 1 THEN N'COMPRESSION, ' ELSE '' END + N'INIT, STATS = 15;' 

                EXEC sys.sp_executesql @SQL

                EXEC master.dbo.xp_fileexist @FileName,@RESULT OUTPUT
                        IF(@RESULT = 1)
                        BEGIN
                            UPDATE @tempDb
                            SET stat = 1
                            WHERE dbName = @DBName
                        END

                FETCH NEXT FROM db INTO 
                    @DBName
                    , @FileName 

            END   

            CLOSE db;   
            DEALLOCATE db;

            WITH LastBackUp AS
            (
            SELECT  bs.database_name,
                    bs.backup_size,
                    bs.backup_start_date,
                    bs.backup_finish_date,
                    bmf.physical_device_name,
                    Position = ROW_NUMBER() OVER( PARTITION BY bs.database_name ORDER BY bs.backup_start_date DESC )
            FROM  msdb.dbo.backupmediafamily bmf
            JOIN msdb.dbo.backupmediaset bms ON bmf.media_set_id = bms.media_set_id
            JOIN msdb.dbo.backupset bs ON bms.media_set_id = bs.media_set_id
            WHERE   bs.[type] = 'D'
            AND bs.is_copy_only = 0
            )
            SELECT 
                    HOST_NAME() as HostnameDbServer,
                    sd.name AS DbName,
                    --CAST(backup_size / 1048576 AS DECIMAL(10, 2) ) AS BackupSizeMb,
                    backup_size AS BackupSize,
                    backup_start_date as BackupStartDate,
                    backup_finish_date as BackupFinishDate,
                    physical_device_name as PhysicalDeviceName,
                    t.stat as BackupStat
            FROM sys.databases AS sd
            LEFT JOIN LastBackUp AS lb
                ON sd.name = lb.database_name
                AND Position = 1
            JOIN @tempDb as t ON lb.physical_device_name = CASE WHEN CHARINDEX(t.fileName,lb.physical_device_name) > 0
                THEN lb.physical_device_name
                ELSE ''
           END
            ORDER BY dbName;
            ";

                using (SqlConnection sqlCon = new SqlConnection(connString))
                {
                    sqlCon.Open();
                    SqlCommand Cmnd = new SqlCommand(cmdBackup, sqlCon);
                    Cmnd.Parameters.AddWithValue("@path_name", backupPath);
                    SqlDataReader rdr = Cmnd.ExecuteReader();
                    dtSql.Load(rdr);
                    sqlCon.Close();
                }

                if (dtSql.Rows.Count != dtCompare.Rows.Count)
                {
                    throw new Exception("Err Integrity Checking");
                }
            }
            return dtSql;
        }

        private void AddMappedDrive(
            string connString,
            string backupPath = "",
            string backupServerUsername = "",
            string backupServerPassword = "",
            long backupTaskId = 0)
        {
            string cmdAddMappedDrive = @"
                EXEC sp_configure 'show advanced options', 1;
                GO
                RECONFIGURE;
                GO

                EXEC sp_configure 'xp_cmdshell',1
                GO
                RECONFIGURE
                GO

                Exec XP_CMDSHELL 'net use x: /delete'
                GO
                
                EXEC XP_CMDSHELL 'net use x: '+ @networkpath +' /user:'+@username+' '+@password+' ' + '/persistent:no' --richard, 20230130, need reconnect every backup
                GO
            ";

            using (SqlConnection sqlCon = new SqlConnection(connString))
            {
                sqlCon.Open();
                SqlCommand Cmnd = new SqlCommand(cmdAddMappedDrive, sqlCon);
                Cmnd.Parameters.AddWithValue("@BackupPathSvr", backupPath);
                Cmnd.Parameters.AddWithValue("@BackupPathSvrLoginUsername", backupServerUsername);
                Cmnd.Parameters.AddWithValue("@BackupPathSvrLoginPwd", backupServerPassword);

                Cmnd.ExecuteNonQuery();
                sqlCon.Close();
            }
        }
        #endregion
    }
}
