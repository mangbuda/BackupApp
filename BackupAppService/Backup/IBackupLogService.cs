using BackupAppService.Model;

namespace BackupAppService.BackupService
{
    public interface IBackupLogService
    {
        void CreateLog(BackupLog backupLog);
    }
}