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
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;

namespace KingfoodIO.Controllers.Common
{
    /// <summary>
    /// 
    /// </summary>
    [Route("api/[controller]/[action]")]
    public class ShopController : BaseController
    {
        private readonly IShopServiceHandler _shopServiceHandler;
        IExchangeUtil _excahngeUtil;

        IMemoryCache _memoryCache;
        /// <summary>
        /// 
        /// </summary>
        /// <param name="shopServiceHandler"></param>
        /// <param name="cachesettingConfig"></param>
        /// <param name="excahngeUtil"></param>
        /// <param name="memoryCache"></param>
        /// <param name="redisCache"></param>
        /// <param name="logger"></param>
        public ShopController(IShopServiceHandler shopServiceHandler, IOptions<CacheSettingConfig> cachesettingConfig, IExchangeUtil excahngeUtil,
         IMemoryCache memoryCache, IRedisCache redisCache, ILogManager logger) : base(cachesettingConfig, memoryCache, redisCache, logger)
        {
            _shopServiceHandler = shopServiceHandler;
            _memoryCache = memoryCache;
            _excahngeUtil=  excahngeUtil;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="shopId"></param>
        /// <param name="cache"></param>
        /// <returns></returns>
        //[ServiceFilter(typeof(AuthActionFilter))]
        [HttpGet]
        [ProducesResponseType(typeof(DbShop), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetShopInfo(int shopId, bool cache = true)
        {
            var shopInfo= await ExecuteAsync(shopId, cache,
                async () => await _shopServiceHandler.GetShopInfo(shopId));
    
            return shopInfo;
        }
        [HttpPost]
        [ProducesResponseType(typeof(DbShop), (int)HttpStatusCode.OK)]
        [ServiceFilter(typeof(AdminAuthFilter))]
        [ServiceFilter(typeof(AuthActionFilter))]
        public async Task<IActionResult> UpdateDeclineReasons([FromBody] List<string> reasons, int shopId)
        {
            return await ExecuteAsync(shopId, false,
                async () => await _shopServiceHandler.UpdateDeclineReasons( shopId, reasons));
        }
        [HttpGet]
        [ProducesResponseType(typeof(List<string>), (int)HttpStatusCode.OK)]
        //[ServiceFilter(typeof(AdminAuthFilter))]
        [ServiceFilter(typeof(AuthActionFilter))]
        public async Task<IActionResult> GetDeclineReasons( int shopId)
        {
            return await ExecuteAsync(shopId, false,
                async () => await _shopServiceHandler.GetDeclineReasons( shopId));
        }
        /// <summary>
        /// 暂弃用，永远返回固定值
        /// </summary>
        /// <param name="exRateExtra"></param>
        /// <param name="shopId"></param>
        /// <returns></returns>
        [HttpGet]
        [ProducesResponseType(typeof(DbExchangeRate), (int)HttpStatusCode.OK)] 
        [ServiceFilter(typeof(AuthActionFilter))]
        public async Task<IActionResult> UpdateExchangeRateExtra(double exRateExtra, int shopId)
        {
            return await ExecuteAsync(shopId, false,
                async () => await _shopServiceHandler.UpdateExchangeRateExtra(exRateExtra, shopId));
        }


        /// <summary>
        /// 暂弃用，永远返回固定值
        /// </summary>
        /// <param name="shopId"></param>
        /// <returns></returns>
        [HttpGet]
        [ProducesResponseType(typeof(DbExchangeRate), (int)HttpStatusCode.OK)]
        [ServiceFilter(typeof(AuthActionFilter))]
        public async Task<IActionResult> GetExchangeRate(int shopId)
        {
            return await ExecuteAsync(shopId, false,
                async () => await _shopServiceHandler.GetExchangeRate(shopId));
        }

    }
}