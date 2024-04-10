using System;
using System.Collections.Generic;
using System.Net;
using System.Security.Claims;
using System.Threading.Tasks;
using App.Domain.Common.Auth;
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
        AppSettingConfig _appsettingConfig;
        ILogManager logger;
        IMemoryCache _memoryCache;
        public CustomerController(IOptions<CacheSettingConfig> cachesettingConfig, IOptions<AppSettingConfig> appsettingConfig,
            IMemoryCache memoryCache, IRedisCache redisCache,
        ICustomerServiceHandler customerServiceHandler, ILogManager logger) : base(cachesettingConfig, memoryCache, redisCache, logger)
        {
            this.logger = logger;
            _memoryCache = memoryCache;
            _customerServiceHandler = customerServiceHandler;
            _appsettingConfig = appsettingConfig.Value;
        }

        [HttpGet]
        [ProducesResponseType(typeof(List<DbCustomer>), (int)HttpStatusCode.OK)]
        //[ServiceFilter(typeof(AuthActionFilter))]
        public async Task<IActionResult> ListCustomers(int shopId)
        {
            return await ExecuteAsync(shopId, false,
                async () => await _customerServiceHandler.List(shopId));
        }
        [HttpGet]
        [ProducesResponseType(typeof(DbCustomer), (int)HttpStatusCode.OK)]
        //[ServiceFilter(typeof(AuthActionFilter))]
        public async Task<object> Login(string email, string password, int shopId)
        {
            DbCustomer customer = null;
            string token = "";
            try
            {
                customer = await _customerServiceHandler.LoginCustomer(email, password, shopId);
                if (customer == null)
                {
                    return new { msg = "User not found!(用户不存在)", data = customer, token };
                }
                else if (!customer.IsVerity) {
                    return new { msg = "Email is not verified!(邮箱未验证)", data = customer, token };
                }
                customer= customer.ClearForOutPut();
                DbToken dbToken = new DbToken()
                {
                    ShopId = shopId,
                    ExpiredTime = DateTime.Now.AddYears(1),
                    ServerKey = _appsettingConfig.ShopAuthKey,
                    UserId = customer.Id,
                    UserName = customer.UserName,
                    UserEmail = customer.Email,
                    IsActive = customer.IsActive,
                    RoleLevel = customer.AuthValue
                };
                token = new TokenEncryptorHelper().Encrypt(dbToken);// new KingfoodIO.Common.JwtAuthManager(_jwtConfig).GenerateTokens(email, claims);
                var claims = new[]
                {
                   new Claim("ServerKey",_appsettingConfig.ShopAuthKey),
                   new Claim("UserId", customer.Id),
                   new Claim("UserName", customer.UserName),
                   new Claim("RoleLevel", customer.AuthValue+""),
                };
                var userIdentity = new ClaimsIdentity(claims, ClaimTypes.Name);
                Request.HttpContext.User = new ClaimsPrincipal(userIdentity);
                Response.Cookies.Append("token", token);
            }
            catch (Exception ex)
            {
                logger.LogError(ex.Message);
                return new { msg = "User name or Password is incorrect!(用户名密码错误)", data = customer, token };

            }
            return new { msg = "ok", data = customer, token };
        }

        [HttpGet]
        [ProducesResponseType(typeof(DbCustomer), (int)HttpStatusCode.OK)]
        //[ServiceFilter(typeof(AuthActionFilter))]
        public async Task<IActionResult> ForgetPassword(string email, int shopId)
        {
            return await ExecuteAsync(shopId, false,
                async () => await _customerServiceHandler.ForgetPassword(email, shopId));
        }

        [HttpGet]
        [ProducesResponseType(typeof(DbCustomer), (int)HttpStatusCode.OK)]
        //[ServiceFilter(typeof(AuthActionFilter))]
        public async Task<IActionResult> ResetPassword(string email, string resetCode, string password, int shopId)
        {
            return await ExecuteAsync(shopId, false,
                async () => await _customerServiceHandler.ResetPassword(email, resetCode, password, shopId));
        }
        [HttpGet]
        [ProducesResponseType(typeof(DbCustomer), (int)HttpStatusCode.OK)]
        //[ServiceFilter(typeof(AuthActionFilter))]
        public async Task<IActionResult> UpdatePassword(string email, string oldPassword, string password, int shopId)
        {
            return await ExecuteAsync(shopId, false,
                async () => await _customerServiceHandler.UpdatePassword(email, oldPassword, password, shopId));
        }

        [HttpGet]
        [ProducesResponseType(typeof(object), (int)HttpStatusCode.OK)]
        //[ServiceFilter(typeof(AuthActionFilter))]
        public async Task<IActionResult> VerityEmail(string email, string customerId, int shopId)
        {
            return await ExecuteAsync(shopId, false,
                async () => await _customerServiceHandler.VerityEmail(email, customerId, shopId));
        }

        [HttpPost]
        [ProducesResponseType(typeof(object), (int)HttpStatusCode.OK)]
        //[ServiceFilter(typeof(AuthActionFilter))]
        public async Task<IActionResult> RegisterAccount([FromBody] DbCustomer customer, int shopId)
        {
            return await ExecuteAsync(shopId, false,
                async () => await _customerServiceHandler.RegisterAccount(customer, shopId));
        }

        [HttpPost]
        [ProducesResponseType(typeof(DbCustomer), (int)HttpStatusCode.OK)]
        //[ServiceFilter(typeof(AuthActionFilter))]
        public async Task<IActionResult> UpdateAccount([FromBody] DbCustomer customer, int shopId)
        {
            return await ExecuteAsync(shopId, false,
                async () => await _customerServiceHandler.UpdateAccount(customer, shopId));
        }


        [HttpPost]
        [ProducesResponseType(typeof(DbCustomer), (int)HttpStatusCode.OK)]
        //[ServiceFilter(typeof(AuthActionFilter))]
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

        [HttpPost]
        [ProducesResponseType(typeof(DbCustomer), (int)HttpStatusCode.OK)]
        [ServiceFilter(typeof(AuthActionFilter))]
        public async Task<IActionResult> UpdateFavorite([FromBody] DbCustomer customer, int shopId)
        {
            return await ExecuteAsync(shopId, false,
                async () => await _customerServiceHandler.UpdateFavorite(customer, shopId));
        }

        [HttpPost]
        [ProducesResponseType(typeof(DbCustomer), (int)HttpStatusCode.OK)]
        [ServiceFilter(typeof(AuthActionFilter))]
        public async Task<IActionResult> UpdateCart([FromBody] DbCustomer customer, int shopId)
        {
            return await ExecuteAsync(shopId, false,
                async () => await _customerServiceHandler.UpdateCart(customer, shopId));
        }

    }
}