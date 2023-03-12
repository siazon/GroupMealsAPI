using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using App.Domain.Common.Shop;
using App.Infrastructure.ServiceHandler.Common;
using Microsoft.AspNetCore.Mvc;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace KingfoodIO.Controllers
{
    [Route("api/[controller]")]
    public class TestController : Controller
    {
        private readonly IShopServiceHandler _shopServiceHandler;

        public TestController(IShopServiceHandler shopServiceHandler)
        {
            _shopServiceHandler = shopServiceHandler;
        }

        [HttpGet("ToGet/{id}")]
        [ProducesResponseType(typeof(DbShop), (int) HttpStatusCode.OK)]
        public async Task<IActionResult> Get2(int id)
        {
            return Ok(await _shopServiceHandler.GetShopInfo(id));
        }

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