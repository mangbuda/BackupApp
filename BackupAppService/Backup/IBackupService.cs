namespace BackupAppService.BackupService
{
    public interface IBackupService
    {
        void Backup();
        void ResetFlagIsBackupInProcess();
    }
}