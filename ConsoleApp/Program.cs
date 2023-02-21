using BackupAppService;
using BackupAppService.BackupService;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Threading.Tasks;
using System.Configuration;
using BackupAppService.Setting_;

namespace ConsoleApp
{
    public class Program
    {
        public static Task Main(string[] args)
        {
            Console.WriteLine("Hello World!");

            string testSetting = ConfigurationManager.AppSettings["TestSetting"];
            Console.WriteLine(testSetting);

            using var host = CreateHostBuilder(args).Build();
            CreateTaskFunction(host.Services);
            BackupFunction(host.Services);

            return host.RunAsync();
        }

        public static void CreateTaskFunction(IServiceProvider services)
        {
            using var serviceScope = services.CreateScope();
            var provider = serviceScope.ServiceProvider;

            var task = provider.GetRequiredService<IBackupTaskService>();

            task.CreateBackupTask();
        }

        public static void BackupFunction(IServiceProvider services)
        {
            using var serviceScope = services.CreateScope();
            var provider = serviceScope.ServiceProvider;

            var task = provider.GetRequiredService<IBackupService>();

            task.Backup();
        }

        private static IHostBuilder CreateHostBuilder(string[] args)
        {
            return Host.CreateDefaultBuilder(args)
                .ConfigureServices((_, services) =>
                    services.AddTransient<IBackupService, BackupService>()
                        .AddTransient<IBackupHistService, BackupHistService>()
                        .AddTransient<IBackupLogService, BackupLogService>()
                        .AddTransient<IBackupTaskService, BackupTaskService>()
                        .AddTransient<ISqliteReaderService, SqliteReaderService>()
                        .AddTransient<ISettingService, SettingService>());
        }

    }
}
