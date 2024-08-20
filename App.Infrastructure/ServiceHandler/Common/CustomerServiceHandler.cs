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
using App.Domain.TravelMeals;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Twilio;
using Twilio.Rest.Chat.V2;
using Twilio.Rest.Verify.V2.Service;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Caching.Memory;
using App.Domain.TravelMeals.Restaurant;
using System.Threading;
using Stripe;
using App.Infrastructure.ServiceHandler.TravelMeals;
using App.Domain.Common.Email;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Razor.Language.Extensions;

namespace App.Infrastructure.ServiceHandler.Common
{
    public interface ICustomerServiceHandler
    {
        Task<List<DbCustomer>> List(int shopId);

        Task<DbCustomer> LoginCustomer(string email, string password, int shopId);

        Task<object> SendForgetPasswordVerifyCode(string email, int shopId);

        Task<object> RegisterAccount(DbCustomer customer, int shopId);
        Task<object> VerityEmail(string email, string id, int shopId);
        Task<object> SendRegistrationVerityCode(string email, int shopId);

        Task<object> ResetPassword(string email, string resetCode, string password, int shopId);
        Task<object> UpdatePassword(string email, string oldPassword, string password, int shopId);

        Task<DbCustomer> UpdateAccount(DbCustomer customer, int shopId);

        Task<DbCustomer> UpdatePassword(DbCustomer customer, int shopId);
        Task<DbCustomer> UpdateFavorite(DbCustomer customer, int shopId);
        Task<object> UpdateCart(List<BookingDetail> cartInfos, string UserId, int shopId);
        Task<object> GetCart(string UserId, int shopId);

        Task<object> Delete(DbCustomer item,string email,string pwd, int shopId);
    }

    public class CustomerServiceHandler : ICustomerServiceHandler
    {
        private readonly IDbCommonRepository<DbCustomer> _customerRepository;
        private readonly IDbCommonRepository<DbShop> _shopRepository;
        private readonly IDbCommonRepository<TrDbRestaurant> _restaurantRepository;
        private readonly IDbCommonRepository<TrDbRestaurantBooking> _restaurantBookingRepository;
        private readonly IDbCommonRepository<DbShopContent> _shopContentRepository;
        private readonly IDbCommonRepository<DbSetting> _settingRepository;
        private readonly IEncryptionHelper _encryptionHelper;
        private readonly IDateTimeUtil _dateTimeUtil;
        private readonly IContentBuilder _contentBuilder;
        private readonly  ISendEmailUtil _emailUtil;
        ITwilioUtil _twilioUtil;
        ILogManager _logger;
        IAmountCalculaterUtil _amountCalculaterV1;
        IHostingEnvironment _environment;
        IMemoryCache _memoryCache;

        public CustomerServiceHandler(IDbCommonRepository<DbCustomer> customerRepository, IAmountCalculaterUtil amountCalculaterV1, IMemoryCache memoryCache, IDbCommonRepository<TrDbRestaurantBooking> restaurantBookingRepository,
        ITwilioUtil twilioUtil, ILogManager logger, IHostingEnvironment environment, IEncryptionHelper encryptionHelper, IDateTimeUtil dateTimeUtil, IDbCommonRepository<TrDbRestaurant> restaurantRepository,
        IDbCommonRepository<DbShop> shopRepository, IDbCommonRepository<DbShopContent> shopContentRepository, IContentBuilder contentBuilder, ISendEmailUtil emailUtil, IDbCommonRepository<DbSetting> settingRepository)
        {
            _customerRepository = customerRepository;
            _encryptionHelper = encryptionHelper;
            _restaurantRepository= restaurantRepository;
            _dateTimeUtil = dateTimeUtil;
            _shopRepository = shopRepository;
            _shopContentRepository = shopContentRepository;
            _restaurantBookingRepository=restaurantBookingRepository;
            _contentBuilder = contentBuilder;
            _emailUtil = emailUtil;
            _twilioUtil = twilioUtil;
            _memoryCache = memoryCache;
            _logger = logger;
            _settingRepository = settingRepository;
            _environment = environment;
            _amountCalculaterV1 = amountCalculaterV1;
        }

        public async Task<List<DbCustomer>> List(int shopId)
        {
            Guard.GreaterThanZero(shopId);
            var customers = await _customerRepository.GetManyAsync(r => r.ShopId == shopId);
            var bookings = await _restaurantBookingRepository.GetManyAsync(a => 1 == 1);
            

            int aaa = 0;
            foreach (var item in customers)
            {
                int count = bookings.Sum(a => { if (a.CustomerEmail == item.Email) return a.Details.Count(); else return 0; });
                item.AuthValue =Convert.ToUInt64(count);
                aaa += count;
            }

            var returnCustomers = customers .OrderByDescending(r => r.AuthValue);
            return returnCustomers.ToList().ClearForOutPut();
        }

        public async Task<DbCustomer> LoginCustomer(string email, string password, int shopId)
        {

            _logger.LogInfo("+++++LoginCustomer: " + email + " : " + password);
            Guard.GreaterThanZero(shopId);
            var passwordEncode = _encryptionHelper.EncryptString(password);
            var customer = await _customerRepository.GetOneAsync(r =>
                r.Email == email && r.IsActive.HasValue && r.IsActive.Value
                && r.ShopId == shopId);

            return customer;
        }

        public async Task<object> SendForgetPasswordVerifyCode(string email, int shopId)
        {
            Guard.NotNull(email);
            Guard.GreaterThanZero(shopId);
            var customer = await _customerRepository.GetOneAsync(r =>
                r.Email == email && r.IsActive.HasValue && r.IsActive.Value && r.ShopId == shopId);
            if (customer == null)
                return new { msg = "用户不存在",  };

          string code= GuidHashUtil.Get6DigitNumber();
            var cacheKey = string.Format("SendForgetPasswordVerifyCode-{1}-{0}", shopId, email);
            _memoryCache.Set(cacheKey, code);
            var shopInfo = await _shopRepository.GetOneAsync(r => r.ShopId == shopId && r.IsActive.HasValue && r.IsActive.Value);
            var emailParams = EmailConfigs.Instance.Emails[EmailTypeEnum.VerifyCode];
            _emailUtil.EmailVerifyCode(email, code, shopInfo, emailParams.TemplateName, _environment.WebRootPath, emailParams.Subject, "Forgot Password", "忘记密码");
            Task.Run(() => {
                Thread.Sleep(60 * 5000);
                resetCode(cacheKey);
            });
            return new { msg = "ok", };
        }

        public async Task<object> RegisterAccount(DbCustomer customer, int shopId)
        {
            Guard.NotNull(customer);
            if (customer.Email.Length < 3)
                return new { msg = "Email invalid" };
            var newItem = customer.Clone();
            var existingCustomer =
               await _customerRepository.GetOneAsync(r => r.ShopId == shopId && r.Email == customer.Email);
            if (existingCustomer != null)
                return new { msg = "用户已注册，请使用密码登录" };
            var cacheKey = string.Format("SendRegistrationVerityCode-{1}-{0}", shopId, customer.Email);
            string code = _memoryCache.Get(cacheKey)?.ToString();
            if (customer.ResetCode != code)
                return new { msg = "验证码错误或者已过期" };
            newItem.Id=Guid.NewGuid().ToString();
            newItem.ShopId = shopId;
            newItem.Created = DateTime.UtcNow;
            newItem.Updated = DateTime.UtcNow;
            newItem.IsActive = true;
            newItem.IsVerity = true;
            newItem.AuthValue = 159;
            var passwordEncode = _encryptionHelper.EncryptString(customer.Password);
            newItem.Password = passwordEncode;
            newItem.ResetCode = null;
            newItem.PinCode = GuidHashUtil.Get6DigitNumber();
            await _customerRepository.UpsertAsync(newItem);

            return new { msg = "ok", data = newItem.ClearForOutPut() };
        }
        public async Task<object> SendRegistrationVerityCode(string email, int shopId)
        {
            var existingCustomer =
              await _customerRepository.GetOneAsync(r => r.ShopId == shopId && r.Email == email);
            if (existingCustomer != null)
                return new { msg = "用户已注册，请使用密码登录" };

            string code = GuidHashUtil.Get6DigitNumber();
            var cacheKey = string.Format("SendRegistrationVerityCode-{1}-{0}", shopId, email);
            _memoryCache.Set(cacheKey, code);
            var shopInfo = await _shopRepository.GetOneAsync(r => r.ShopId == shopId && r.IsActive.HasValue && r.IsActive.Value);
            var emailParams = EmailConfigs.Instance.Emails[EmailTypeEnum.VerifyCode];
            _emailUtil.EmailVerifyCode(email, code, shopInfo, emailParams.TemplateName, _environment.WebRootPath, emailParams.Subject, "Verity Code", "注册验证码");
            Task.Run(() => {
                Thread.Sleep(60*5000);
                resetCode(cacheKey);
            });
            return new { msg = "ok", };

        }
        private void resetCode(string cacheKey)
        {
            string code = "";
            _memoryCache.Set(cacheKey, code);
        }
        public async Task<object> VerityEmail(string email, string id, int shopId)
        {
            var customer = await _customerRepository.GetOneAsync(c => c.Id == id);
            if (customer == null)
                return new { msg = "用户不存在" };
            customer.IsVerity = true;
            var savedCustomer = await _customerRepository.UpsertAsync(customer);
            if (savedCustomer != null)
            {
                return new { msg = "ok", data = savedCustomer.ClearForOutPut() };
            }
            else
                return new { msg = "error" };
        }

        public async Task<object> ResetPassword(string email, string resetCode, string password, int shopId)
        {
            Guard.NotNull(email);
            Guard.GreaterThanZero(shopId);
            var customer = await _customerRepository.GetOneAsync(r =>
                r.Email == email && r.IsActive.HasValue && r.IsActive.Value && r.ShopId == shopId);
            if (customer == null)
                return new { msg = "用户不存在", };
            var cacheKey = string.Format("SendForgetPasswordVerifyCode-{1}-{0}", shopId, email);
            string code = _memoryCache.Get(cacheKey)?.ToString();

             if (code != resetCode)
                return new { msg = "验证码错误或已过期", };

            customer.Password = _encryptionHelper.EncryptString(password);
            var updatedCustomer = await _customerRepository.UpsertAsync(customer);
            return new { msg = "ok", data = updatedCustomer.ClearForOutPut() };
        }
        public async Task<object> UpdatePassword(string email, string oldPassword, string password, int shopId)
        {
            Guard.NotNull(email);
            Guard.GreaterThanZero(shopId);
            var passwordEncode = _encryptionHelper.EncryptString(oldPassword);
            var customer = await _customerRepository.GetOneAsync(r =>
                r.Email == email && r.Password == passwordEncode && r.IsActive.HasValue && r.IsActive.Value && r.ShopId == shopId);
            if (customer == null)
                return new { msg = "用户不存在，或原密码错误", };

            customer.Password = _encryptionHelper.EncryptString(password);
            var updatedCustomer = await _customerRepository.UpsertAsync(customer);

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

        public async Task<DbCustomer> UpdateFavorite(DbCustomer customer, int shopId)
        {
            Guard.NotNull(customer);
            var existingCustomer =
                await _customerRepository.GetOneAsync(r => r.ShopId == shopId && r.Id == customer.Id);
            if (existingCustomer == null)
                throw new ServiceException("Customer Not Exists");

            existingCustomer.Updated = DateTime.UtcNow;
            existingCustomer.Favorites = customer.Favorites;

            var savedCustomer = await _customerRepository.UpsertAsync(existingCustomer);

            return savedCustomer.ClearForOutPut();
        }
        public async Task<object> UpdateCart(List<BookingDetail> cartInfos, string userId, int shopId)
        {
            var existingCustomer =
                await _customerRepository.GetOneAsync(r => r.ShopId == shopId && r.Id == userId);
            if (existingCustomer == null)
                return new { msg = "User not found!(用户不存在)", };

            if (cartInfos != null)
            {
                foreach (var item in cartInfos)
                {

                    if (string.IsNullOrWhiteSpace(item.Id))
                        item.Id = Guid.NewGuid().ToString();
                    if (item.AmountInfos == null)
                        item.AmountInfos = new List<AmountInfo>();
                    item.AmountInfos?.Clear();
                    AmountInfo amountInfo = new AmountInfo() { Amount = _amountCalculaterV1.getItemAmount(item), PaidAmount = _amountCalculaterV1.getItemPayAmount(item) };
                    item.AmountInfos.Add(amountInfo);
                }
            }


            existingCustomer.CartInfos = cartInfos;
            var savedCustomer = await _customerRepository.UpsertAsync(existingCustomer);

            return new { msg = "ok", data = savedCustomer.ClearForOutPut() };
        }
        public async Task<object> GetCart(string userId, int shopId)
        {
            var existingCustomer =
                await _customerRepository.GetOneAsync(r => r.ShopId == shopId && r.Id == userId);
            foreach (var item in existingCustomer.CartInfos) {
                var rest = await _restaurantRepository.GetOneAsync(a=>a.Id==item.RestaurantId);
                if(rest != null)
                {
                    List<TrDbRestaurantMenuItem> courses = new List<TrDbRestaurantMenuItem>();
                    foreach (var cate in rest.Categories)
                    {
                        courses.AddRange(cate.MenuItems);
                    }
                    foreach (var course in item.Courses)
                    {
                       var menu=courses.FirstOrDefault(a=>a.Id==course.Id);
                        if (menu != null)
                        {
                            course.MenuItemName=menu.MenuItemName;
                            course.Price=menu.Price;
                            course.ChildrenPrice=menu.ChildrenPrice;
                        }

                    }
                }
                foreach (var info in item.AmountInfos)
                {
                    info.Amount = _amountCalculaterV1.getItemAmount(item);
                    info.PaidAmount = _amountCalculaterV1.getItemPayAmount(item);
                }
            }
            if(existingCustomer.CartInfos.Count>0)
            existingCustomer=await _customerRepository.UpsertAsync(existingCustomer);

            if (existingCustomer == null)
                return new { msg = "User not found!(用户不存在)", };


            return new { msg = "ok", data = existingCustomer.CartInfos };
        }
        public async Task<object> Delete(DbCustomer item, string email, string pwd, int shopId)
        {
            Guard.NotNull(item);
            if(item.Email== email)
                return new { msg = "无法删除你自己的账号", };
            var passwordEncode = _encryptionHelper.EncryptString(pwd);
            var customer = await _customerRepository.GetOneAsync(r =>
                r.Email == email && r.IsActive.HasValue && r.IsActive.Value
                && r.ShopId == shopId);
            if (customer.Password != passwordEncode) {
                return new { msg = "密码错误",  };
            }

            var existingItem = await _customerRepository.GetOneAsync(r => r.Id == item.Id && r.ShopId == shopId);
            if (existingItem == null)
                throw new ServiceException("Cannot find Existing Item");

            item = await _customerRepository.DeleteAsync(item);

            return new { msg = "ok", };
        }

        
    }
}