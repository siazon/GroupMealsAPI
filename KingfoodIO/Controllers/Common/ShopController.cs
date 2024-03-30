using App.Domain.Common.Customer;
using App.Domain.Common.Shop;
using App.Domain.Config;
using App.Domain.TravelMeals;
using App.Domain.TravelMeals.Restaurant;
using App.Infrastructure.ServiceHandler.Common;
using App.Infrastructure.ServiceHandler.TravelMeals;
using App.Infrastructure.Utility.Common;
using KingfoodIO.Application.Filter;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Stripe;
using System.Net;
using System.Threading.Tasks;

namespace KingfoodIO.Controllers.Common
{
    [Route("api/[controller]/[action]")]
    public class ShopController : BaseController
    {
        private readonly IShopServiceHandler _shopServiceHandler;
        IExcahngeUtil _excahngeUtil;

        IMemoryCache _memoryCache;
        public ShopController(IShopServiceHandler shopServiceHandler, IOptions<CacheSettingConfig> cachesettingConfig, IExcahngeUtil excahngeUtil,
         IMemoryCache memoryCache, IRedisCache redisCache, ILogManager logger) : base(cachesettingConfig, memoryCache, redisCache, logger)
        {
            _shopServiceHandler = shopServiceHandler;
            _memoryCache = memoryCache;
            _excahngeUtil=  excahngeUtil;
        }

        //[ServiceFilter(typeof(AuthActionFilter))]
        [HttpGet]
        [ProducesResponseType(typeof(DbShop), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetShopInfo(int shopId, bool cache = true)
        {
            var shopInfo= await ExecuteAsync(shopId, cache,
                async () => await _shopServiceHandler.GetShopInfo(shopId));
    
            return shopInfo;
        }

        [HttpGet]
        [ProducesResponseType(typeof(DbCustomer), (int)HttpStatusCode.OK)] 
        [ServiceFilter(typeof(AuthActionFilter))]
        public async Task<IActionResult> UpdateExchangeRate(double exRate, int shopId)
        {
            _excahngeUtil.getGBPExchangeRate();
            _memoryCache.Set("ExchangeRate", exRate);
            return await ExecuteAsync(shopId, false,
                async () => await _shopServiceHandler.UpdateExchangeRate(exRate, shopId));
        }

    }
}