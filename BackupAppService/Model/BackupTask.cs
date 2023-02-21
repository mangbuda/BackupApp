using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BackupAppService.Model
{
    public class BackupTask
    {
        public long BackupTaskId { get; set; }
        public DateTime TaskEndTime { get; set; }
        public string TaskStatus { get; set; }
    }
}
