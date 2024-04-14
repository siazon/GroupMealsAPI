using Microsoft.Extensions.Hosting;
using Quartz.Impl;
using Quartz;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using App.Domain.Common.Shop;
using App.Infrastructure.Repository;
using Hangfire;
using App.Infrastructure.Exceptions;
using App.Domain.Common;
using Newtonsoft.Json;
using Microsoft.Extensions.DependencyInjection;
using App.Infrastructure.ServiceHandler.TravelMeals;
using static System.Formats.Asn1.AsnWriter;

namespace App.Infrastructure.Utility.Common
{
    public class ExchangeService : IHostedService
    {
        private readonly IServiceScopeFactory _serviceScopeFactory;
        public ExchangeService(IServiceScopeFactory serviceScopeFactory)
        {
            _serviceScopeFactory = serviceScopeFactory;
        }
        public async Task StartAsync(CancellationToken cancellationToken)
        {

            RecurringJob.AddOrUpdate("TO_DO_ANOTHER_TASK_JOB", () => DoTask(), Cron.Daily);
            RecurringJob.AddOrUpdate("TO_DO_ANOTHER_TASK_JOB_Minutely", () => DoTaskMinutely(), Cron.Minutely);

            //var schedulerFactory = new StdSchedulerFactory();
            //var scheduler = await schedulerFactory.GetScheduler();
            //await scheduler.Start();

            ////创建作业和触发器
            //var jobDetail = JobBuilder.Create<ExchangeRateTask>().SetJobData(new JobDataMap() {
            //                    new KeyValuePair<string, object>("_shopRepository", _shopRepository),
            //                }).Build();
            //var trigger = TriggerBuilder.Create()
            //                            .WithSimpleSchedule(m =>
            //                            {
            //                                m.WithRepeatCount(0).WithIntervalInSeconds(10);
            //                            }).StartAt(new DateTimeOffset(DateTime.Now.AddSeconds(20)))
            //                            .Build();

            ////添加调度
            //await scheduler.ScheduleJob(jobDetail, trigger);

        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
        public void DoTaskMinutely()
        {
            using (var scope = _serviceScopeFactory.CreateScope())
            {

                var _restaurantBookingServiceHandler = scope.ServiceProvider.GetRequiredService<ITrRestaurantBookingServiceHandler>();

                _restaurantBookingServiceHandler.SettleOrder();

            }
        }
        public void DoTask()
        {



            using (var scope = _serviceScopeFactory.CreateScope())
            {
                // 获取需要的服务实例
                var myService = scope.ServiceProvider.GetRequiredService<IExchangeUtil>();

                // 在这里执行后台任务
                myService.UpdateToDB(9);


            }

            Console.WriteLine("ssss" + DateTime.Now.ToString("HH:mm:ss"));





        }


    }

}
