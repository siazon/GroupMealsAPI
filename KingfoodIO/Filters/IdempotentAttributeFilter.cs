using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using Newtonsoft.Json;
using System;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Primitives;
using System.Threading.Tasks;
using System.Threading;
using KingfoodIO.Common;
using App.Infrastructure.Utility.Common;

namespace KingfoodIO.Filters
{
    public class IdempotentAttributeFilter : IActionFilter, IResultFilter
    {
        private readonly IMemoryCache _memoryCache;
        private bool _isIdempotencyCache = false;
        const string IdempotencyKeyHeaderName = "IdempotencyKey";
        private string _idempotencyKey;
        public IdempotentAttributeFilter(IMemoryCache memoryCache)
        {
            _memoryCache = memoryCache;
        }

        public async void OnActionExecuting(ActionExecutingContext context)
        {

            context.HttpContext.Request.Headers.TryGetValue("WAuthToken", out StringValues token);

            string rawRequestBody = await context.HttpContext.Request.GetRawBodyAsync();
            string bodyMd5 = rawRequestBody.CreateMD5();
            //Microsoft.Extensions.Primitives.StringValues idempotencyKeys;
            //context.HttpContext.Request.Headers.TryGetValue(IdempotencyKeyHeaderName, out idempotencyKeys);
            _idempotencyKey = context.HttpContext.Request.Path + context.HttpContext.Request.Method; // idempotencyKeys.ToString();

            var cacheData = _memoryCache.Get (GetDistributedCacheKey())?.ToString();
            //_distributedCache.Remove(GetDistributedCacheKey());
            if (cacheData != null&&cacheData==token+bodyMd5)
            {
                _isIdempotencyCache = true;
                 context.Result = new ContentResult { StatusCode = 501, Content = "Duplicate requests" }; return; 
            }
            else {
                _memoryCache.Set<string>(GetDistributedCacheKey(), token+ bodyMd5);

                var cacheData9 = _memoryCache.Get(GetDistributedCacheKey())?.ToString();
                Task.Run(() =>
                {
                    Thread.Sleep(3000);
                    _memoryCache.Set<string>(GetDistributedCacheKey(), null);
                });
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
            var cacheData = _memoryCache.Get(GetDistributedCacheKey())?.ToString();
            //缓存:
            //_memoryCache.Set<string>(GetDistributedCacheKey(),null);
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
