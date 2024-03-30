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
using App.Domain.Holiday;
using App.Infrastructure.ServiceHandler.Tour;
using Hangfire;
using Microsoft.AspNetCore.Hosting;

namespace App.Infrastructure.ServiceHandler.Common
{
    public interface ICustomerServiceHandler
    {
        Task<List<DbCustomer>> List(int shopId);

        Task<DbCustomer> LoginCustomer(string email, string password, int shopId);

        Task<DbCustomer> ForgetPassword(string email, int shopId);

        Task<object> RegisterAccount(DbCustomer customer, int shopId);
        Task<object> VerityEmail(string email, string id, int shopId);

        Task<DbCustomer> ResetPassword(string email, string resetCode, string password, int shopId);
        Task<object> UpdatePassword(string email, string oldPassword, string password, int shopId);

        Task<DbCustomer> UpdateAccount(DbCustomer customer, int shopId);

        Task<DbCustomer> UpdatePassword(DbCustomer customer, int shopId);
        Task<DbCustomer> UpdateFavorite(DbCustomer customer, int shopId);
        Task<DbCustomer> UpdateCart(DbCustomer customer, int shopId);

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
        ILogManager _logger;

        IHostingEnvironment _environment;

        public CustomerServiceHandler(IDbCommonRepository<DbCustomer> customerRepository, ILogManager logger, IHostingEnvironment environment, IEncryptionHelper encryptionHelper, IDateTimeUtil dateTimeUtil, IDbCommonRepository<DbShop> shopRepository, IDbCommonRepository<DbShopContent> shopContentRepository, IContentBuilder contentBuilder, IEmailUtil emailUtil, IDbCommonRepository<DbSetting> settingRepository)
        {
            _customerRepository = customerRepository;
            _encryptionHelper = encryptionHelper;
            _dateTimeUtil = dateTimeUtil;
            _shopRepository = shopRepository;
            _shopContentRepository = shopContentRepository;
            _contentBuilder = contentBuilder;
            _emailUtil = emailUtil;
            _logger= logger;
            _settingRepository = settingRepository;
            _environment = environment;
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
            
            _logger.LogInfo("+++++LoginCustomer: " + email+" : "+ password);
            Guard.GreaterThanZero(shopId);
            var passwordEncode = _encryptionHelper.EncryptString(password);
            var customer = await _customerRepository.GetOneAsync(r =>
                r.Email == email && r.Password == passwordEncode && r.IsActive.HasValue && r.IsActive.Value && r.IsVerity
                && r.ShopId == shopId);
            if (customer == null)
                throw new ServiceException("User not exist");

            return customer.ClearForOutPut();
        }

        public async Task<DbCustomer> ForgetPassword(string email, int shopId)
        {

            Guard.NotNull(email);
            Guard.GreaterThanZero(shopId);
            var customer = await _customerRepository.GetOneAsync(r =>
                r.Email == email && r.IsActive.HasValue && r.IsActive.Value && r.ShopId == shopId);
            if (customer == null)
                throw new ServiceException("User not exist");

            //Email customer ResetCode
            customer.ResetCode = GuidHashUtil.Get6DigitNumber();
            var updatedCustomer = await _customerRepository.UpdateAsync(customer);
            var shopInfo = await _shopRepository.GetOneAsync(r => r.ShopId == shopId && r.IsActive.HasValue && r.IsActive.Value);
            EmailForgetPWDSender(updatedCustomer, shopInfo, "Fotget password");

            return updatedCustomer.ClearForOutPut();
        }

        public async Task<object> RegisterAccount(DbCustomer customer, int shopId)
        {
            Guard.NotNull(customer);
            if(customer.Email.Length<3)
                return new { msg = "Email invalid" };
            var newItem = customer.Clone();
            var existingCustomer =
               await _customerRepository.GetOneAsync(r => r.ShopId == shopId && r.Email == customer.Email);
            if (existingCustomer != null && existingCustomer.IsVerity)
                return new { msg = "Customer Already Exists" };
            else if (existingCustomer != null && !existingCustomer.IsVerity) {
                newItem = existingCustomer;
                if (newItem != null)
                {
                    var shopInfo = await _shopRepository.GetOneAsync(r => r.ShopId == shopId && r.IsActive.HasValue && r.IsActive.Value);
                    EmailVerifySender(newItem, shopInfo, "Email Verify");
                    return new { msg = "ok", data = newItem.ClearForOutPut() };
                }
            }

            newItem.ShopId = shopId;
            newItem.Created = _dateTimeUtil.GetCurrentTime();
            newItem.Updated = _dateTimeUtil.GetCurrentTime();
            newItem.IsActive = true;
            newItem.AuthValue = 159;
            var passwordEncode = _encryptionHelper.EncryptString(customer.Password);
            newItem.Password = passwordEncode;
            newItem.ResetCode = null;
            newItem.PinCode = GuidHashUtil.Get6DigitNumber();
            if (existingCustomer != null)
            {
                newItem.Id= existingCustomer.Id;
                await _customerRepository.UpdateAsync(newItem); }
            else
                newItem = await _customerRepository.CreateAsync(newItem);
            if (newItem != null)
            {
                var shopInfo = await _shopRepository.GetOneAsync(r => r.ShopId == shopId && r.IsActive.HasValue && r.IsActive.Value);
                EmailVerifySender(newItem, shopInfo, "Email Verify");
            }
            return new { msg = "ok", data = newItem.ClearForOutPut() };
        }

        public async Task<object> VerityEmail(string email, string id, int shopId) {
            var customer = await _customerRepository.GetOneAsync(c => c.Id == id);
            customer.IsVerity=true;
            var savedCustomer = await _customerRepository.UpdateAsync(customer);
            if (savedCustomer != null)
            {
                return new { msg = "ok", data = savedCustomer.ClearForOutPut() };
            }
            else
                return new { msg = "error" };
        }
        private async Task EmailVerifySender(DbCustomer user, DbShop shopInfo, string subject)
        {
            string wwwPath = this._environment.WebRootPath;
            string htmlTemp = EmailTemplateUtil.ReadTemplate(wwwPath, "email_verify");

            try
            {
                BackgroundJob.Enqueue<IContentBuilder>(
                    s => s.SendEmail(user, htmlTemp,shopInfo.ShopSettings, shopInfo.Email, user.Email, subject
                        ));
            }
            catch (Exception ex)
            {
            }
        }
        private async Task EmailForgetPWDSender(DbCustomer user, DbShop shopInfo, string subject)
        {
            string wwwPath = this._environment.WebRootPath;
            string htmlTemp = EmailTemplateUtil.ReadTemplate(wwwPath, "reset_password");

         var   emailHtml = await _contentBuilder.BuildRazorContent(new { code = user.ResetCode }, htmlTemp);
            try
            {
                BackgroundJob.Enqueue<ITourBatchServiceHandler>(s => s.SendEmail(shopInfo.ShopSettings, shopInfo.Email, user.Email, subject, emailHtml));

            }
            catch (Exception ex)
            {
                _logger.LogError($"EmailCustomer Email Customer Error {ex.Message} -{ex.StackTrace} ");
            }

            //try
            //{
            //    BackgroundJob.Enqueue<IContentBuilder>(
            //        s => s.SendEmail(new {code= user.ResetCode}, htmlTemp, shopInfo.ShopSettings, shopInfo.Email, user.Email, subject
            //            ));
            //}
            //catch (Exception ex)
            //{
            //}
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
        public async Task<object> UpdatePassword(string email, string oldPassword, string password, int shopId)
        {
            Guard.NotNull(email);
            Guard.GreaterThanZero(shopId);
            var passwordEncode = _encryptionHelper.EncryptString(oldPassword);
            var customer = await _customerRepository.GetOneAsync(r =>
                r.Email == email && r.Password == passwordEncode && r.IsActive.HasValue && r.IsActive.Value && r.ShopId == shopId);
            if (customer == null)
                return new { msg = "用户不存在，或原密码错误",  };

            customer.Password = _encryptionHelper.EncryptString(password);
            var updatedCustomer = await _customerRepository.UpdateAsync(customer);

            return new { msg = "ok", data = updatedCustomer.ClearForOutPut() };
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

        public async Task<DbCustomer> UpdateFavorite(DbCustomer customer, int shopId)
            {
            Guard.NotNull(customer);
            var existingCustomer =
                await _customerRepository.GetOneAsync(r => r.ShopId == shopId && r.Id == customer.Id);
            if (existingCustomer == null)
                throw new ServiceException("Customer Not Exists");

            existingCustomer.Updated = _dateTimeUtil.GetCurrentTime();
            existingCustomer.Favorites = customer.Favorites;

            var savedCustomer = await _customerRepository.UpdateAsync(existingCustomer);

            return savedCustomer.ClearForOutPut();
        }
        public async Task<DbCustomer> UpdateCart(DbCustomer customer, int shopId)
        {
            Guard.NotNull(customer);
            var existingCustomer =
                await _customerRepository.GetOneAsync(r => r.ShopId == shopId && r.Id == customer.Id);
            if (existingCustomer == null)
                throw new ServiceException("Customer Not Exists");

            existingCustomer.CartInfos = customer.CartInfos;
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