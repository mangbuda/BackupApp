using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BackupAppService.Model
{
    public class BackupLog
    {
        public long BackupHistoryId { get; set; }
        public string LogType { get; set; }
        public string LogMessage { get; set; }
        public DateTime LogTime { get; set; }
    }
}
