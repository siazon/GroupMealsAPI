using App.Domain.Config;
using App.Domain.Holiday;
using App.Infrastructure.ServiceHandler.Tour;
using App.Infrastructure.Utility.Common;
using KingfoodIO.Application.Filter;
using KingfoodIO.Controllers.Common;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;

namespace KingfoodIO.Controllers.Tour
{
    [Route("api/[controller]/[action]")]
    public class TourController : BaseController
    {
        private readonly ITourServiceHandler _tourServiceHandler;

        public TourController(
            IOptions<CacheSettingConfig> cachesettingConfig, IRedisCache redisCache, ILogManager logger, ITourServiceHandler tourServiceHandler) : base(cachesettingConfig, redisCache, logger)
        {
            _tourServiceHandler = tourServiceHandler;
        }

        [HttpGet]
        [ServiceFilter(typeof(AuthActionFilter))]
        [ProducesResponseType(typeof(List<App.Domain.Holiday.Tour>), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> ListTours(int shopId, bool cache = true)
        {
            return await ExecuteAsync(shopId, cache,
                async () => await _tourServiceHandler.ListTours(shopId));
        }

        [HttpPost]
        [ProducesResponseType(typeof(TourBooking), (int)HttpStatusCode.OK)]
        [ServiceFilter(typeof(AuthActionFilter))]
        public async Task<IActionResult> RequestBooking([FromBody] TourBooking booking, int shopId)
        {
            return await ExecuteAsync(shopId, false,
                async () => await _tourServiceHandler.RequestBooking(booking, shopId));
        }
    }
}