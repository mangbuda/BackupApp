using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BackupAppService.Model
{
    public class BackupHist
    {
        public long BackupHistId { get; set; }
        public long BackupTaskId { get; set; }
        public long SettingId { get; set; }
        public string HostnameDbSvr { get; set; }
        public string DbName { get; set; }
        public float BackupSize { get; set; }
        public DateTime BackupStartTime { get; set; }
        public DateTime BackupFinishTime { get; set; }
        public string BackupFilename { get; set; }
        public string BackupStatus { get; set; }
    }
}
