using BackupAppService.Model;
using System.Data;

namespace BackupAppService.BackupService
{
    public interface IBackupTaskService
    {
        void CreateBackupTask();
        DataTable GetBackupTask();
        void UpdateBackupTask(BackupTask backupTask);
    }
}