using App.Domain.Common.Shop;
using App.Domain.TravelMeals.Restaurant;
using App.Infrastructure.Exceptions;
using App.Infrastructure.Repository;
using App.Infrastructure.Validation;
using Microsoft.CodeAnalysis.Text;
using Microsoft.Extensions.Caching.Memory;
using Stripe;
using System;
using System.Threading.Tasks;

namespace App.Infrastructure.ServiceHandler.Common
{
    public interface IShopServiceHandler
    {
        Task<DbShop> GetShopInfo(int shopId);
        Task<DbExchangeRate> UpdateExchangeRateExtra(double exRateExtra, int shopId);
        Task<DbExchangeRate> GetExchangeRate(int shopId);
    }

    public class BookingBatchServiceHandler : IShopServiceHandler
    {
        private readonly IDbCommonRepository<DbShop> _shopRepository;
        IMemoryCache _memoryCache;

        public BookingBatchServiceHandler(IDbCommonRepository<DbShop> shopRepository,IMemoryCache memoryCache)
        {
            _memoryCache = memoryCache;
            _shopRepository = shopRepository;
        }

        public async Task<DbShop> GetShopInfo(int shopId)
        {
            var shopInfo = await GetBasicShopInfo(shopId);
            return shopInfo.ClearForOutPut();
        }

        public async Task<DbShop> GetBasicShopInfo(int shopId)
        {
            Guard.GreaterThanZero(shopId);

            var shopInfo =
                await _shopRepository.GetOneAsync(r => r.ShopId == shopId && r.IsActive.HasValue && r.IsActive.Value);
            if (shopInfo == null)
                throw new ServiceException("Cannot find shop info");
            return shopInfo;
        }
        public async Task<DbExchangeRate> UpdateExchangeRateExtra(double exRateExtra, int shopId)
        {
            var existShop =
               await _shopRepository.GetOneAsync(a => a.ShopId==shopId);
            if (existShop == null)
                throw new ServiceException("shop Not Exists");
            existShop.ExchangeRateExtra = exRateExtra;
             var savedShop= await _shopRepository.UpdateAsync(existShop);
            DbExchangeRate rate = new DbExchangeRate() { Rate = savedShop.ExchangeRate, UpdateTime = savedShop.RateUpdate };
            return rate;
        }
        public async Task<DbExchangeRate> GetExchangeRate(int shopId)
        {
            var savedShop =
               await _shopRepository.GetOneAsync(a => a.ShopId == shopId);
            DbExchangeRate rate = new DbExchangeRate() { Rate = savedShop.ExchangeRate, UpdateTime = savedShop.RateUpdate };
            return rate;
        }

    }
}