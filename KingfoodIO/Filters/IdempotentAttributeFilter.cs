using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using Newtonsoft.Json;
using System;

namespace KingfoodIO.Filters
{
    public class IdempotentAttributeFilter : IActionFilter, IResultFilter
    {
        private readonly IDistributedCache _distributedCache;
        private bool _isIdempotencyCache = false;
        const string IdempotencyKeyHeaderName = "IdempotencyKey";
        private string _idempotencyKey;
        public IdempotentAttributeFilter(IDistributedCache distributedCache)
        {
            _distributedCache = distributedCache;
        }

        public void OnActionExecuting(ActionExecutingContext context)
        {
            //Microsoft.Extensions.Primitives.StringValues idempotencyKeys;
            //context.HttpContext.Request.Headers.TryGetValue(IdempotencyKeyHeaderName, out idempotencyKeys);
            _idempotencyKey = context.HttpContext.Request.Path + context.HttpContext.Request.Method; // idempotencyKeys.ToString();

            var cacheData = _distributedCache.GetString(GetDistributedCacheKey());
            //_distributedCache.Remove(GetDistributedCacheKey());
            if (cacheData != null)
            {
                context.Result = JsonConvert.DeserializeObject<ObjectResult>(cacheData);
                _isIdempotencyCache = true;
                return;
            }
        }

        public void OnResultExecuted(ResultExecutedContext context)
        {
            //已缓存
            if (_isIdempotencyCache)
            {
                return;
            }

            var contextResult = context.Result;

            DistributedCacheEntryOptions cacheOptions = new DistributedCacheEntryOptions();
            cacheOptions.AbsoluteExpirationRelativeToNow = new TimeSpan(0, 0, 10);

            //缓存:
            _distributedCache.SetString(GetDistributedCacheKey(), JsonConvert.SerializeObject(contextResult), cacheOptions);
        }

        public void OnActionExecuted(ActionExecutedContext context)
        {
        }

        public void OnResultExecuting(ResultExecutingContext context)
        {
        }

        private string GetDistributedCacheKey()
        {
            return "Idempotency:" + _idempotencyKey;
        }
    }
}
