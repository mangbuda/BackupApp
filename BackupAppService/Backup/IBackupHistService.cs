using BackupAppService.Model;
using System.Collections.Generic;

namespace BackupAppService.BackupService
{
    public interface IBackupHistService
    {
        void CreateBackupHist(List<BackupHist> backupHists);
    }
}