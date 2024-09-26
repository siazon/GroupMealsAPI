using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using App.Domain.Common.Customer;
using App.Domain.Common.Shop;
using App.Domain.TravelMeals.Restaurant;
using App.Infrastructure.ServiceHandler.Common;
using App.Infrastructure.ServiceHandler.Tour;
using App.Infrastructure.ServiceHandler.TravelMeals;
using App.Infrastructure.Utility.Common;
using KingfoodIO.Application.Filter;
using Microsoft.AspNetCore.Mvc;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace KingfoodIO.Controllers
{
    [Route("api/[controller]/[action]")]
    public class TestController : Controller
    {
        private readonly IShopServiceHandler _shopServiceHandler;
        private ITrRestaurantBookingServiceHandler _trRestaurantBookingServiceHandler;
        ITwilioUtil _twilioUtil;
        ILogManager _logger;

        public TestController(ITwilioUtil twilioUtil, ILogManager logger,ITrRestaurantBookingServiceHandler trRestaurantBookingServiceHandler, IShopServiceHandler shopServiceHandler)
        {
            _shopServiceHandler = shopServiceHandler;
            _trRestaurantBookingServiceHandler= trRestaurantBookingServiceHandler;
            _logger = logger;
            _twilioUtil= twilioUtil;
        }

        [HttpGet("ToGet/{id}")]
        [ProducesResponseType(typeof(DbShop), (int) HttpStatusCode.OK)]
        public async Task<IActionResult> Get2(string id)
        {
            _logger.LogInfo("get2:" + id);
            _trRestaurantBookingServiceHandler.ResendEmail(id);
            return Ok();
        }
        [ServiceFilter(typeof(AuthActionFilter))]
        [HttpGet]
        public IEnumerable<string> Get()
        {
            return new[] {"value1", "value2"};
        }

        // GET api/values/5
        [HttpGet("{id}")]
        public string Get(int id)
        {
            return "value";
        }

        // POST api/values
        [HttpPost]
        public void Post([FromBody] string value)
        {
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="logInfo">[当地时间]_[操作人邮箱]_[关键信息如字段名字段内容]</param>
        /// <returns></returns>
        [HttpPost]
        public string DebugLog([FromBody] string logInfo)
        {
            _logger.LogDebug("FontEnd.Log: " + logInfo);
            return "value";
        }

       
        // PUT api/values/5
        [HttpPut("{id}")]
        public void Put(int id, [FromBody] string value)
        {
        }

        // DELETE api/values/5
        [HttpDelete("{id}")]
        public void Delete(int id)
        {
        }
    }
}