using BackupAppService.Model;
using System.Collections.Generic;
using System.Data;

namespace BackupAppService.BackupService
{
    public interface IBackupHistService
    {
        void CreateBackupHist(List<BackupHist> backupHists);
        DataTable GetSummaryBackupHistByBackupHistId(long backupTaskId);
    }
}