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

namespace App.Infrastructure.ServiceHandler.Common
{
    public interface ICustomerServiceHandler
    {
        Task<List<DbCustomer>> List(int shopId);

        Task<DbCustomer> LoginCustomer(string email, string password, int shopId);

        Task<DbCustomer> ForgetPassword(string email, int shopId);

        Task<DbCustomer> RegisterAccount(DbCustomer customer, int shopId);

        Task<DbCustomer> ResetPassword(string email, string resetCode, string password, int shopId);

        Task<DbCustomer> UpdateAccount(DbCustomer customer, int shopId);

        Task<DbCustomer> UpdatePassword(DbCustomer customer, int shopId);

        Task<DbCustomer> Delete(DbCustomer item, int shopId);
    }

    public class CustomerServiceHandler : ICustomerServiceHandler
    {
        private readonly IDbCommonRepository<DbCustomer> _customerRepository;
        private readonly IDbCommonRepository<DbShop> _shopRepository;
        private readonly IDbCommonRepository<DbShopContent> _shopContentRepository;
        private readonly IDbCommonRepository<DbSetting> _settingRepository;
        private readonly IEncryptionHelper _encryptionHelper;
        private readonly IDateTimeUtil _dateTimeUtil;
        private readonly IContentBuilder _contentBuilder;
        private readonly IEmailUtil _emailUtil;

        public CustomerServiceHandler(IDbCommonRepository<DbCustomer> customerRepository, IEncryptionHelper encryptionHelper, IDateTimeUtil dateTimeUtil, IDbCommonRepository<DbShop> shopRepository, IDbCommonRepository<DbShopContent> shopContentRepository, IContentBuilder contentBuilder, IEmailUtil emailUtil, IDbCommonRepository<DbSetting> settingRepository)
        {
            _customerRepository = customerRepository;
            _encryptionHelper = encryptionHelper;
            _dateTimeUtil = dateTimeUtil;
            _shopRepository = shopRepository;
            _shopContentRepository = shopContentRepository;
            _contentBuilder = contentBuilder;
            _emailUtil = emailUtil;
            _settingRepository = settingRepository;
        }

        public async Task<List<DbCustomer>> List(int shopId)
        {
            Guard.GreaterThanZero(shopId);
            var customers = await _customerRepository.GetManyAsync(r => r.ShopId == shopId);

            var returnCustomers = customers.OrderByDescending(r => r.Updated).Take(2000);

            return returnCustomers.ToList().ClearForOutPut();
        }

        public async Task<DbCustomer> LoginCustomer(string email, string password, int shopId)
        {
            Guard.GreaterThanZero(shopId);
            var passwordEncode = _encryptionHelper.EncryptString(password);
            var customer = await _customerRepository.GetOneAsync(r =>
                r.Email == email && r.Password == passwordEncode && r.IsActive.HasValue && r.IsActive.Value
                && r.ShopId == shopId);
            if (customer == null)
                throw new ServiceException("User not exist");

            return customer.ClearForOutPut();
        }

        public async Task<DbCustomer> ForgetPassword(string email, int shopId)
        {

            throw new NotImplementedException();
            //Guard.NotNull(email);
            //Guard.GreaterThanZero(shopId);
            //var customer = await _customerRepository.GetOneAsync(r =>
            //    r.Email == email && r.IsActive.HasValue && r.IsActive.Value && r.ShopId == shopId);
            //if (customer == null)
            //    throw new ServiceException("User not exist");

            ////Email customer ResetCode
            //customer.ResetCode = GuidHashUtil.Get6DigitNumber();
            //var updatedCustomer = await _customerRepository.UpdateAsync(customer);

            //var shopInfo =
            //    await _shopRepository.GetOneAsync(r => r.ShopId == shopId && r.IsActive.HasValue && r.IsActive.Value);
            //if (shopInfo == null)
            //    throw new ServiceException("Cannot find shop info");

            //var content = await _shopContentRepository.GetOneAsync(r =>
            //    r.ShopId == shopId && r.Key == EmailTemplateEnum.ForgetPassword.ToString());
            //if (content == null)
            //    throw new ServiceException("Cannot find email content");

            //dynamic objectContent = new
            //{
            //    ShopName = shopInfo.ShopName,
            //    PhoneNumber = shopInfo.ShopNumber,
            //    Address = shopInfo.ShopAddressInfo.Address1 + ',' + shopInfo.ShopAddressInfo.Address2,
            //    ShopWebSite = shopInfo.Website,
            //    ResetCode = customer.ResetCode
            //};

            //var emailContent = await _contentBuilder.BuildRazorContent(objectContent, content.Content);

            //var settings = await _settingRepository.GetManyAsync(r => r.ShopId == shopId);

            //var result = await _emailUtil.SendEmail(settings.ToList(), shopInfo.Email, "",
            //    email, "", content.Subject, null, emailContent, null);

            //if (!result)
            //    throw new ServiceException("Failed to send reset code");

            //return updatedCustomer.ClearForOutPut();
        }

        public async Task<DbCustomer> RegisterAccount(DbCustomer customer, int shopId)
        {
            Guard.NotNull(customer);
            var existingCustomer =
                _customerRepository.GetOneAsync(r => r.ShopId == shopId && r.Email == customer.Email);
            if (existingCustomer != null)
                throw new ServiceException("Customer Already Exists");

            var newItem = customer.Clone();

            newItem.ShopId = shopId;
            newItem.Created = _dateTimeUtil.GetCurrentTime();
            newItem.Updated = _dateTimeUtil.GetCurrentTime();
            newItem.IsActive = true;
            var passwordEncode = _encryptionHelper.EncryptString(customer.Password);
            newItem.Password = passwordEncode;
            newItem.ResetCode = null;
            newItem.PinCode = GuidHashUtil.Get6DigitNumber();

            var savedCustomer = await _customerRepository.CreateAsync(newItem);
            return savedCustomer.ClearForOutPut();
        }

        public async Task<DbCustomer> ResetPassword(string email, string resetCode, string password, int shopId)
        {
            Guard.NotNull(email);
            Guard.GreaterThanZero(shopId);
            var customer = await _customerRepository.GetOneAsync(r =>
                r.Email == email && r.ResetCode == resetCode && r.IsActive.HasValue && r.IsActive.Value && r.ShopId == shopId);
            if (customer == null)
                throw new ServiceException("Cannot Reset User not exist");

            customer.Password = _encryptionHelper.EncryptString(password);
            var updatedCustomer = await _customerRepository.UpdateAsync(customer);

            return updatedCustomer.ClearForOutPut();
        }

        public async Task<DbCustomer> UpdateAccount(DbCustomer customer, int shopId)
        {
            Guard.NotNull(customer);
            var existingCustomer =
               await _customerRepository.GetOneAsync(r => r.ShopId == shopId && r.Email == customer.Email);
            if (existingCustomer == null)
                throw new ServiceException("Customer Not Exists");

            var updateCustomer = existingCustomer.Copy(customer);

            var savedCustomer = await _customerRepository.UpdateAsync(updateCustomer);

            return savedCustomer.ClearForOutPut();
        }

        public async Task<DbCustomer> UpdatePassword(DbCustomer customer, int shopId)
        {
            Guard.NotNull(customer);
            Guard.NotNull(customer.Password);
            var existingCustomer =
                await _customerRepository.GetOneAsync(r => r.ShopId == shopId && r.Id == customer.Id);
            if (existingCustomer == null)
                throw new ServiceException("Customer Not Exists");

            existingCustomer.Updated = _dateTimeUtil.GetCurrentTime();
            existingCustomer.Password = _encryptionHelper.EncryptString(customer.Password);

            var savedCustomer = await _customerRepository.UpdateAsync(existingCustomer);

            return savedCustomer.ClearForOutPut();
        }

        public async Task<DbCustomer> Delete(DbCustomer item, int shopId)
        {
            Guard.NotNull(item);

            var existingItem = await _customerRepository.GetOneAsync(r => r.Id == item.Id && r.ShopId == shopId);
            if (existingItem == null)
                throw new ServiceException("Cannot find Existing Item");

            item = await _customerRepository.DeleteAsync(item);

            return item;
        }
    }
}