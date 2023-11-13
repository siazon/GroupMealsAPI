using App.Domain.Config;
using App.Domain.Holiday;
using App.Infrastructure.ServiceHandler.Tour;
using App.Infrastructure.Utility.Common;
using KingfoodIO.Application.Filter;
using KingfoodIO.Controllers.Common;
using KingfoodIO.Filters;
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
    public class TourBookingController : BaseController
    {
        private readonly ITourBookingServiceHandler _tourBookingServiceHandler;

        IMemoryCache _memoryCache;
        public TourBookingController(
            IOptions<CacheSettingConfig> cachesettingConfig, IMemoryCache memoryCache, IRedisCache redisCache, ILogManager logger, ITourBookingServiceHandler tourBookingServiceHandler) 
            : base(cachesettingConfig, memoryCache,redisCache, logger)
        {
            _memoryCache = memoryCache;
            _tourBookingServiceHandler = tourBookingServiceHandler;
        }


        [Idempotent]
        [HttpPost]
        [ProducesResponseType(typeof(TourBooking), (int)HttpStatusCode.OK)]
        [ServiceFilter(typeof(AuthActionFilter))]
        public async Task<IActionResult> RequestBooking([FromBody] TourBooking booking, int shopId)
        {
            return await ExecuteAsync(shopId, false,
                async () => await _tourBookingServiceHandler.RequestBooking(booking, shopId));
        }
        [HttpGet]
        [ServiceFilter(typeof(AuthActionFilter))]
        [ProducesResponseType(typeof(TourBooking), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetTourBookingById(int shopId, string id,  bool cache = true)
        {
            return await ExecuteAsync(shopId, cache,
                async () => await _tourBookingServiceHandler.GetTourBooking(id));
        }

        [Idempotent]
        [HttpGet]
        [ServiceFilter(typeof(AuthActionFilter))]
        [ProducesResponseType(typeof(TourBooking), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> BookingRefundApply(int shopId, string id, bool cache = true)
        {
            return await ExecuteAsync(shopId, cache,
                async () => await _tourBookingServiceHandler.TourBookingRefundApply(id));
        }
        [HttpGet]
        [ServiceFilter(typeof(AuthActionFilter))]
        [ProducesResponseType(typeof(List<TourBooking>), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> ListTourBookings(int shopId,string code,string email, bool cache = true)
        {
            return await ExecuteAsync(shopId, cache,
                async () => await _tourBookingServiceHandler.GetTourBookings(code,email));
        }
        [HttpGet]
        [ServiceFilter(typeof(AuthActionFilter))]
        [ProducesResponseType(typeof(List<TourBooking>), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> ListTourBookingsByAdmin(int shopId, string code, bool cache = true)
        {
            return await ExecuteAsync(shopId, cache,
                async () => await _tourBookingServiceHandler.GetTourBookingsByAdmin(code));
        }

        [HttpGet]
        [ServiceFilter(typeof(AuthActionFilter))]
        [ProducesResponseType(typeof(bool), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> DeleteTourBookingsById(int shopId, string id, bool cache = true)
        {
            return await ExecuteAsync(shopId, cache,
                async () => await _tourBookingServiceHandler.DeleteTourBookingById(id));
        }

        [HttpPost]
        [ProducesResponseType(typeof(TourBooking), (int)HttpStatusCode.OK)]
        [ServiceFilter(typeof(AuthActionFilter))]
        public async Task<IActionResult> UpdateBooking([FromBody] TourBooking booking, int shopId)
        {
            return await ExecuteAsync(shopId, false,
                async () => await _tourBookingServiceHandler.UpdateTourBooking(booking));
        }

    }
}