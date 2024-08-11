using System;
using App.Domain.Common.Content;
using App.Domain.Common.Customer;
using App.Domain.Common.Setting;
using App.Domain.Common.Shop;
using App.Infrastructure.Builders.Common;
using App.Infrastructure.Exceptions;
using App.Infrastructure.Repository;
using App.Infrastructure.Utility.Common;
using App.Infrastructure.Validation;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using App.Domain.Enum;
using App.Domain.Common.Auth;
using App.Domain.Common;
using System.Web;

namespace App.Infrastructure.ServiceHandler.Common
{
    public interface IMsgPusherServiceHandler
    {
        Task<List<PushMsgModel>> ListMsgs(int shopId, DbToken user);
        Task<PushMsgModel> AddMsg(PushMsgModel menu);
        Task<PushMsgModel> UpdateMsg(PushMsgModel menu);
        Task<bool> DeleteMsg(string id);
        Task<PushMsgModel> TagMsg(string id,int status);

    }

    public class MsgPusherServiceHandler : IMsgPusherServiceHandler
    {
        private readonly IDbCommonRepository<PushMsgModel> _msgRepository;
        private readonly IEncryptionHelper _encryptionHelper;
        private readonly IDateTimeUtil _dateTimeUtil;
        private readonly IContentBuilder _contentBuilder;
        private readonly IEmailUtil _emailUtil;

        public MsgPusherServiceHandler(IDbCommonRepository<DbCustomer> customerRepository, IEncryptionHelper encryptionHelper, IDateTimeUtil dateTimeUtil,
            IDbCommonRepository<PushMsgModel> msgRepository, IContentBuilder contentBuilder, IEmailUtil emailUtil)
        {
            _encryptionHelper = encryptionHelper;
            _dateTimeUtil = dateTimeUtil;
            _msgRepository = msgRepository;
            _contentBuilder = contentBuilder;
            _emailUtil = emailUtil;
        }

        public async Task<List<PushMsgModel>> ListMsgs(int shopId, DbToken user)
        {
            Guard.GreaterThanZero(shopId);
            var customers = await _msgRepository.GetManyAsync(r => r.ShopId == shopId&&(r.Receiver==""||r.Receiver==user.UserId));

            var returnCustomers = customers.OrderByDescending(r => r.Updated).Take(200);

            return returnCustomers.ToList();
        }


        public async Task<PushMsgModel> AddMsg(PushMsgModel msg)
        {
            Guard.NotNull(msg);
            var savedMenu = await _msgRepository.UpsertAsync(msg);
            return savedMenu;
        }
        public async Task<PushMsgModel> UpdateMsg(PushMsgModel menu)
        {
            Guard.NotNull(menu);
            var savedMenu = await _msgRepository.UpsertAsync(menu);
            return savedMenu;
        }
        public async Task<PushMsgModel> TagMsg(string id,int status)
        {
            var savedMenu = await _msgRepository.GetOneAsync(a => a.Id == id);
            if (savedMenu != null)
            {
                savedMenu.MsgStatus = (MSGEnum)status;
                 savedMenu = await _msgRepository.UpsertAsync(savedMenu);
            }
            return savedMenu;
        }

        public async Task<bool> DeleteMsg(string Id)
        {
            try
            {
                var savedMenu = await _msgRepository.GetOneAsync(a => a.Id == Id);
                if (savedMenu != null)
                {
                    await _msgRepository.DeleteAsync(savedMenu);
                }
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }

        }

    }
}