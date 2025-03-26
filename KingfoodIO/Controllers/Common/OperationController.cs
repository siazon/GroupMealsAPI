using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System;
using App.Domain.Config;
using App.Infrastructure.ServiceHandler.Common;
using App.Infrastructure.Utility.Common;
using Microsoft.Extensions.Options;
using System.Diagnostics;
using System.Net.Http;
using System.Net;
using App.Domain.Common.Shop;
using KingfoodIO.Application.Filter;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.WebUtilities;
using Twilio.TwiML.Voice;
using Stream = System.IO.Stream;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Processing;
using App.Infrastructure.ServiceHandler.TravelMeals;
using App.Domain.TravelMeals.VO;
using FirebaseAdmin.Messaging;
using App.Domain.Common;
using Twilio.TwiML.Messaging;
using Newtonsoft.Json;
using App.Domain.Common.Auth;
using App.Domain.Common.Customer;
using App.Domain.TravelMeals;

namespace KingfoodIO.Controllers.Common
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class OperationController : BaseController
    {
        IOperationServiceHandler _operationServiceHandler;
        IMemoryCache _memoryCache;
        private readonly AzureStorageConfig storageConfig;
        ILogManager logger;
        public OperationController(IOptions<CacheSettingConfig> cachesettingConfig, IMemoryCache memoryCache, IRedisCache redisCache,  
        IUtilServiceHandler utilServiceHandler, IOptions<AzureStorageConfig> _storageConfig, ILogManager logger, IOperationServiceHandler operationServiceHandler) : base(cachesettingConfig, memoryCache, redisCache, logger)
        {
            this.logger = logger;
            _memoryCache = memoryCache;
            storageConfig = _storageConfig.Value;
            _operationServiceHandler = operationServiceHandler;
        }

        [HttpGet]
        [ProducesResponseType(typeof(DbOpearationInfo), (int)HttpStatusCode.OK)]
        [ServiceFilter(typeof(AuthActionFilter))]
        public async Task<IActionResult> GetOpearations(int shopId,string id)
        {
            //var authHeader = Request.Headers["Wauthtoken"];
            //string userId = "";
            //if (!string.IsNullOrWhiteSpace(authHeader))
            //    userId = new TokenEncryptorHelper().Decrypt<DbToken>(authHeader).UserId;
            return await ExecuteAsync(shopId, false,
                async () => await _operationServiceHandler.GetOpearationsformat( id));
        }
    }
}
