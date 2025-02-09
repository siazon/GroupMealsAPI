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
using App.Domain.TravelMeals.VO;
using App.Domain.Common;
using App.Domain.Common.Auth;
using System.Security.Cryptography;
using App.Infrastructure.Extensions;

namespace App.Infrastructure.ServiceHandler.Common
{
    public interface ICustomerServiceHandler
    {
        Task<List<DbCustomer>> List(int shopId, string context);
        Task<List<DbCustomer>> ListBossUsers(int shopId, string context);
        Task<DbCustomer> GetCustomer(string userId, int shopId);
        Task<DbCustomer> LoginCustomer(string email, string password, int shopId);
        Task<ResponseModel> CloseAccount(string userId, string email, string pwd);

        Task<ResponseModel> SendForgetPasswordVerifyCode(string email, int shopId);
        Task<ResponseModel> CreateAccount(DbCustomer customer, int shopId);
        Task<ResponseModel> RegisterAccount(DbCustomer customer, int shopId);
        Task<ResponseModel> Logout(string email);
        Task<ResponseModel> VerityEmail(string email, string id, int shopId);
        Task<ResponseModel> AsyncToken(string email, string token);
        Task<ResponseModel> SendRegistrationVerityCode(string email, int shopId);

        Task<ResponseModel> ResetPassword(string email, string resetCode, string password, int shopId);
        Task<ResponseModel> ResetPasswordRestaurant(string email, int shopId);
        Task<ResponseModel> UpdatePassword(string email, string oldPassword, string password, int shopId);

        Task<DbCustomer> UpdateAccount(DbCustomer customer, int shopId);
        Task<DbCustomer> RefreshCartInfo(DbCustomer customer);

        Task<DbCustomer> UpdatePassword(DbCustomer customer, int shopId);
        Task<DbCustomer> UpdateFavorite(DbCustomer customer, int shopId);
        Task<ResponseModel> UpdateCart(List<DbBooking> cartInfos, string UserId, int shopId);
        Task<ResponseModel> UpdateCartInfo(List<DbBooking> cartInfos, DbCustomer user);
        Task<ResponseModel> GetCart(string UserId, int shopId);

        Task<ResponseModel> Delete(DbCustomer item, string email, string pwd, int shopId);
    }

    public class CustomerServiceHandler : ICustomerServiceHandler
    {
        private readonly IDbCommonRepository<DbCustomer> _customerRepository;
        private readonly IDbCommonRepository<DbShop> _shopRepository;
        private readonly IDbCommonRepository<TrDbRestaurant> _restaurantRepository;
        private readonly IDbCommonRepository<DbBooking> _bookingRepository;
        private readonly IDbCommonRepository<DbShopContent> _shopContentRepository;
        private readonly IDbCommonRepository<DbSetting> _settingRepository;
        private readonly IDbCommonRepository<DbDeviceToken> _deviceTokenRepository;
        private readonly IEncryptionHelper _encryptionHelper;
        private readonly IDateTimeUtil _dateTimeUtil;
        private readonly IContentBuilder _contentBuilder;
        private readonly ISendEmailUtil _emailUtil;
        ITwilioUtil _twilioUtil;
        ILogManager _logger;
        IAmountCalculaterUtil _amountCalculaterV1;
        IHostingEnvironment _environment;
        IMemoryCache _memoryCache;

        public CustomerServiceHandler(IDbCommonRepository<DbCustomer> customerRepository, IAmountCalculaterUtil amountCalculaterV1, 
            IMemoryCache memoryCache, IDbCommonRepository<DbBooking> bookingRepository, IDbCommonRepository<DbDeviceToken> deviceTokenRepository,
        ITwilioUtil twilioUtil, ILogManager logger, IHostingEnvironment environment, IEncryptionHelper encryptionHelper, IDateTimeUtil dateTimeUtil, IDbCommonRepository<TrDbRestaurant> restaurantRepository,
        IDbCommonRepository<DbShop> shopRepository, IDbCommonRepository<DbShopContent> shopContentRepository, IContentBuilder contentBuilder, ISendEmailUtil emailUtil, IDbCommonRepository<DbSetting> settingRepository)
        {
            _customerRepository = customerRepository;
            _encryptionHelper = encryptionHelper;
            _restaurantRepository = restaurantRepository;
            _dateTimeUtil = dateTimeUtil;
            _shopRepository = shopRepository;
            _shopContentRepository = shopContentRepository;
            _bookingRepository = bookingRepository;
            _deviceTokenRepository=deviceTokenRepository;
            _contentBuilder = contentBuilder;
            _emailUtil = emailUtil;
            _twilioUtil = twilioUtil;
            _memoryCache = memoryCache;
            _logger = logger;
            _settingRepository = settingRepository;
            _environment = environment;
            _amountCalculaterV1 = amountCalculaterV1;
        }

        public async Task<List<DbCustomer>> List(int shopId, string context)
        {
            Guard.GreaterThanZero(shopId);
            var customers = new List<DbCustomer>();
            if (string.IsNullOrWhiteSpace(context))
            {
                var allcust = await _customerRepository.GetManyAsync(r => r.ShopId == shopId && r.IsBoss);
                customers = allcust.ToList();
            }
            else
            {
                var customersFiltter = await _customerRepository.GetManyAsync(r => r.ShopId == shopId && r.IsBoss && (r.Email.Contains(context) || r.UserName.Contains(context)));
                customers = customersFiltter.ToList();
            }
            List<DbBooking> bookings = new List<DbBooking>();
            string token = "";
            while (token != null)
            {
                var temo = await _bookingRepository.GetManyAsync(a => (a.Status != OrderStatusEnum.None), 500, token);
                var list = temo.Value.ToList();
                bookings.AddRange(list);
                token = temo.Key;
            }
            var cust = customers.OrderByDescending(r => r.AuthValue);
            var returnCustomers = cust.ToList();
            int aaa = 0;
            foreach (var item in returnCustomers)
            {
                int count = 0;

                count = bookings.ToList().FindAll(a => a.Creater == item.Id).Count;

                item.BookingQty = count;
                aaa += count;
            }


            return returnCustomers.ClearForOutPut();
        }
        public async Task<List<DbCustomer>> ListBossUsers(int shopId, string context)
        {
            Guard.GreaterThanZero(shopId);
            var customers = new List<DbCustomer>();
            if (string.IsNullOrWhiteSpace(context))
            {
                var allcust = await _customerRepository.GetManyAsync(r => r.ShopId == shopId && !r.IsBoss);
                customers = allcust.ToList();
            }
            else
            {
                var customersFiltter = await _customerRepository.GetManyAsync(r => r.ShopId == shopId && !r.IsBoss && (r.Email.Contains(context) || r.UserName.Contains(context)));
                customers = customersFiltter.ToList();
            }
            List<DbBooking> bookings = new List<DbBooking>();
            string token = "";
            while (token != null)
            {
                var temo = await _bookingRepository.GetManyAsync(a => (a.Status != OrderStatusEnum.None), 500, token);
                var list = temo.Value.ToList();
                bookings.AddRange(list);
                token = temo.Key;
            }


            List<TrDbRestaurant> restaurants = new List<TrDbRestaurant>();
            token = "";
            while (token != null)
            {
                var temo = await _restaurantRepository.GetManyAsync(a => (1 == 1), 500, token);
                var list = temo.Value.ToList();
                restaurants.AddRange(list);
                token = temo.Key;
            }

            var cust = customers.OrderByDescending(r => r.AuthValue);
            var returnCustomers = cust.ToList();
            int aaa = 0;
            foreach (var item in returnCustomers)
            {
                int count = 0;
                count = bookings.ToList().FindAll(a => a.RestaurantEmail.ToLower().Trim() == item.Email.ToLower().Trim()).Count;

                item.BookingQty = count;

                var rests = restaurants.FindAll(a => a.Users.Contains(item.Email));
                if (rests != null)
                {
                    List<string> reststrs = new List<string>();
                    foreach (var rest in rests)
                    {
                        if (!reststrs.Contains(rest.StoreName))
                            reststrs.Add(rest.StoreName);
                    }
                    item.Restaurants = string.Join(",\r\n", reststrs);
                }

                aaa += count;
            }

            return returnCustomers.ClearForOutPut();
        }

        public async Task<DbCustomer> GetCustomer(string userId, int shopId)
        {
            Guard.GreaterThanZero(shopId);
            var customer = await _customerRepository.GetOneAsync(r => r.Id == userId && r.ShopId == shopId);

            return customer;
        }
        public async Task<DbCustomer> LoginCustomer(string email, string password, int shopId)
        {
            email = email.ToLower().Trim();
            _logger.LogInfo("+++++LoginCustomer: " + email + " : " + password);

            Guard.GreaterThanZero(shopId);
            var passwordEncode = _encryptionHelper.EncryptString(password);
            var customer = await _customerRepository.GetOneAsync(r =>
                r.Email.ToLower().Trim() == email && r.IsActive.HasValue && r.IsActive.Value
                && r.ShopId == shopId);

            return customer;
        }
        public async Task<ResponseModel> AsyncToken(string email, string token)
        {
            _logger.LogDebug("AsyncToken: "+token);
            DbDeviceToken deviceToken = new DbDeviceToken() { Id=Guid.NewGuid().ToString(),Token=token };
            await _deviceTokenRepository.UpsertAsync(deviceToken);
            if (string.IsNullOrWhiteSpace(email))
            {
                var customer1 = await _customerRepository.GetOneAsync(r => r.Email == "siazonchen@outlook.com");
                customer1.DeviceToken = token;
                await _customerRepository.UpsertAsync(customer1);
                return new ResponseModel() { msg = "ok", code = 200 };
            }
            var customer = await _customerRepository.GetOneAsync(r => r.Email == email);
            if (customer == null)
                return new ResponseModel() { msg = $"未找到用户:{email}", code = 200 };
            else
            {
                customer.DeviceToken = token;
                await _customerRepository.UpsertAsync(customer);
            }
            return new ResponseModel() { msg = "ok", code = 200 };
        }
        public async Task<ResponseModel> CloseAccount(string userId, string email, string pwd)
        {
            email = email.ToLower().Trim();
            var customer = await _customerRepository.GetOneAsync(r => r.Id == userId);
            if (customer != null && customer.Password != pwd)
            {
                return new ResponseModel() { msg = "密码错误", code = 501 };
            }
            var bookings = await _bookingRepository.GetManyAsync(a => a.Creater == customer.Id && a.Status != OrderStatusEnum.Settled && a.Status != OrderStatusEnum.Canceled && a.Status != OrderStatusEnum.None);
            if (bookings != null && bookings.Count() > 0)
            {
                return new ResponseModel() { msg = "您有订单未完成，请联系客服完成订单后再注销订单", code = 501 };
            }
            await _customerRepository.DeleteAsync(customer);
            return new ResponseModel() { msg = "ok", code = 200 };
        }
        public async Task<ResponseModel> SendForgetPasswordVerifyCode(string email, int shopId)
        {
            email = email.ToLower().Trim();
            Guard.NotNull(email);
            Guard.GreaterThanZero(shopId);
            var customer = await _customerRepository.GetOneAsync(r =>
                r.Email.ToLower().Trim() == email && r.IsActive.HasValue && r.IsActive.Value && r.ShopId == shopId);
            if (customer == null)
                return new ResponseModel() { msg = "用户不存在", code = 501 };

            string code = GuidHashUtil.Get6DigitNumber();
            var cacheKey = string.Format("SendForgetPasswordVerifyCode-{1}-{0}", shopId, email);
            _memoryCache.Set(cacheKey, code);
            var shopInfo = await _shopRepository.GetOneAsync(r => r.ShopId == shopId && r.IsActive.HasValue && r.IsActive.Value);
            var emailParams = EmailConfigs.Instance.Emails[EmailTypeEnum.VerifyCode];
            _emailUtil.EmailVerifyCode(email, code, shopInfo, emailParams.TemplateName, _environment.WebRootPath, emailParams.Subject, "Forgot Password", "忘记密码");
            Task.Run(() =>
            {
                Thread.Sleep(60 * 5000);
                resetCode(cacheKey);
            });
            return new ResponseModel() { msg = "ok", code = 200 };
        }
        public async Task<ResponseModel> CreateAccount(DbCustomer customer, int shopId)
        {
            Guard.NotNull(customer);
            customer.Email = customer.Email.ToLower().Trim();
            if (string.IsNullOrWhiteSpace(customer.UserName))
                customer.UserName = "defult";
            if (!customer.Email.IsValidEmail())
                return new ResponseModel() { msg = "Email无效", code = 501 };
            var existingCustomer =
               await _customerRepository.GetOneAsync(r => r.ShopId == shopId && r.Email.ToLower().Trim() == customer.Email.ToLower().Trim());
            if (existingCustomer != null)
            {
                if (!existingCustomer.AuthValue.IsBitSet(1))
                {
                    existingCustomer.AuthValue = existingCustomer.AuthValue.SetBit(1);
                }
                return new ResponseModel() { msg = "用户已注册，请使用密码登录", code = 501 };
            }
            else
            {
                customer.Id = Guid.NewGuid().ToString();
                customer.ShopId = shopId;
                customer.Created = DateTime.UtcNow;
                customer.Updated = DateTime.UtcNow;
                customer.IsActive = true;
                customer.IsVerity = true;
                customer.AuthValue = 2;
                customer.InitPassword = GuidHashUtil.Get6DigitNumber();
                var passwordEncode = _encryptionHelper.EncryptString(customer.InitPassword.CreateMD5().ToLower());
                customer.Password = passwordEncode;
                customer.ResetCode = null;
                existingCustomer = customer;

            }
            existingCustomer.IsBoss = true;
            await _customerRepository.UpsertAsync(existingCustomer);

            return new ResponseModel() { msg = "ok", code = 200, data = customer };
        }
        public async Task<ResponseModel> RegisterAccount(DbCustomer customer, int shopId)
        {
            Guard.NotNull(customer);
            customer.Email = customer.Email.ToLower().Trim();
            if (!customer.Email.IsValidEmail())
                return new ResponseModel() { msg = "Email invalid", code = 501 };
            var newItem = customer.Clone();
            var existingCustomer =
               await _customerRepository.GetOneAsync(r => r.ShopId == shopId && r.Email.ToLower().Trim() == customer.Email.ToLower().Trim());
            if (existingCustomer != null)
                return new ResponseModel() { msg = "用户已注册，请使用密码登录", code = 501 };

            var cacheKey = string.Format("SendRegistrationVerityCode-{1}-{0}", shopId, customer.Email.ToLower().Trim());
            string code = _memoryCache.Get(cacheKey)?.ToString();
            if (customer.ResetCode != code)
                return new ResponseModel() { msg = "验证码错误或者已过期", code = 501 };

            newItem.Id = Guid.NewGuid().ToString();
            newItem.ShopId = shopId;
            newItem.Created = DateTime.UtcNow;
            newItem.Updated = DateTime.UtcNow;
            newItem.IsActive = true;
            newItem.IsVerity = true;
            newItem.AuthValue = 0;
            if (customer.IsBoss)
                newItem.AuthValue = 2;
            var passwordEncode = _encryptionHelper.EncryptString(customer.Password);
            newItem.Password = passwordEncode;
            newItem.ResetCode = null;
            newItem.PinCode = GuidHashUtil.Get6DigitNumber();
            await _customerRepository.UpsertAsync(newItem);

            return new ResponseModel() { msg = "ok", code = 200 };
        }
        public async Task<ResponseModel> SendRegistrationVerityCode(string email, int shopId)
        {
            email = email.ToLower().Trim();
            var existingCustomer =
              await _customerRepository.GetOneAsync(r => r.ShopId == shopId && r.Email.ToLower().Trim() == email);
            if (existingCustomer != null)
                return new ResponseModel() { msg = "用户已注册，请使用密码登录", code = 501 };

            string code = GuidHashUtil.Get6DigitNumber();
            var cacheKey = string.Format("SendRegistrationVerityCode-{1}-{0}", shopId, email);
            string _code = _memoryCache.Get(cacheKey)?.ToString();
            if (string.IsNullOrWhiteSpace(_code))
                _memoryCache.Set(cacheKey, code);
            var shopInfo = await _shopRepository.GetOneAsync(r => r.ShopId == shopId && r.IsActive.HasValue && r.IsActive.Value);
            var emailParams = EmailConfigs.Instance.Emails[EmailTypeEnum.VerifyCode];
            _emailUtil.EmailVerifyCode(email, code, shopInfo, emailParams.TemplateName, _environment.WebRootPath, emailParams.Subject, "Verity Code", "注册验证码");
            Task.Run(() =>
            {
                Thread.Sleep(60 * 5000);
                resetCode(cacheKey);
            });
            return new ResponseModel() { msg = "ok", code = 200 };

        }
        private void resetCode(string cacheKey)
        {
            string code = "";
            _memoryCache.Set(cacheKey, code);
        }
        public async Task<ResponseModel> VerityEmail(string email, string id, int shopId)
        {
            email = email.ToLower().Trim();
            var customer = await _customerRepository.GetOneAsync(c => c.Id == id);
            if (customer == null)
                return new ResponseModel() { msg = "用户不存在", code = 501 };
            customer.IsVerity = true;
            var savedCustomer = await _customerRepository.UpsertAsync(customer);
            if (savedCustomer != null)
            {
                return new ResponseModel() { msg = "ok", code = 200 };
            }
            else
                return new ResponseModel() { msg = "操作失败", code = 501 };
        }
        public async Task<ResponseModel> ResetPasswordRestaurant(string email, int shopId)
        {

            email = email.ToLower().Trim();
            Guard.NotNull(email);
            Guard.GreaterThanZero(shopId);
            var customer = await _customerRepository.GetOneAsync(r =>
                r.Email.ToLower().Trim() == email && r.IsActive.HasValue && r.IsActive.Value && r.ShopId == shopId);
            if (customer == null)
                return new ResponseModel() { msg = "用户不存在", code = 501 };
            var cacheKey = string.Format("SendForgetPasswordVerifyCode-{1}-{0}", shopId, email);
            string code = _memoryCache.Get(cacheKey)?.ToString();
            string newpwd = GuidHashUtil.Get6DigitNumber();
            string pwdMd5 = newpwd.CreateMD5().ToLower();
            customer.Password = _encryptionHelper.EncryptString(pwdMd5);
            customer.InitPassword = newpwd;
            var updatedCustomer = await _customerRepository.UpsertAsync(customer);
            updatedCustomer.Password = newpwd;
            await _emailUtil.EmailSystemMessage($"您账号[{email}]的最新密码为：{newpwd}",  email, "system_message", "密码重置");
            return new ResponseModel() { msg = "ok", code = 200, data = updatedCustomer };
        }
        public async Task<ResponseModel> ResetPassword(string email, string resetCode, string password, int shopId)
        {
            email = email.ToLower().Trim();
            Guard.NotNull(email);
            Guard.GreaterThanZero(shopId);
            var customer = await _customerRepository.GetOneAsync(r =>
                r.Email.ToLower().Trim() == email && r.IsActive.HasValue && r.IsActive.Value && r.ShopId == shopId);
            if (customer == null)
                return new ResponseModel() { msg = "用户不存在", code = 501 };
            var cacheKey = string.Format("SendForgetPasswordVerifyCode-{1}-{0}", shopId, email);
            string code = _memoryCache.Get(cacheKey)?.ToString();

            if (code != resetCode)
                return new ResponseModel() { msg = "验证码错误或已过期", code = 501 };

            customer.Password = _encryptionHelper.EncryptString(password);
            customer.InitPassword = "";
            var updatedCustomer = await _customerRepository.UpsertAsync(customer);
            return new ResponseModel() { msg = "ok", code = 200, data = updatedCustomer.ClearForOutPut() };
        }
        public async Task<ResponseModel> UpdatePassword(string email, string oldPassword, string password, int shopId)
        {
            email = email.ToLower().Trim();
            Guard.NotNull(email);
            Guard.GreaterThanZero(shopId);
            var passwordEncode = _encryptionHelper.EncryptString(oldPassword);
            var customer = await _customerRepository.GetOneAsync(r =>
                r.Email.ToLower().Trim() == email && r.Password == passwordEncode && r.IsActive.HasValue && r.IsActive.Value && r.ShopId == shopId);
            if (customer == null)
                return new ResponseModel() { msg = "用户不存在，或原密码错误", code = 501 };

            customer.Password = _encryptionHelper.EncryptString(password);
            customer.InitPassword = "";
            var updatedCustomer = await _customerRepository.UpsertAsync(customer);

            return new ResponseModel() { msg = "ok", code = 200, data = updatedCustomer.ClearForOutPut() };
        }
        public async Task<ResponseModel> Logout(string email)
        {
            var existingCustomer =
               await _customerRepository.GetOneAsync(r => r.Email == email);
            // TODO清空设备token
            return new ResponseModel() { msg = "ok", code = 200, };
        }
        public async Task<DbCustomer> UpdateAccount(DbCustomer customer, int shopId)
        {
            Guard.NotNull(customer);
            var existingCustomer =
               await _customerRepository.GetOneAsync(r => r.ShopId == customer.ShopId && r.Id == customer.Id);
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
        public async Task<ResponseModel> UpdateCart(List<DbBooking> cartInfos, string userId, int shopId)
        {
            var existingCustomer =
                await _customerRepository.GetOneAsync(r => r.ShopId == shopId && r.Id == userId);
            if (existingCustomer == null)
                return new ResponseModel { msg = "User not found!(用户不存在)", };

            //if (cartInfos != null)
            //{
            //    foreach (var item in cartInfos)
            //    {

            //        if (string.IsNullOrWhiteSpace(item.Id))
            //            item.Id = Guid.NewGuid().ToString();

            //        var itemPayInfo = _amountCalculaterV1.getItemPayAmount(item.ConvertToAmount(), existingCustomer, item.Vat);
            //        UpdateAmountInfo(item, itemPayInfo);
            //    }
            //}

            existingCustomer.CartInfos = cartInfos;
            existingCustomer = await RefreshCartInfo(existingCustomer);
            var savedCustomer = await _customerRepository.UpsertAsync(existingCustomer);

            return new ResponseModel() { msg = "ok", code = 200, data = savedCustomer.ClearForOutPut() };
        }
        public async Task<ResponseModel> UpdateCartInfo(List<DbBooking> cartInfos, DbCustomer existingCustomer)
        {
            foreach (var item in cartInfos)
            {
                int idx = existingCustomer.CartInfos.FindIndex(a => a.Id == item.Id);
                existingCustomer.CartInfos[idx] = item;
            }
            existingCustomer = await RefreshCartInfo(existingCustomer);
            var savedCustomer = await _customerRepository.UpsertAsync(existingCustomer);
            return new ResponseModel() { msg = "ok", code = 200, data = savedCustomer.ClearForOutPut() };
        }
        private void UpdateAmountInfo(DbBooking item, ItemPayInfo itemPayInfo) {
            if (item.AmountInfos.Count > 0&& !string.IsNullOrWhiteSpace(item.AmountInfos[0].Id))
                itemPayInfo.Id = item.AmountInfos[0].Id; 
            else
                itemPayInfo.Id = Guid.NewGuid().ToString();
            item.AmountInfos.Clear();
            item.AmountInfos.Add(itemPayInfo);
        }
        public async Task<DbCustomer> RefreshCartInfo(DbCustomer customer)
        {
            foreach (var item in customer?.CartInfos)
            {
                var rest = await _restaurantRepository.GetOneAsync(a => a.Id == item.RestaurantId);
                if (rest != null)
                {
                    item.RestaurantName = rest.StoreName;
                    item.RestaurantEmail = rest.Email;
                    item.RestaurantAddress = rest.Address;
                    item.RestaurantPhone = rest.PhoneNumber;
                    item.EmergencyPhone = rest.ContactPhone;
                    item.RestaurantWechat = rest.Wechat;
                    item.RestaurantCountry = rest.Country;
                    item.Currency = rest.Currency;
                    item.RestaurantTimeZone = rest.TimeZone;
                    item.Vat = rest.Vat;
                    if (!customer.IsOldCustomer)
                        item.BillInfo = rest.BillInfo;//更新最新的付款信息
                    item.BillInfo.IsOldCustomer = customer.IsOldCustomer;
                    item.RestaurantIncluedVAT = rest.IncluedVAT;
                    item.ShowPaid = rest.ShowPaid;

                    item.IntentType = rest.BillInfo.PaymentType == PaymentTypeEnum.Full ? IntentTypeEnum.PaymentIntent : IntentTypeEnum.SetupIntent;
                    List<TrDbRestaurantMenuItem> courses = new List<TrDbRestaurantMenuItem>();
                    foreach (var cate in rest.Categories)
                    {
                        courses.AddRange(cate.MenuItems);
                    }
                    foreach (var course in item.Courses)
                    {
                        var menu = courses.FirstOrDefault(a => a.Id == course.Id);
                        if (menu != null)
                        {
                            course.MenuItemName = menu.MenuItemName;
                            course.Price = menu.Price;
                            course.ChildrenPrice = menu.ChildrenPrice;
                        }
                    }
                }
                var itemPayInfo = _amountCalculaterV1.getItemPayAmount(item.ConvertToAmount(), customer, item.Vat);
                UpdateAmountInfo(item, itemPayInfo);

                DateTime dateTime = item.SelectDateTime.Value;
                if (!string.IsNullOrWhiteSpace(item.MealTime))
                {
                    DateTime.TryParse(item.MealTime, out dateTime);
                    item.SelectDateTime = dateTime.GetTimeZoneByIANACode(item.RestaurantTimeZone);
                }
            }
            return customer;
        }
        public async Task<ResponseModel> GetCart(string userId, int shopId)
        {
            var existingCustomer =
                await _customerRepository.GetOneAsync(r => r.ShopId == shopId && r.Id == userId);
            if (existingCustomer == null)
                return new ResponseModel() { msg = "User not found!(用户不存在)", code = 501 };
            existingCustomer = await RefreshCartInfo(existingCustomer);
            if (existingCustomer.CartInfos.Count > 0)
                existingCustomer = await _customerRepository.UpsertAsync(existingCustomer);




            return new ResponseModel() { msg = "ok", code = 200, data = existingCustomer.CartInfos };
        }
        public async Task<ResponseModel> Delete(DbCustomer item, string email, string pwd, int shopId)
        {
            Guard.NotNull(item);
            if (item.Email == email)
                return new ResponseModel() { msg = "无法删除你自己的账号", code = 501 };
            var passwordEncode = _encryptionHelper.EncryptString(pwd);
            var customer = await _customerRepository.GetOneAsync(r =>
                r.Email == email && r.IsActive.HasValue && r.IsActive.Value
                && r.ShopId == shopId);
            if (customer.Password != passwordEncode)
            {
                return new ResponseModel() { msg = "密码错误", code = 501 };
            }

            var existingItem = await _customerRepository.GetOneAsync(r => r.Id == item.Id && r.ShopId == shopId);
            if (existingItem == null)
                return new ResponseModel() { msg = "无法找到用户", code = 501 };

            item = await _customerRepository.DeleteAsync(item);

            return new ResponseModel() { msg = "ok", code = 500 };
        }


    }
}