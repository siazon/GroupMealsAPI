using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using App.Domain.Common;
using App.Domain.Common.Auth;
using App.Domain.Common.Customer;
using App.Domain.Config;
using App.Infrastructure.ServiceHandler.Common;
using App.Infrastructure.Utility.Common;
using KingfoodIO.Application.Filter;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;

namespace KingfoodIO.Controllers.Common
{
    [Route("api/[controller]/[action]")]
    public class MsgPusherController : BaseController
    {
        private readonly IMsgPusherServiceHandler _msgPusherServiceHandler;
        ILogManager logger;
        IMemoryCache _memoryCache;
        public MsgPusherController(IOptions<CacheSettingConfig> cachesettingConfig, IMemoryCache memoryCache, IRedisCache redisCache,
            IMsgPusherServiceHandler msgPusherServiceHandler, ILogManager logger) : base(cachesettingConfig, memoryCache, redisCache, logger)
        {
            this.logger = logger;
            _memoryCache = memoryCache;
            _msgPusherServiceHandler = msgPusherServiceHandler;
        }

        /// <summary>
        /// status 0:新消息，1:已读消息
        /// MsgType: 0-6 Text, Order, Restaurant, User, OrderHistory, Country, Pement,先只处理0和1
        /// </summary>
        /// <param name="shopId"></param>
        /// <returns></returns>
        [HttpGet]
        [ProducesResponseType(typeof(List<PushMsgModel>), (int)HttpStatusCode.OK)]
        //[ServiceFilter(typeof(AuthActionFilter))]
        public async Task<IActionResult> ListMsgs(int shopId)
        {
            var authHeader = Request.Headers["Wauthtoken"];
            DbToken user = new DbToken() { UserId=null};
            if (!StringValues.IsNullOrEmpty(authHeader))
                user = new TokenEncryptorHelper().Decrypt<DbToken>(authHeader);
            return await ExecuteAsync(shopId, false,
                async () => await _msgPusherServiceHandler.ListMsgs(shopId, user));
        }
        /// <summary>
        /// Status 0:新消息，1:已读消息
        /// MsgType: 0-7 Text, Order, Restaurant, User, OrderHistory, Country, Peyment,UnAcceptOrder,AcceptOrder,先只处理0和1
        /// </summary>
        /// <param name="msg"></param>
        /// <param name="shopId"></param>
        /// <returns></returns>
        [HttpPost]
        [ProducesResponseType(typeof(PushMsgModel), (int)HttpStatusCode.OK)]
        [ServiceFilter(typeof(AuthActionFilter))]
        public async Task<IActionResult> AddMsg([FromBody] PushMsgModel msg, int shopId)
        {
            return await ExecuteAsync(shopId, false,
                async () => await _msgPusherServiceHandler.AddMsg(msg));
        }

        [HttpPost]
        [ProducesResponseType(typeof(PushMsgModel), (int)HttpStatusCode.OK)]
        [ServiceFilter(typeof(AuthActionFilter))]
        public async Task<IActionResult> UpdateMsg([FromBody] PushMsgModel msg, int shopId)
        {
            return await ExecuteAsync(shopId, false,
                async () => await _msgPusherServiceHandler.UpdateMsg(msg));
        }


        [HttpGet]
        [ProducesResponseType(typeof(bool), (int)HttpStatusCode.OK)]
        [ServiceFilter(typeof(AuthActionFilter))]
        public async Task<IActionResult> DeleteMsg(string id, int shopId)
        {
            return await ExecuteAsync(shopId, false,
                async () => await _msgPusherServiceHandler.DeleteMsg(id));
        }


        /// <summary>
        /// 更新消息状态
        /// </summary>
        /// <param name="id"></param>
        /// <param name="status">0:新消息，1:已读消息</param>
        /// <param name="shopId"></param>
        /// <returns></returns>
        [HttpGet]
        [ProducesResponseType(typeof(bool), (int)HttpStatusCode.OK)]
        [ServiceFilter(typeof(AuthActionFilter))]
        public async Task<IActionResult> TagMsg(string id, int status, int shopId)
        {
            return await ExecuteAsync(shopId, false,
                async () => await _msgPusherServiceHandler.TagMsg(id, status));
        }
    }
}