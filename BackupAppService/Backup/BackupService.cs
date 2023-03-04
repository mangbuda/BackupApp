using BackupAppService.Model;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Net.Mail;
using System.Net;
using System.Runtime.InteropServices;
using System.Security.Policy;
using System.Security.Cryptography;
using System.Xml.Linq;

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

                    Email email = new Email();
                    email.SmtpSvr = row["SmtpSvr"].ToString();
                    email.SmtpPort = row["SmtpPort"].ToString();
                    email.SmtpNotifEmailFrom = row["SmtpNotifEmailFrom"].ToString();
                    email.SmtpNotifEmailTo = row["SmtpNotifEmailFrom"].ToString();
                    email.SmtpLoginUsername = row["SmtpLoginUsername"].ToString();
                    email.SmtpLoginPwd = row["SmtpLoginPwd"].ToString();
                    email.IsSslForSmtp = row["IsSslForSmtp"].ToString();

                    try
                    {
                        string connectionString = "Data Source=" + DbSvr + ";User ID=" + DbSvrLoginUsername + ";Password=" + DbSvrLoginPwd + "";

                        bool driveAdded = false;
                        string bPath = "x://";

                        if (!string.IsNullOrEmpty(BackupPathSvrLoginUsername)
                            && !string.IsNullOrEmpty(BackupPathSvrLoginPwd))
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
                            bPath = "D:\\Backup\\DSF_20230301\\";
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
                                DbName = dtRes.Rows[j]["DbName"].ToString(),
                                BackupSize = float.Parse(dtRes.Rows[j]["BackupSize"].ToString()),
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

                        #region send email
                        DataTable dtResForEmail = backupHistService.GetSummaryBackupHistByBackupHistId(BackupTaskId);
                        string html = GetHtmlForEmail(dtResForEmail);
                        SendEmail(html, email);
                        #endregion
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
                    AND sd.name LIKE '%REPORT%'
            ";
            DataTable dtCompare = new DataTable();
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

            DataTable dtSql = new DataTable();
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
                    AND sd.name LIKE '%REPORT%'
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

        public string GetHtmlForEmail(DataTable dtSource)
        {
            try
            {
                string messageBody = "<font>The following are the records: </font><br><br>";
                if (dtSource.Rows.Count == 0) return messageBody;
                string htmlTableStart = "<table style=\"border-collapse:collapse; text-align:center;\" >";
                string htmlTableEnd = "</table>";
                string htmlHeaderRowStart = "<tr style=\"background-color:#6FA1D2; color:#ffffff;\">";
                string htmlHeaderRowEnd = "</tr>";
                string htmlTrStart = "<tr style=\"color:#555555;\">";
                string htmlTrEnd = "</tr>";
                string htmlTdStart = "<td style=\" border-color:#5c87b2; border-style:solid; border-width:thin; padding: 5px;\">";
                string htmlTdEnd = "</td>";
                messageBody += htmlTableStart;
                messageBody += htmlHeaderRowStart;
                messageBody += htmlTdStart + "Hostname DB Server" + htmlTdEnd;
                messageBody += htmlTdStart + "Backup Size" + htmlTdEnd;
                messageBody += htmlTdStart + "Start Time" + htmlTdEnd;
                messageBody += htmlTdStart + "End Time" + htmlTdEnd;
                messageBody += htmlTdStart + "Backup Status" + htmlTdEnd;
                messageBody += htmlTdStart + "Remarks" + htmlTdEnd;
                messageBody += htmlHeaderRowEnd;
                for (int i = 0; i <= dtSource.Rows.Count - 1; i++)
                {
                    messageBody = messageBody + htmlTrStart;
                    messageBody = messageBody + htmlTdStart + dtSource.Rows[i]["HostnameDbSvr"].ToString() + htmlTdEnd;
                    messageBody = messageBody + htmlTdStart + SizeSuffix(Convert.ToInt64(dtSource.Rows[i]["BackupSize"].ToString())) + htmlTdEnd;
                    messageBody = messageBody + htmlTdStart + dtSource.Rows[i]["BackupStartTime"].ToString() + htmlTdEnd;
                    messageBody = messageBody + htmlTdStart + dtSource.Rows[i]["BackupFinishTime"].ToString() + htmlTdEnd;
                    messageBody = messageBody + htmlTdStart + "COMPLETE" + htmlTdEnd;
                    messageBody = messageBody + htmlTdStart + (dtSource.Rows[i]["BackupStatus"].ToString() == "True"?"Backup Completed Successfully":"Backup Failed") + htmlTdEnd;

                    messageBody = messageBody + htmlTrEnd;
                }
                messageBody = messageBody + htmlTableEnd;
                return messageBody;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        private void SendEmail(string htmlString, Email email)
        {
            try
            {
                MailMessage message = new MailMessage();
                SmtpClient smtp = new SmtpClient();
                message.From = new MailAddress(email.SmtpNotifEmailFrom);
                message.To.Add(new MailAddress(email.SmtpNotifEmailTo));
                message.Subject = "Backup Job Report";
                message.IsBodyHtml = true;
                message.Body = htmlString;
                smtp.Port = Convert.ToInt32(email.SmtpPort);
                smtp.Host = email.SmtpSvr;
                smtp.EnableSsl = email.IsSslForSmtp == "1" ? true: false;
                smtp.UseDefaultCredentials = false;
                smtp.Credentials = new NetworkCredential(email.SmtpNotifEmailFrom, email.SmtpLoginPwd);
                smtp.DeliveryMethod = SmtpDeliveryMethod.Network;
                smtp.Send(message);
            }
            catch (Exception ex) 
            {
                throw ex;
            }
        }

        static readonly string[] SizeSuffixes =
                   { "bytes", "KB", "MB", "GB", "TB", "PB", "EB", "ZB", "YB" };
        static string SizeSuffix(Int64 value, int decimalPlaces = 1)
        {
            if (decimalPlaces < 0) { throw new ArgumentOutOfRangeException("decimalPlaces"); }
            if (value < 0) { return "-" + SizeSuffix(-value, decimalPlaces); }
            if (value == 0) { return string.Format("{0:n" + decimalPlaces + "} bytes", 0); }

            // mag is 0 for bytes, 1 for KB, 2, for MB, etc.
            int mag = (int)Math.Log(value, 1024);

            // 1L << (mag * 10) == 2 ^ (10 * mag) 
            // [i.e. the number of bytes in the unit corresponding to mag]
            decimal adjustedSize = (decimal)value / (1L << (mag * 10));

            // make adjustment when the value is large enough that
            // it would round up to 1000 or more
            if (Math.Round(adjustedSize, decimalPlaces) >= 1000)
            {
                mag += 1;
                adjustedSize /= 1024;
            }

            return string.Format("{0:n" + decimalPlaces + "} {1}",
                adjustedSize,
                SizeSuffixes[mag]);
        }
        #endregion
    }
}

//MailMessage message = new MailMessage();
//SmtpClient smtp = new SmtpClient();
//message.From = new MailAddress("FromMailAddress");
//message.To.Add(new MailAddress("ToMailAddress"));
//message.Subject = "Test";
//message.IsBodyHtml = true; //to make message body as html
//message.Body = htmlString;
//smtp.Port = 587;
//smtp.Host = "smtp.gmail.com"; //for gmail host
//smtp.EnableSsl = true;
//smtp.UseDefaultCredentials = false;
//smtp.Credentials = new NetworkCredential("FromMailAddress", "password");
//smtp.DeliveryMethod = SmtpDeliveryMethod.Network;
//smtp.Send(message);
