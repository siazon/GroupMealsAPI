using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using System;

namespace KingfoodIO.Filters
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    public class IdempotentAttribute : Attribute, IFilterFactory
    {
        public bool IsReusable => false;

        public IFilterMetadata CreateInstance(IServiceProvider serviceProvider)
        {
            var distributedCache = (IMemoryCache)serviceProvider.GetService(typeof(IMemoryCache));

            var filter = new IdempotentAttributeFilter(distributedCache);
            return filter;
        }
    }
}
