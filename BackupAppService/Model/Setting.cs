using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BackupAppService.Model
{
    public class Setting
    {
        public long SettingId { get; set; }
        public string BackupStatus { get; set; }
        public string BackupStorePeriodType { get; set; }
        public string BackupStoreNumDays { get; set; }
        public string BackupPathSvr { get; set; }
        public string BackupPathSvrLoginUsername { get; set; }
        public string BackupPathSvrLoginPwd { get; set; }
        public string DbSvr { get; set; }
        public string DbSvrLoginUsername { get; set; }
        public string DbSvrLoginPwd { get; set; }
        public string SmtpSvr { get; set; }
        public string SmtpPort { get; set; }
        public string IsSslForSmtp { get; set; }
        public string SmtpLoginUsername { get; set; }
        public string SmtpLoginPwd { get; set; }
        public string SmtpNotifEmailFrom { get; set; }
        public string SmtpNotifEmailTo { get; set; }
        public string IsActive { get; set; }
        public string LastBackupDate { get; set; }
        public string NextBackupDate { get; set; }
        public string Version { get; set; }

    }
}
