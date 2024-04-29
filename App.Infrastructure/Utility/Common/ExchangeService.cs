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
#if RELEASE
            RecurringJob.AddOrUpdate("Daily_Update_ExChangeRate_TASK_JOB", () => DoTask(), Cron.Daily);
            //RecurringJob.AddOrUpdate("TO_DO_ANOTHER_TASK_JOB_Minutely", () => DoTaskMinutely(), Cron.Daily);
#endif
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
#if RELEASE
            using (var scope = _serviceScopeFactory.CreateScope())
            {
              var _restaurantBookingServiceHandler = scope.ServiceProvider.GetRequiredService<ITrRestaurantBookingServiceHandler>();

                _restaurantBookingServiceHandler.SettleOrder();
                // 获取需要的服务实例
                var myService = scope.ServiceProvider.GetRequiredService<IExchangeUtil>();
                // 在这里执行后台任务
                myService.getGBPExchangeRate();


            }
#endif
        }


    }

}
