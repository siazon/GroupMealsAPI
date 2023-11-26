using App.Domain.Common.Auth;
using App.Domain.Config;
using App.Infrastructure.Exceptions;
using App.Infrastructure.Utility.Common;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using System;
using System.Threading.Tasks;

namespace KingfoodIO.Controllers.Common
{
    public class BaseController : Controller
    {
        private readonly CacheSettingConfig _cachesettingConfig;
        private readonly IRedisCache _redisCache;
        private readonly IMemoryCache _memoryCache;
        private readonly ILogManager _logger;

        public BaseController(IOptions<CacheSettingConfig> cachesettingConfig, IMemoryCache memoryCache, IRedisCache redisCache, ILogManager logger)
        {
            _cachesettingConfig = cachesettingConfig.Value;
            _redisCache = redisCache;
            _memoryCache = memoryCache;
            _logger = logger;
        }

        protected async Task<IActionResult> ExecuteAsync<T>(int shopId, bool cache,Func<Task<T>> action, bool validateShopId = true)
        {
            //if (validateShopId)
            //    ValidateKeyWithShopId(shopId);

            var actionName = ControllerContext.RouteData.Values["action"].ToString();
            var controllerName = ControllerContext.RouteData.Values["controller"].ToString();

            _logger.LogInfo($"Action:{actionName}, Controller:{controllerName}, shopId:{shopId}");

            //Read Cache
            var cacheKey = string.Format("motionmedia-{1}-{2}-{0}", shopId, controllerName, actionName);
            var cacheOn = _cachesettingConfig.CacheOn;

            if (cacheOn && cache)
            {
                var cacheResult = _memoryCache == null ? await _redisCache.Get<T>(cacheKey) : _memoryCache.Get<T>(cacheKey);
                if (cacheResult != null)
                    return Ok(cacheResult);
            }

            //Execute
            var content = await action();

            //Save Cache
            if (cacheOn && cache && content != null)
            {
                if (_memoryCache == null)
                    await _redisCache.Set(cacheKey, content);
                else _memoryCache.Set(cacheKey, content);
            }

            return Ok(content);
        }

        private void ValidateKeyWithShopId(int shopId)
        {
            var authHeader = Request.Headers["Wauthtoken"];
            if (authHeader.Count != 1)
                throw new AuthException("Unauthorized API User");
            var accesstoken = new TokenEncryptorHelper().Decrypt<DbToken>(authHeader[0]);
            //Only 0  and shopid in token is the same as in request
            if (accesstoken.ShopId != 0 && accesstoken.ShopId != shopId)
                throw new AuthException("Unauthorized API User");
        }
    }
}