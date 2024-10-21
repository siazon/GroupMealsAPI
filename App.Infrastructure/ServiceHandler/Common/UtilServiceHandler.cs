using App.Domain.Common;
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
using System.Linq;
using System.Threading.Tasks;

namespace App.Infrastructure.ServiceHandler.Common
{
    public interface IUtilServiceHandler
    {
        Task<ResponseModel> CheckAppVersion(int shopId);
    }

    public class UtilServiceHandler : IUtilServiceHandler
    {
        private readonly IDbCommonRepository<DbShop> _shopRepository;
        IShopServiceHandler _shopServiceHandler;
        IMemoryCache _memoryCache;

        public UtilServiceHandler(IShopServiceHandler shopServiceHandler, IMemoryCache memoryCache)
        {
            _memoryCache = memoryCache;
            _shopServiceHandler = shopServiceHandler;
        }

        public async Task<ResponseModel> CheckAppVersion(int shopId)
        {
            var shop = await _shopServiceHandler.GetBasicShopInfo(shopId);
            if (shop != null)
            {
                var version = shop.ShopSettings.FirstOrDefault(a => a.SettingKey == "app.version")?.SettingValue;
                var appUrl = shop.ShopSettings.FirstOrDefault(a => a.SettingKey == "app.url")?.SettingValue;
                if(!string.IsNullOrWhiteSpace(version)&&!string.IsNullOrWhiteSpace(appUrl))
                return new ResponseModel { msg = "ok", data = new { version, appUrl } };
            }
            return new ResponseModel { msg = "error", data = null };
        }

    }
}