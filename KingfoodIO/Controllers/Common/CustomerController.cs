using System;
using System.Collections.Generic;
using System.Net;
using System.Security.Claims;
using System.Threading.Tasks;
using App.Domain.Common.Auth;
using App.Domain.Common.Customer;
using App.Domain.Config;
using App.Domain.TravelMeals;
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
    /// <summary>
    /// 
    /// </summary>
    [Route("api/[controller]/[action]")]
    public class CustomerController : BaseController
    {
        private readonly ICustomerServiceHandler _customerServiceHandler;
        AppSettingConfig _appsettingConfig;
        ILogManager logger;
        IMemoryCache _memoryCache;
        IEncryptionHelper _encryptionHelper;
        /// <summary>
        /// 
        /// </summary>
        /// <param name="cachesettingConfig"></param>
        /// <param name="appsettingConfig"></param>
        /// <param name="memoryCache"></param>
        /// <param name="redisCache"></param>
        /// <param name="customerServiceHandler"></param>
        /// <param name="logger"></param>
        public CustomerController(IOptions<CacheSettingConfig> cachesettingConfig, IOptions<AppSettingConfig> appsettingConfig,
            IMemoryCache memoryCache, IRedisCache redisCache, IEncryptionHelper encryptionHelper,
        ICustomerServiceHandler customerServiceHandler, ILogManager logger) : base(cachesettingConfig, memoryCache, redisCache, logger)
        {
            this.logger = logger;
            _memoryCache = memoryCache;
            _customerServiceHandler = customerServiceHandler;
            _appsettingConfig = appsettingConfig.Value;
            _encryptionHelper= encryptionHelper;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="shopId"></param>
        /// <returns></returns>
        [HttpGet]
        [ProducesResponseType(typeof(List<DbCustomer>), (int)HttpStatusCode.OK)]
        //[ServiceFilter(typeof(AuthActionFilter))]
        public async Task<IActionResult> ListCustomers(int shopId)
        {
            return await ExecuteAsync(shopId, false,
                async () => await _customerServiceHandler.List(shopId));
        }
        /// <summary>
        /// AuthValue:0.HomePage, 1.OrdersPage, 2.ContactPage, 3.CartPage, 4.ProfilePage, 5.RestaurantMagPage, 6.authPage, 
        /// </summary>
        /// <param name="email"></param>
        /// <param name="password"></param>
        /// <param name="shopId"></param>
        /// <returns></returns>
        [HttpGet]
        [ProducesResponseType(typeof(DbCustomer), (int)HttpStatusCode.OK)]
        //[ServiceFilter(typeof(AuthActionFilter))]
        public async Task<object> Login(string email, string password, int shopId)
        {
            DbCustomer customer = null;
            string token = "";
            try
            {
                var passwordEncode = _encryptionHelper.EncryptString(password);

                customer = await _customerServiceHandler.LoginCustomer(email, password, shopId);
                if (customer == null)
                {
                    return new { msg = "User not found!(用户不存在)", data = customer, token };
                } else if (customer.Password!=passwordEncode) {
                    return new { msg = "Wrong Password!(密码错误)",  };
                }
                else if (!customer.IsVerity) {
                    return new { msg = "Email is not verified!(邮箱未验证)", data = customer, token };
                }
                customer= customer.ClearForOutPut();
                DbToken dbToken = new DbToken()
                {
                    ShopId = shopId,
                    ExpiredTime = DateTime.UtcNow.AddDays(814),
                    ServerKey = _appsettingConfig.ShopAuthKey,
                    UserId = customer.Id,
                    UserName = customer.UserName,
                    UserEmail = customer.Email,
                    IsActive = customer.IsActive,
                    RoleLevel = customer.AuthValue,
                };
                token = new TokenEncryptorHelper().Encrypt(dbToken);// new KingfoodIO.Common.JwtAuthManager(_jwtConfig).GenerateTokens(email, claims);

                var temo = new TokenEncryptorHelper().Decrypt<DbToken>(token);
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
                Console.WriteLine(ex.Message);
                logger.LogError(ex.Message);
                return new { msg = "User name or Password is incorrect!(用户名密码错误)", data = customer, token };

            }
            return new { msg = "ok", data = customer, token };
        }
        [HttpGet]
        [ProducesResponseType(typeof(DbCustomer), (int)HttpStatusCode.OK)]
        [ServiceFilter(typeof(AuthActionFilter))]
        public async Task<object> CloseAccount(int shopId)
        {
            var authHeader = Request.Headers["Wauthtoken"];
            var user = new TokenEncryptorHelper().Decrypt<DbToken>(authHeader);
            return await ExecuteAsync(shopId, false,
          async () => await _customerServiceHandler.CloseAccount(user.UserId));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="email"></param>
        /// <param name="shopId"></param>
        /// <returns></returns>
        [HttpGet]
        [ProducesResponseType(typeof(DbCustomer), (int)HttpStatusCode.OK)]
        //[ServiceFilter(typeof(AuthActionFilter))]
        public async Task<IActionResult> SendForgetPasswordVerifyCode(string email, int shopId)
        {
            return await ExecuteAsync(shopId, false,
                async () => await _customerServiceHandler.SendForgetPasswordVerifyCode(email, shopId));
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="email"></param>
        /// <param name="resetCode"></param>
        /// <param name="password"></param>
        /// <param name="shopId"></param>
        /// <returns></returns>
        [HttpGet]
        [ProducesResponseType(typeof(DbCustomer), (int)HttpStatusCode.OK)]
        //[ServiceFilter(typeof(AuthActionFilter))]
        public async Task<IActionResult> ResetPassword(string email, string resetCode, string password, int shopId)
        {
            return await ExecuteAsync(shopId, false,
                async () => await _customerServiceHandler.ResetPassword(email, resetCode, password, shopId));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="email"></param>
        /// <param name="shopId"></param>
        /// <returns></returns>
        [HttpGet]
        [ProducesResponseType(typeof(DbCustomer), (int)HttpStatusCode.OK)]
        //[ServiceFilter(typeof(AuthActionFilter))]
        public async Task<IActionResult> SendRegistrationVerityCode(string email,  int shopId)
        {
            return await ExecuteAsync(shopId, false,
                async () => await _customerServiceHandler.SendRegistrationVerityCode(email,   shopId));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="email"></param>
        /// <param name="oldPassword"></param>
        /// <param name="password"></param>
        /// <param name="shopId"></param>
        /// <returns></returns>
        [HttpGet]
        [ProducesResponseType(typeof(DbCustomer), (int)HttpStatusCode.OK)]
        [ServiceFilter(typeof(AuthActionFilter))]
        public async Task<IActionResult> UpdatePassword( string oldPassword, string password, int shopId)
        {
            var authHeader = Request.Headers["Wauthtoken"];
            var user = new TokenEncryptorHelper().Decrypt<DbToken>(authHeader);
            return await ExecuteAsync(shopId, false,
                async () => await _customerServiceHandler.UpdatePassword(user.UserEmail, oldPassword, password, shopId));
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="email"></param>
        /// <param name="customerId"></param>
        /// <param name="shopId"></param>
        /// <returns></returns>
        [HttpGet]
        [ProducesResponseType(typeof(object), (int)HttpStatusCode.OK)]
        //[ServiceFilter(typeof(AuthActionFilter))]
        public async Task<IActionResult> VerityEmail(string email, string customerId, int shopId)
        {
            return await ExecuteAsync(shopId, false,
                async () => await _customerServiceHandler.VerityEmail(email, customerId, shopId));
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="customer"></param>
        /// <param name="shopId"></param>
        /// <returns></returns>
        [HttpPost]
        [ProducesResponseType(typeof(object), (int)HttpStatusCode.OK)]
        //[ServiceFilter(typeof(AuthActionFilter))]
        public async Task<IActionResult> RegisterAccount([FromBody] DbCustomer customer, int shopId)
        {
            return await ExecuteAsync(shopId, false,
                async () => await _customerServiceHandler.RegisterAccount(customer, shopId));
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="customer"></param>
        /// <param name="shopId"></param>
        /// <returns></returns>
        [HttpPost]
        [ProducesResponseType(typeof(DbCustomer), (int)HttpStatusCode.OK)]
        //[ServiceFilter(typeof(AuthActionFilter))]
        public async Task<IActionResult> UpdateAccount([FromBody] DbCustomer customer, int shopId)
        {
            return await ExecuteAsync(shopId, false,
                async () => await _customerServiceHandler.UpdateAccount(customer, shopId));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="customer"></param>
        /// <param name="shopId"></param>
        /// <returns></returns>
        [HttpPost]
        [ProducesResponseType(typeof(DbCustomer), (int)HttpStatusCode.OK)]
        //[ServiceFilter(typeof(AuthActionFilter))]
        public async Task<IActionResult> UpdatePassword([FromBody] DbCustomer customer, int shopId)
        {
            return await ExecuteAsync(shopId, false,
                async () => await _customerServiceHandler.UpdatePassword(customer, shopId));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="item"></param>
        /// <param name="shopId"></param>
        /// <returns></returns>
        [ServiceFilter(typeof(AdminAuthFilter))]
        [HttpPost]
        [ProducesResponseType(typeof(DbCustomer), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> Delete([FromBody] DbCustomer item,string pwd, int shopId)
        {
            var authHeader = Request.Headers["Wauthtoken"];
            var userInfo = new TokenEncryptorHelper().Decrypt<DbToken>(authHeader);
            string email = userInfo.UserEmail;
            return await ExecuteAsync(shopId, false,
                async () => await _customerServiceHandler.Delete(item, email,pwd, shopId));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="customer"></param>
        /// <param name="shopId"></param>
        /// <returns></returns>
        [HttpPost]
        [ProducesResponseType(typeof(DbCustomer), (int)HttpStatusCode.OK)]
        [ServiceFilter(typeof(AuthActionFilter))]
        public async Task<IActionResult> UpdateFavorite([FromBody] DbCustomer customer, int shopId)
        {
            return await ExecuteAsync(shopId, false,
                async () => await _customerServiceHandler.UpdateFavorite(customer, shopId));
        }

        /// <summary>
        /// 更新后会计算金额，保存在amountInfo中，直接取值可以省下单独调计算的接口
        /// </summary>
        /// <param name="cartInfos">此处Id在前端生成GUID,不然后端每次update生成的ID不一样</param>
        /// <param name="shopId"></param>
        /// <returns></returns>
        [HttpPost]
        [ProducesResponseType(typeof(DbCustomer), (int)HttpStatusCode.OK)]
        [ServiceFilter(typeof(AuthActionFilter))]
        public async Task<IActionResult> UpdateCartInfos([FromBody] List<BookingDetail> cartInfos, int shopId)
        {
            var authHeader = Request.Headers["Wauthtoken"];
            var user = new TokenEncryptorHelper().Decrypt<DbToken>(authHeader);
            return await ExecuteAsync(shopId, false,
                async () => await _customerServiceHandler.UpdateCart(cartInfos,user.UserId, shopId));
        }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="shopId"></param>
      /// <returns></returns>
        [HttpGet]
        [ProducesResponseType(typeof(List<BookingDetail>), (int)HttpStatusCode.OK)]
        [ServiceFilter(typeof(AuthActionFilter))]
        public async Task<IActionResult> GetCartInfos(int shopId)
        {
            var authHeader = Request.Headers["Wauthtoken"];
            var user = new TokenEncryptorHelper().Decrypt<DbToken>(authHeader);
            return await ExecuteAsync(shopId, false,
                async () => await _customerServiceHandler.GetCart(user.UserId, shopId));
        }

    }
}