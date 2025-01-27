using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using App.Domain.Common.Auth;
using App.Domain.Common.Shop;
using App.Domain.Config;
using App.Domain.Enum;
using App.Infrastructure.Exceptions;
using App.Infrastructure.Repository;
using App.Infrastructure.Utility.Common;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;

namespace KingfoodIO.Application.Filter
{
    public class RestaurantAuthFilter : Attribute, IActionFilter
    {
        private readonly IDbCommonRepository<DbShop> _shopRepository;
        private readonly AppSettingConfig _appsettingConfig;
        private IMemoryCache _memoryCache;

        public RestaurantAuthFilter(IDbCommonRepository<DbShop> shopRepository, IOptions<AppSettingConfig> appsettingConfig, IMemoryCache memoryCache)
        {
            _shopRepository = shopRepository;
            _appsettingConfig = appsettingConfig.Value;
            _memoryCache = memoryCache;
        }

        public void OnActionExecuting(ActionExecutingContext context)
        {
            context.HttpContext.Request.Headers.TryGetValue("WAuthToken", out StringValues token);

            if (StringValues.IsNullOrEmpty(token))
                throw new AuthException("Unauthorized User");

            if (!ValidateToken(token[0]))
                throw new AuthException("Unauthorized User");
        }

        private bool ValidateToken(string token)
        {
            var accesstoken = new TokenEncryptorHelper().Decrypt<DbToken>(token);
            if (accesstoken?.ExpiredTime == null || accesstoken.ExpiredTime.Value < DateTime.UtcNow)
                return false;
            if (string.IsNullOrEmpty(accesstoken.ServerKey))
                return false;

            //Validate token against super key

            var masterToken = _appsettingConfig.ShopAuthKey;
            if (masterToken != accesstoken.ServerKey)
                return false;
            bool IsAdmin = accesstoken.RoleLevel.AuthVerify((ulong)AuthEnum.Restaurant);
            return IsAdmin;

            //Check if shop id = shop key
            List<DbShop> cacheShop;
            if (!_memoryCache.TryGetValue(CacheKeysEnum.AuthShop, out cacheShop))
            {
                var task = Task.Run(async () => await _shopRepository.GetManyAsync(r => r.IsActive.HasValue && r.IsActive.Value));

                cacheShop = task.Result.ToList();
                if (cacheShop.Count > 0)
                {
                    var cacheEntryOptions = new MemoryCacheEntryOptions()
                        .SetSlidingExpiration(TimeSpan.FromMinutes(15));

                    _memoryCache.Set(CacheKeysEnum.AuthShop, cacheShop, cacheEntryOptions);
                }
            }

            if (!cacheShop.Any(r => r.AdminTokenKey == accesstoken.ServerKey && r.ShopId == accesstoken.ShopId))
            {
                return false;
            }

            return true;
        }

        public void OnActionExecuted(ActionExecutedContext context)
        {
            // Do something after the action executes.
        }
    }
}