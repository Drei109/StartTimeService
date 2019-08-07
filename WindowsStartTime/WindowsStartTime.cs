using Quartz;
using Quartz.Impl;
using System;
using System.Configuration;
using System.Net.Http;
using System.Net.Http.Headers;
using System.ServiceProcess;
using System.Threading.Tasks;

namespace WindowsStartTime
{
    public partial class WindowsStartTime : ServiceBase
    {
        //Local variables
        private static readonly int localId = Convert.ToInt32(ConfigurationManager.AppSettings["local"]);

        private static readonly string baseAddress = ConfigurationManager.AppSettings["baseAddress"];
        private static readonly string createAddress = ConfigurationManager.AppSettings["createAddress"];
        private static readonly string updateAddress = ConfigurationManager.AppSettings["updateAddress"];
        private static readonly int timeInterval = Convert.ToInt32(ConfigurationManager.AppSettings["timeInterval"]);
        public static string nombreServicio = "WindowsStartTime";

        //Quartz
        private static readonly StdSchedulerFactory factory = new StdSchedulerFactory();

        //Triggers
        private static readonly ITrigger triggerUpdateTurnOffTime = TriggerBuilder.Create()
                .WithIdentity("triggerUpdateTurnOffTime", "group1")
                .StartAt(DateBuilder.FutureDate(5, IntervalUnit.Minute))
                .WithSimpleSchedule(x => x
                    .WithIntervalInMinutes(timeInterval)
                    .RepeatForever())
                .Build();

        private static readonly ITrigger triggerCheckStartUpTime = TriggerBuilder.Create()
                .WithIdentity("triggerCheckStartUpTime", "group1")
                .StartNow().WithSimpleSchedule(x => x
                    .WithIntervalInMinutes(timeInterval)
                    .RepeatForever())
                .Build();

        //Jobs
        private static readonly IJobDetail jobStart = JobBuilder.Create<StartJob>()
                .WithIdentity("jobStart", "group1")
                .Build();

        private static readonly IJobDetail jobUpdate = JobBuilder.Create<UpdateJob>()
            .WithIdentity("jobUpdate", "group1")
            .Build();

        //Service Methods
        public WindowsStartTime()
        {
            InitializeComponent();
            CanShutdown = true;
            CanHandleSessionChangeEvent = true;
        }

        protected override void OnStart(string[] args)
        {
            RunAsyncStart().Wait();
            RunAsyncJobs().Wait();
            //RunTimer();
        }

        protected override void OnStop()
        {
            RunAsyncStop().Wait();
        }

        protected override void OnShutdown()
        {
            RunAsyncStop().Wait();
            base.OnShutdown();
        }

        protected override void OnSessionChange(SessionChangeDescription changeDescription)
        {
            if (changeDescription.Reason == SessionChangeReason.SessionLogoff)
            {
                RunAsyncStop().Wait();
            }

            if (changeDescription.Reason == SessionChangeReason.SessionLogon)
            {
                RunAsyncStart().Wait();
            }

            base.OnSessionChange(changeDescription);
        }

        //Async Methods
        private static async Task RunAsyncStart()
        {
            using (var client = new HttpClient())
            {
                client.BaseAddress = new Uri(baseAddress);
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                // HTTP POST
                var registroNuevo = new Registro()
                {
                    local_id = localId,
                    tipo = 1
                };
                HttpResponseMessage response = await client.PostAsJsonAsync(createAddress, registroNuevo).ConfigureAwait(false);
            }
        }

        private static async Task RunAsyncStop()
        {
            using (var client = new HttpClient())
            {
                client.BaseAddress = new Uri(baseAddress);
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                // HTTP UPDATE
                var registroNuevo = new Registro()
                {
                    local_id = localId,
                    tipo = 2
                };
                HttpResponseMessage response = await client.PostAsJsonAsync(updateAddress, registroNuevo).ConfigureAwait(false);
            }
        }

        private static async Task RunAsyncJobs()
        {
            IScheduler sched = await factory.GetScheduler().ConfigureAwait(false);
            await sched.Start().ConfigureAwait(false);

            await sched.ScheduleJob(jobStart, triggerCheckStartUpTime).ConfigureAwait(false);
            await sched.ScheduleJob(jobUpdate, triggerUpdateTurnOffTime).ConfigureAwait(false);
        }

        // Job Definition
        public class UpdateJob : IJob
        {
            public async Task Execute(IJobExecutionContext context)
            {
                using (var client = new HttpClient())
                {
                    client.BaseAddress = new Uri(baseAddress);
                    client.DefaultRequestHeaders.Accept.Clear();
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                    // HTTP UPDATE
                    var registroNuevo = new Registro()
                    {
                        local_id = localId,
                        tipo = 1
                    };
                    HttpResponseMessage response = await client.PostAsJsonAsync(updateAddress, registroNuevo).ConfigureAwait(false);
                }
            }
        }

        public class StartJob : IJob
        {
            public async Task Execute(IJobExecutionContext context)
            {
                await RunAsyncStart().ConfigureAwait(false);
            }
        }
    }
}