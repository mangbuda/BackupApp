using System;
using System.Configuration;
using System.ServiceProcess;
using System.Timers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using BackupAppService.BackupService;
using BackupAppService.Setting_;
using BackupAppService;

namespace WinServiceBackupApp
{
    public partial class Service1 : ServiceBase
    {
        private Timer _timer;
        private IHost _host;

        public Service1()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            _host = CreateHostBuilder(args).Build();
            _timer = new Timer();
            _timer.Interval = 1000; // 1 seconds
            _timer.Elapsed += new ElapsedEventHandler(TimerElapsed);
            _timer.Enabled = true;

            RunApplication();
        }

        private void TimerElapsed(object sender, ElapsedEventArgs e)
        {
            // Call your existing method here
            RunApplication();
        }

        protected override void OnStop()
        {
            _timer.Enabled = false;
            _timer.Dispose();
            _host.StopAsync().GetAwaiter().GetResult();
            _host.Dispose();
        }

        private void RunApplication()
        {
            //Console.WriteLine("Hello World!");

            //string testSetting = ConfigurationManager.AppSettings["TestSetting"];
            //Console.WriteLine(testSetting);

            CreateTaskFunction(_host.Services);
            BackupFunction(_host.Services);
        }

        public static void CreateTaskFunction(IServiceProvider services)
        {
            using (var serviceScope = services.CreateScope())
            {
                var provider = serviceScope.ServiceProvider;

                var task = provider.GetRequiredService<IBackupTaskService>();

                task.CreateBackupTask();
            }
        }

        public static void BackupFunction(IServiceProvider services)
        {
            using (var serviceScope = services.CreateScope())
            {
                var provider = serviceScope.ServiceProvider;

                var task = provider.GetRequiredService<IBackupService>();

                task.Backup();
            }
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
