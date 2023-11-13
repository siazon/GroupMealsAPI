using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using App.Domain.Common.Auth;
using App.Domain.Common.Customer;
using App.Domain.Config;
using App.Infrastructure.ServiceHandler.Common;
using App.Infrastructure.Utility.Common;
using KingfoodIO.Application.Filter;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace KingfoodIO.Controllers.Common
{
    [Route("api/[controller]/[action]")]
    public class AuthController : BaseController
    {
        private readonly IAuthServiceHandler _authServiceHandler;
        ILogManager logger;
        IMemoryCache _memoryCache;
        public AuthController(IOptions<CacheSettingConfig> cachesettingConfig, IMemoryCache memoryCache, IRedisCache redisCache,
            IAuthServiceHandler authServiceHandler, ILogManager logger) : base(cachesettingConfig, memoryCache, redisCache, logger)
        {
            this.logger= logger;
            _memoryCache= memoryCache;
            _authServiceHandler = authServiceHandler;
        }

        [HttpGet]
        [ProducesResponseType(typeof(List<DbCustomer>), (int)HttpStatusCode.OK)]
        [ServiceFilter(typeof(AuthActionFilter))]
        public async Task<IActionResult> ListCustomers( int shopId)
        {
            return await ExecuteAsync(shopId, false,
                async () => await _authServiceHandler.List( shopId));
        }
        [HttpGet]
        [ProducesResponseType(typeof(List<DbCustomer>), (int)HttpStatusCode.OK)]
        [ServiceFilter(typeof(AuthActionFilter))]
        public async Task<IActionResult> ListMenus(int shopId)
        {
            return await ExecuteAsync(shopId, false,
                async () => await _authServiceHandler.ListMenus(shopId));
        }


        [HttpGet]
        [ProducesResponseType(typeof(List<DbCustomer>), (int)HttpStatusCode.OK)]
        [ServiceFilter(typeof(AuthActionFilter))]
        public async Task<IActionResult> ListRoles(int shopId)
        {
            return await ExecuteAsync(shopId, false,
                async () => await _authServiceHandler.ListRoles(shopId));
        }

        [HttpPost]
        [ProducesResponseType(typeof(Menu), (int)HttpStatusCode.OK)]
        [ServiceFilter(typeof(AuthActionFilter))]
        public async Task<IActionResult> AddMenu([FromBody] Menu menu, int shopId)
        {
            return await ExecuteAsync(shopId, false,
                async () => await _authServiceHandler.AddMenu(menu));
        }

        [HttpPost]
        [ProducesResponseType(typeof(Menu), (int)HttpStatusCode.OK)]
        [ServiceFilter(typeof(AuthActionFilter))]
        public async Task<IActionResult> UpdateMenu([FromBody] Menu menu, int shopId)
        {
            return await ExecuteAsync(shopId, false,
                async () => await _authServiceHandler.UpdateMenu(menu));
        }

        [HttpPost]
        [ProducesResponseType(typeof(Role), (int)HttpStatusCode.OK)]
        [ServiceFilter(typeof(AuthActionFilter))]
        public async Task<IActionResult> AddRole([FromBody] Role role, int shopId)
        {
            return await ExecuteAsync(shopId, false,
                async () => await _authServiceHandler.AddRole(role));
        }

        [HttpPost]
        [ProducesResponseType(typeof(Role), (int)HttpStatusCode.OK)]
        [ServiceFilter(typeof(AuthActionFilter))]
        public async Task<IActionResult> UpdateRole([FromBody] Role role, int shopId)
        {
            return await ExecuteAsync(shopId, false,
                async () => await _authServiceHandler.UpdateRole(role));
        }

        [HttpPost]
        [ProducesResponseType(typeof(DbCustomer), (int) HttpStatusCode.OK)]
        [ServiceFilter(typeof(AuthActionFilter))]
        public async Task<IActionResult> UpdateAccount([FromBody] DbCustomer customer, int shopId)
        {
            return await ExecuteAsync( shopId, false,
                async () => await _authServiceHandler.UpdateAccount(customer, shopId));
        }
        
   

        [ServiceFilter(typeof(AdminAuthFilter))]
        [HttpDelete]
        [ProducesResponseType(typeof(DbCustomer), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> Delete(DbCustomer item, int shopId)
        {
            return await ExecuteAsync(shopId, false,
                async () => await _authServiceHandler.Delete(item, shopId));
        }
    }
}