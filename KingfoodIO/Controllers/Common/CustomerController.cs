using System;
using System.Collections.Generic;
using System.Net;
using System.Security.Claims;
using System.Threading.Tasks;
using App.Domain.Common.Customer;
using App.Domain.Config;
using App.Infrastructure.ServiceHandler.Common;
using App.Infrastructure.Utility.Common;
using KingfoodIO.Application.Filter;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Documents;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;

namespace KingfoodIO.Controllers.Common
{
    [Route("api/[controller]/[action]")]
    public class CustomerController : BaseController
    {
        private readonly ICustomerServiceHandler _customerServiceHandler;
        JwtTokenConfig _jwtConfig;
        ILogManager logger;
        IMemoryCache _memoryCache;
        public CustomerController(IOptions<CacheSettingConfig> cachesettingConfig, IMemoryCache memoryCache, IRedisCache redisCache, JwtTokenConfig jwtConfig,
        ICustomerServiceHandler customerServiceHandler, ILogManager logger) : base(cachesettingConfig, memoryCache, redisCache, logger)
        {
            this.logger = logger;
            _memoryCache= memoryCache;
            _customerServiceHandler = customerServiceHandler;
            _jwtConfig = jwtConfig;
        }

        [HttpGet]
        [ProducesResponseType(typeof(List<DbCustomer>), (int)HttpStatusCode.OK)]
        [ServiceFilter(typeof(AuthActionFilter))]
        public async Task<IActionResult> ListCustomers(int shopId)
        {
            return await ExecuteAsync(shopId, false,
                async () => await _customerServiceHandler.List(shopId));
        }
        [HttpGet]
        [ProducesResponseType(typeof(DbCustomer), (int)HttpStatusCode.OK)]
        [ServiceFilter(typeof(AuthActionFilter))]
        public async Task<object> LoginCustomer(string email, string password, int shopId)
        {
            DbCustomer customer = null;
            string token = "";
            try
            {

                customer = await _customerServiceHandler.LoginCustomer(email, password, shopId);
            var claims = new[]
            {
                   new Claim("userName", customer.ContactName??""),
                   new Claim("account", customer.Email??""),
                   new Claim("age", customer.Phone??""),
            };
            token = new KingfoodIO.Common.JwtAuthManager(_jwtConfig).GenerateTokens(email, claims);
            Response.Cookies.Append("token", token);
            }
            catch (Exception ex)
            {
                logger.LogError(ex.Message);
                return new { msg = "User name or Password is incorrect!(用户名密码错误)", data = customer, token };

            }
            return new { msg = "ok", data = customer,  token };
        }

        [HttpGet]
        [ProducesResponseType(typeof(DbCustomer), (int)HttpStatusCode.OK)]
        [ServiceFilter(typeof(AuthActionFilter))]
        public async Task<IActionResult> ForgetPassword(string email, int shopId)
        {
            return await ExecuteAsync(shopId, false,
                async () => await _customerServiceHandler.ForgetPassword(email, shopId));
        }

        [HttpGet]
        [ProducesResponseType(typeof(DbCustomer), (int)HttpStatusCode.OK)]
        [ServiceFilter(typeof(AuthActionFilter))]
        public async Task<IActionResult> ResetPassword(string email, string resetCode, string password, int shopId)
        {
            return await ExecuteAsync(shopId, false,
                async () => await _customerServiceHandler.ResetPassword(email, resetCode, password, shopId));
        }

        [HttpGet]
        [ProducesResponseType(typeof(object), (int)HttpStatusCode.OK)]
        [ServiceFilter(typeof(AuthActionFilter))]
        public async Task<IActionResult> VerityEmail(string email, string id, int shopId)
        {
            return await ExecuteAsync(shopId, false,
                async () => await _customerServiceHandler.VerityEmail(email, id, shopId));
        }

        [HttpPost]
        [ProducesResponseType(typeof(object), (int)HttpStatusCode.OK)]
        [ServiceFilter(typeof(AuthActionFilter))]
        public async Task<IActionResult> RegisterAccount([FromBody] DbCustomer customer, int shopId)
        {
            return await ExecuteAsync(shopId, false,
                async () => await _customerServiceHandler.RegisterAccount(customer, shopId));
        }

        [HttpPost]
        [ProducesResponseType(typeof(DbCustomer), (int)HttpStatusCode.OK)]
        [ServiceFilter(typeof(AuthActionFilter))]
        public async Task<IActionResult> UpdateAccount([FromBody] DbCustomer customer, int shopId)
        {
            return await ExecuteAsync(shopId, false,
                async () => await _customerServiceHandler.UpdateAccount(customer, shopId));
        }

        [HttpPost]
        [ProducesResponseType(typeof(DbCustomer), (int)HttpStatusCode.OK)]
        [ServiceFilter(typeof(AuthActionFilter))]
        public async Task<IActionResult> UpdatePassword([FromBody] DbCustomer customer, int shopId)
        {
            return await ExecuteAsync(shopId, false,
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