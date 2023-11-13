using App.Domain.Config;
using App.Domain.Holiday;
using App.Infrastructure.ServiceHandler.Tour;
using App.Infrastructure.Utility.Common;
using KingfoodIO.Application.Filter;
using KingfoodIO.Controllers.Common;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
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
        private IHostingEnvironment _environment;

        IMemoryCache _memoryCache;

        public TourController(IOptions<CacheSettingConfig> cachesettingConfig, IMemoryCache memoryCache, IRedisCache redisCache,
            IHostingEnvironment environment, ILogManager logger, ITourServiceHandler tourServiceHandler) :
            base(cachesettingConfig, memoryCache, redisCache, logger)
        {
            _tourServiceHandler = tourServiceHandler;
            _memoryCache = memoryCache;
            _environment = environment;
        }

        [HttpGet]
        [ServiceFilter(typeof(AuthActionFilter))]
        [ProducesResponseType(typeof(List<App.Domain.Holiday.DbTour>), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> ListTours(int shopId, bool cache = true)
        {

            return await ExecuteAsync(shopId, cache,
                async () => await _tourServiceHandler.ListTours(shopId));
        }
        [HttpGet]
        [ServiceFilter(typeof(AuthActionFilter))]
        [ProducesResponseType(typeof(bool), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetTourById(int shopId, string id, bool cache = true)
        {
            return await ExecuteAsync(shopId, cache,
                async () => await _tourServiceHandler.GetTourById(id));
        }
        [HttpPost]
        [ProducesResponseType(typeof(DbTour), (int)HttpStatusCode.OK)]
        [ServiceFilter(typeof(AuthActionFilter))]
        public async Task<IActionResult> CreateTour([FromBody] DbTour tour, int shopId)
        {
            return await ExecuteAsync(shopId, false,
                async () => await _tourServiceHandler.CreateTour(shopId));
        }
        [HttpPost]
        [ProducesResponseType(typeof(DbTour), (int)HttpStatusCode.OK)]
        [ServiceFilter(typeof(AuthActionFilter))]
        public async Task<IActionResult> UpdateTour([FromBody] DbTour tour, int shopId)
        {
            return await ExecuteAsync(shopId, false,
                async () => await _tourServiceHandler.UpdateTour(tour, shopId));
        }
    }
}