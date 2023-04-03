using System.Net;
using System.Threading.Tasks;
using App.Domain.Common.Customer;
using App.Domain.Config;
using App.Infrastructure.ServiceHandler.Common;
using App.Infrastructure.Utility.Common;
using KingfoodIO.Application.Filter;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace KingfoodIO.Controllers.Common
{
    [Route("api/[controller]/[action]")]
    public class CustomerController : BaseController
    {
        private readonly ICustomerServiceHandler _customerServiceHandler;
        ILogManager logger;
        public CustomerController(IOptions<CacheSettingConfig> cachesettingConfig, IRedisCache redisCache,
            ICustomerServiceHandler customerServiceHandler, ILogManager logger) : base(cachesettingConfig, redisCache, logger)
        {
            this.logger= logger;
            _customerServiceHandler = customerServiceHandler;
        }


        [HttpGet]
        [ProducesResponseType(typeof(DbCustomer), (int) HttpStatusCode.OK)]
        [ServiceFilter(typeof(AuthActionFilter))]
        public async Task<IActionResult> LoginCustomer(string email, string password, int shopId)
        {
            logger.LogDebug("test:"+email+password+shopId);
            return await ExecuteAsync( shopId, false,
                async () => await _customerServiceHandler.LoginCustomer(email, password, shopId));
        }

        [HttpGet]
        [ProducesResponseType(typeof(DbCustomer), (int) HttpStatusCode.OK)]
        [ServiceFilter(typeof(AuthActionFilter))]
        public async Task<IActionResult> ForgetPassword(string email, int shopId)
        {
            return await ExecuteAsync(shopId, false,
                async () => await _customerServiceHandler.ForgetPassword(email, shopId));
        }

        [HttpGet]
        [ProducesResponseType(typeof(DbCustomer), (int) HttpStatusCode.OK)]
        [ServiceFilter(typeof(AuthActionFilter))]
        public async Task<IActionResult> ResetPassword(string email, string resetCode, string password, int shopId)
        {
            return await ExecuteAsync( shopId, false,
                async () => await _customerServiceHandler.ResetPassword(email, resetCode, password, shopId));
        }

        [HttpPost]
        [ProducesResponseType(typeof(DbCustomer), (int) HttpStatusCode.OK)]
        [ServiceFilter(typeof(AuthActionFilter))]
        public async Task<IActionResult> RegisterAccount([FromBody] DbCustomer customer, int shopId)
        {
            return await ExecuteAsync( shopId, false,
                async () => await _customerServiceHandler.RegisterAccount(customer, shopId));
        }
        
        [HttpPost]
        [ProducesResponseType(typeof(DbCustomer), (int) HttpStatusCode.OK)]
        [ServiceFilter(typeof(AuthActionFilter))]
        public async Task<IActionResult> UpdateAccount([FromBody] DbCustomer customer, int shopId)
        {
            return await ExecuteAsync( shopId, false,
                async () => await _customerServiceHandler.UpdateAccount(customer, shopId));
        }
        
        [HttpPost]
        [ProducesResponseType(typeof(DbCustomer), (int) HttpStatusCode.OK)]
        [ServiceFilter(typeof(AuthActionFilter))]
        public async Task<IActionResult> UpdatePassword([FromBody] DbCustomer customer, int shopId)
        {
            return await ExecuteAsync( shopId, false,
                async () => await _customerServiceHandler.UpdatePassword(customer, shopId));
        }


        [ServiceFilter(typeof(AdminAuthFilter))]
        [HttpDelete]
        [ProducesResponseType(typeof(DbCustomer), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> Delete(DbCustomer item, int shopId)
        {
            return await ExecuteAsync(shopId, false,
                async () => await _customerServiceHandler.Delete(item, shopId));
        }
    }
}