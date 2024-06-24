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

namespace App.Infrastructure.ServiceHandler.Common
{
    public interface IAuthServiceHandler
    {
        Task<List<DbCustomer>> List(int shopId);
        Task<List<Menu>> ListMenus(int shopId);
        Task<List<Role>> ListRoles(int shopId);

        Task<DbCustomer> UpdateAccount(DbCustomer customer, int shopId);
        Task<Menu> AddMenu(Menu menu);
        Task<Role> AddRole(Role menu);
        Task<Menu> UpdateMenu(Menu menu);
        Task<Role> UpdateRole(Role menu);

        Task<DbCustomer> Delete(DbCustomer item, int shopId);
    }

    public class AuthServiceHandler : IAuthServiceHandler
    {
        private readonly IDbCommonRepository<DbCustomer> _customerRepository;
        private readonly IDbCommonRepository<Menu> _menuRepository;
        private readonly IDbCommonRepository<Role> _roleRepository;
        private readonly IEncryptionHelper _encryptionHelper;
        private readonly IDateTimeUtil _dateTimeUtil;
        private readonly IContentBuilder _contentBuilder;
        private readonly IEmailUtil _emailUtil;

        public AuthServiceHandler(IDbCommonRepository<DbCustomer> customerRepository, IEncryptionHelper encryptionHelper, IDateTimeUtil dateTimeUtil, 
            IDbCommonRepository<Menu> shopRepository, IDbCommonRepository<Role> shopContentRepository, IContentBuilder contentBuilder, IEmailUtil emailUtil)
        {
            _customerRepository = customerRepository;
            _encryptionHelper = encryptionHelper;
            _dateTimeUtil = dateTimeUtil;
            _menuRepository = shopRepository;
            _roleRepository = shopContentRepository;
            _contentBuilder = contentBuilder;
            _emailUtil = emailUtil;
        }

        public async Task<List<DbCustomer>> List(int shopId)
        {
            Guard.GreaterThanZero(shopId);
            var customers = await _customerRepository.GetManyAsync(r => r.ShopId == shopId);

            var returnCustomers = customers.OrderByDescending(r => r.Updated).Take(200);

            return returnCustomers.ToList().ClearForOutPut();
        }
        public async Task<List<Menu>> ListMenus(int shopId)
        {
            Guard.GreaterThanZero(shopId);
            var customers = await _menuRepository.GetManyAsync(r => r.ShopId == shopId);

            var returnCustomers = customers.OrderByDescending(r => r.Updated).Take(200);

            return returnCustomers.ToList();
        }
        public async Task<List<Role>> ListRoles(int shopId)
        {
            Guard.GreaterThanZero(shopId);
            var customers = await _roleRepository.GetManyAsync(r => r.ShopId == shopId);

            var returnCustomers = customers.OrderByDescending(r => r.Updated).Take(200);

            return returnCustomers.ToList();
        }

        public async Task<Menu> AddMenu(Menu menu)
        {
            Guard.NotNull(menu);
            var savedMenu = await _menuRepository.UpsertAsync(menu);
            return savedMenu;
        }
        public async Task<Menu> UpdateMenu(Menu menu)
        {
            Guard.NotNull(menu);
            var savedMenu = await _menuRepository.UpsertAsync(menu);
            return savedMenu;
        }
        public async Task<Role> AddRole(Role role)
        {
            Guard.NotNull(role);
            var savedRole = await _roleRepository.UpsertAsync(role);
            return savedRole;
        }
        public async Task<Role> UpdateRole(Role role)
        {
            Guard.NotNull(role);
            var savedRole = await _roleRepository.UpsertAsync(role);
            return savedRole;
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
               await _customerRepository.GetOneAsync(r => r.ShopId == shopId && r.Email == customer.Email);
            if (existingCustomer != null)
                throw new ServiceException("Customer Already Exists");

            var newItem = customer.Clone();

            newItem.ShopId = shopId;
            newItem.Created = DateTime.UtcNow;
            newItem.Updated = DateTime.UtcNow;
            newItem.IsActive = true;
            var passwordEncode = _encryptionHelper.EncryptString(customer.Password);
            newItem.Password = passwordEncode;
            newItem.ResetCode = null;
            newItem.PinCode = GuidHashUtil.Get6DigitNumber();

            var savedCustomer = await _customerRepository.UpsertAsync(newItem);
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
            var updatedCustomer = await _customerRepository.UpsertAsync(customer);

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

            var savedCustomer = await _customerRepository.UpsertAsync(updateCustomer);

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

            existingCustomer.Updated = DateTime.UtcNow;
            existingCustomer.Password = _encryptionHelper.EncryptString(customer.Password);

            var savedCustomer = await _customerRepository.UpsertAsync(existingCustomer);

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