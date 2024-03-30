using App.Domain.Common.Shop;
using App.Domain.TravelMeals;
using App.Infrastructure.Builders.Common;
using App.Infrastructure.Exceptions;
using App.Infrastructure.Repository;
using App.Infrastructure.ServiceHandler.TravelMeals;
using Quartz;
using Stripe;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace App.Infrastructure.Utility.Common
{
    public class ExchangeRateTask : IJob
    {


        //public ExchangeRateTask(IDbCommonRepository<DbShop> shopRepository)
        //{
        //    _shopRepository = shopRepository;
        //}

        private readonly IDbCommonRepository<DbShop> _shopRepository;
        public Task Execute(IJobExecutionContext context)
        {
            return Task.Factory.StartNew(async () =>
            {
                try
                {


                    var existShop = await _shopRepository.GetOneAsync(r => r.ShopId == 11);
                    if (existShop == null)
                        throw new ServiceException("shop Not Exists");
                    var savedShop = await _shopRepository.UpdateAsync(existShop);
                }
                catch (Exception ex)
                {

                    Console.WriteLine(ex);
                }

            });
        }
    }
}
