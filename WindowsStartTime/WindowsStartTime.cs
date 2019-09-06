using Quartz;
using Quartz.Impl;
using System;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.NetworkInformation;
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
        private static readonly int tipo = Convert.ToInt32(ConfigurationManager.AppSettings["tipo"]);
        public static string nombreServicio = "WindowsStartTime";

        private static readonly string macAddress = (from nic in NetworkInterface.GetAllNetworkInterfaces()
                                                     where nic.OperationalStatus == OperationalStatus.Up
                                                     select nic.GetPhysicalAddress().ToString()
                                            ).FirstOrDefault();

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
            RunAsyncMethod(1, createAddress).Wait();
            RunAsyncJobs().Wait();
        }

        protected override void OnStop()
        {
            RunAsyncMethod(2, updateAddress).Wait();
        }

        protected override void OnShutdown()
        {
            RunAsyncMethod(2, updateAddress).Wait();
            base.OnShutdown();
        }

        protected override void OnSessionChange(SessionChangeDescription changeDescription)
        {
            if (changeDescription.Reason == SessionChangeReason.SessionLogoff)
            {
                RunAsyncMethod(2, updateAddress).Wait();
            }

            if (changeDescription.Reason == SessionChangeReason.SessionLogon)
            {
                RunAsyncMethod(1, createAddress).Wait();
                //RunAsyncStart().Wait();
            }

            base.OnSessionChange(changeDescription);
        }

        //Async Methods
        private static async Task RunAsyncMethod(int estado, string direccion)
        {
            using (var client = new HttpClient())
            {
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                client.BaseAddress = new Uri(baseAddress);
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                var registroNuevo = new Registro()
                {
                    estado = estado,
                    mac = macAddress,
                    tipo_id = tipo
                };
                HttpResponseMessage response = await client.PostAsJsonAsync(direccion, registroNuevo).ConfigureAwait(false);
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
                await RunAsyncMethod(1, updateAddress).ConfigureAwait(false);
            }
        }

        public class StartJob : IJob
        {
            public async Task Execute(IJobExecutionContext context)
            {
                await RunAsyncMethod(1, createAddress).ConfigureAwait(false);
                //await RunAsyncStart().ConfigureAwait(false);
            }
        }
    }
}