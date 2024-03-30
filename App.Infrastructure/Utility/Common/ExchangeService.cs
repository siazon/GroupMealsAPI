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

namespace App.Infrastructure.Utility.Common
{
    public class ExchangeService : IHostedService
    {
        IDbCommonRepository<DbShop> _shopRepository;
      //public  ExchangeService(IDbCommonRepository<DbShop> shopRepository)
      //  {

            
      //      _shopRepository = shopRepository;
      //  }
        public async Task StartAsync(CancellationToken cancellationToken)
        {
            
            var schedulerFactory = new StdSchedulerFactory();
            var scheduler = await schedulerFactory.GetScheduler();
            await scheduler.Start();

            //创建作业和触发器
            var jobDetail = JobBuilder.Create<ExchangeRateTask>().SetJobData(new JobDataMap() {
                                new KeyValuePair<string, object>("_shopRepository", _shopRepository),
                            }).Build();
            var trigger = TriggerBuilder.Create()
                                        .WithSimpleSchedule(m =>
                                        {
                                            m.WithRepeatCount(0).WithIntervalInSeconds(10);
                                        }).StartAt(new DateTimeOffset(DateTime.Now.AddSeconds(20)))
                                        .Build();

            //添加调度
            await scheduler.ScheduleJob(jobDetail, trigger);

        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
