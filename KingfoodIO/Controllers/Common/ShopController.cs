using App.Domain.Common.Shop;
using App.Domain.Config;
using App.Domain.TravelMeals;
using App.Infrastructure.ServiceHandler.Common;
using App.Infrastructure.ServiceHandler.TravelMeals;
using App.Infrastructure.Utility.Common;
using KingfoodIO.Application.Filter;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Net;
using System.Threading.Tasks;

namespace KingfoodIO.Controllers.Common
{
    [Route("api/[controller]/[action]")]
    public class ShopController : BaseController
    {
        private readonly IShopServiceHandler _shopServiceHandler;

        public ShopController(IShopServiceHandler shopServiceHandler, IOptions<CacheSettingConfig> cachesettingConfig,
            IRedisCache redisCache, ILogManager logger) : base(cachesettingConfig, redisCache, logger)
        {
            _shopServiceHandler = shopServiceHandler;
        }

        [ServiceFilter(typeof(AuthActionFilter))]
        [HttpGet]
        [ProducesResponseType(typeof(DbShop), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetShopInfo(int shopId, bool cache = true)
        {
            return await ExecuteAsync(shopId, cache,
                async () => await _shopServiceHandler.GetShopInfo(shopId));
        }
       
    }
}