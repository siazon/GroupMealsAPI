using App.Domain.Common.Shop;
using App.Domain.TravelMeals.Restaurant;
using App.Infrastructure.Exceptions;
using App.Infrastructure.Repository;
using App.Infrastructure.Validation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis.Text;
using Microsoft.Extensions.Caching.Memory;
using SendGrid.Helpers.Mail;
using Stripe;
using System;
using System.Threading.Tasks;

namespace App.Infrastructure.ServiceHandler.Common
{
    public interface IShopServiceHandler
    {
        Task<DbShop> GetShopInfo(int shopId);
        Task<DbShop> GetBasicShopInfo(int shopId);
        Task<DbExchangeRate> UpdateExchangeRateExtra(double exRateExtra, int shopId);
        Task<DbExchangeRate> GetExchangeRate(int shopId);
    }

    public class ShopServiceHandler : IShopServiceHandler
    {
        private readonly IDbCommonRepository<DbShop> _shopRepository;
        IMemoryCache _memoryCache;

        public ShopServiceHandler(IDbCommonRepository<DbShop> shopRepository, IMemoryCache memoryCache)
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
            var cacheKey = string.Format("motionmedia-{1}-{0}", shopId, typeof(DbShop).Name);
            var cacheResult = _memoryCache.Get<DbShop>(cacheKey);
            if (cacheResult != null)
            {
                return cacheResult;
            }
            var shopInfo =
                await _shopRepository.GetOneAsync(r => r.ShopId == shopId && r.IsActive.HasValue && r.IsActive.Value);
            if (shopInfo == null)
                throw new ServiceException("Cannot find shop info");
            _memoryCache.Set(cacheKey, shopInfo);
            return shopInfo;
        }
        
        public async Task<DbExchangeRate> UpdateExchangeRateExtra(double exRateExtra, int shopId)
        {
            
            return null;
        }
        public async Task<DbExchangeRate> GetExchangeRate(int shopId)
        {
           
            return null;
        }

    }
}