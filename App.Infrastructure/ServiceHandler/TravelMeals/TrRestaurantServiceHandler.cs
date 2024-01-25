using App.Domain.Common.Content;
using App.Domain.Common.Setting;
using App.Domain.Common.Shop;
using App.Domain.TravelMeals;
using App.Domain.TravelMeals.Restaurant;
using App.Infrastructure.Builders.Common;
using App.Infrastructure.Builders.TravelMeals;
using App.Infrastructure.Exceptions;
using App.Infrastructure.Repository;
using App.Infrastructure.Utility.Common;
using App.Infrastructure.Validation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using App.Domain.Enum;
using System.Diagnostics;
using App.Domain.Common.Stripe;
using Stripe.Issuing;
using Microsoft.AspNetCore.Http;
using App.Domain.Holiday;
using App.Domain.Common;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Quartz.Impl;
using Quartz;
using App.Domain.Common.Auth;
using Newtonsoft.Json.Linq;
using Microsoft.Azure.Cosmos.Serialization.HybridRow.Schemas;

namespace App.Infrastructure.ServiceHandler.TravelMeals
{
    public interface ITrRestaurantServiceHandler
    {
        Task<List<TrDbRestaurant>> GetRestaurantInfo(int shopId);
        Task<ResponseModel> GetRestaurantInfo(int shopId, int pageSize = -1, string continuationToke = null);
        Task<List<TrDbRestaurant>> SearchRestaurantInfo(int shopId, string searchContent);

        Task<ResponseModel> RequestBooking(TrDbRestaurantBooking booking, int shopId, string email);
        Task<bool> DeleteBooking(string bookingId, int shopId);
        Task<TrDbRestaurant> AddRestaurant(TrDbRestaurant restaurant, int shopId);
        Task<TrDbRestaurant> UpdateRestaurant(TrDbRestaurant restaurant, int shopId);
        Task<ResponseModel> SearchBookings(int shopId, string email, string content, int pageSize = -1, string continuationToke = null);

    }

    public class TrRestaurantServiceHandler : ITrRestaurantServiceHandler
    {
        private readonly IDbCommonRepository<TrDbRestaurant> _restaurantRepository;
        private readonly IDateTimeUtil _dateTimeUtil;
        private readonly IDbCommonRepository<DbShop> _shopRepository;
        private readonly ITrBookingDataSetBuilder _bookingDataSetBuilder;
        private readonly IDbCommonRepository<TrDbRestaurantBooking> _restaurantBookingRepository;
        private readonly IContentBuilder _contentBuilder;
        private readonly IEmailUtil _emailUtil;
        ITwilioUtil _twilioUtil;

        Microsoft.AspNetCore.Hosting.IHostingEnvironment _environment;
        private ITrRestaurantBookingServiceHandler _trRestaurantBookingServiceHandler;
        ILogManager _logger;

        public TrRestaurantServiceHandler(IDbCommonRepository<TrDbRestaurant> restaurantRepository, IDateTimeUtil dateTimeUtil, ILogManager logger, ITwilioUtil twilioUtil, Microsoft.AspNetCore.Hosting.IHostingEnvironment environment, IDbCommonRepository<DbShop> shopRepository, ITrBookingDataSetBuilder bookingDataSetBuilder, IDbCommonRepository<TrDbRestaurantBooking> restaurantBookingRepository, ITrRestaurantBookingServiceHandler trRestaurantBookingServiceHandler, IContentBuilder contentBuilder, IEmailUtil emailUtil)
        {
            _restaurantRepository = restaurantRepository;
            _dateTimeUtil = dateTimeUtil;
            _shopRepository = shopRepository;
            _bookingDataSetBuilder = bookingDataSetBuilder;
            _restaurantBookingRepository = restaurantBookingRepository;
            _contentBuilder = contentBuilder;
            _emailUtil = emailUtil;
            _environment = environment;
            _twilioUtil = twilioUtil;
            _trRestaurantBookingServiceHandler = trRestaurantBookingServiceHandler;
            _logger = logger;
        }
        public async Task<ResponseModel> GetRestaurantInfo(int shopId, int pageSize = -1, string continuationToke = null)
        {
            DateTime stime = DateTime.Now;
            KeyValuePair<string, IEnumerable<TrDbRestaurant>> currentPage;
            //for (int i = 0; i < 5; i++)

            currentPage = await _restaurantRepository
                .GetManyAsync(r => r.ShopId == shopId && r.IsActive.HasValue && r.IsActive.Value, pageSize, continuationToke);
            var temp = currentPage.Value.ClearForOutPut().OrderBy(a => a.SortOrder).ToList();
            continuationToke = currentPage.Key;
            _logger.LogInfo("_restaurantRepository.GetManyAsync:" + (DateTime.Now - stime).ToString());

            return new ResponseModel { msg = "ok", code = 200, token = continuationToke, data = temp };
        }
        public async Task<List<TrDbRestaurant>> GetRestaurantInfo(int shopId)
        {
            DateTime stime = DateTime.Now;
            var restaurants = await _restaurantRepository.GetManyAsync(r => r.ShopId == shopId && r.IsActive.HasValue && r.IsActive.Value);
            var temp = restaurants.ClearForOutPut().OrderBy(a => a.SortOrder).ToList();
            _logger.LogInfo("_restaurantRepository.GetManyAsync:" + (DateTime.Now - stime).ToString());
            return temp;
        }
        public async Task<List<TrDbRestaurant>> SearchRestaurantInfo(int shopId, string searchContent)
        {
            var restaurants = await _restaurantRepository.GetManyAsync(r => r.ShopId == shopId
            && (r.StoreName.Contains(searchContent) || r.StoreNameCn.Contains(searchContent) || r.ShopAddress.Contains(searchContent))
            && r.IsActive.HasValue && r.IsActive.Value);
            var temp = restaurants.ClearForOutPut();
            return temp;//TrDbRestaurant
        }
        public async Task<ResponseModel> SearchBookings(int shopId, string email, string content, int pageSize = -1, string continuationToken = null)
        {

            if (string.IsNullOrWhiteSpace(email) && string.IsNullOrWhiteSpace(content))
            {
                var Bookings = await _restaurantBookingRepository.GetManyAsync(a => !a.IsDeleted || a.Details.Any(b => b.RestaurantEmail == email), pageSize, continuationToken);
                var list = Bookings.Value.ToList();
                return new ResponseModel { msg = "ok", code = 200, token = Bookings.Key, data = list };
            }
            if (string.IsNullOrWhiteSpace(content))
            {
                var Bookings = await _restaurantBookingRepository.GetManyAsync(a => !a.IsDeleted || a.Details.Any(b => b.RestaurantEmail == email), pageSize, continuationToken);
                var list = Bookings.Value.ToList().FindAll(a => a.CustomerEmail == email).ToList();
                return new ResponseModel { msg = "ok", code = 200, token = Bookings.Key, data = list };
            }

            else
            {
                var Bookings = await _restaurantBookingRepository.GetManyAsync(a => !a.IsDeleted || a.Details.Any(b => b.RestaurantEmail == email), pageSize, continuationToken);
                var list = Bookings.Value.ToList().FindAll(a => a.Details.Any(d => d.RestaurantName.ToLower().Contains(content.ToLower()))).ToList();
                return new ResponseModel { msg = "ok", code = 200, token = Bookings.Key, data = list };
            }
        }


        public async Task<ResponseModel> RequestBooking(TrDbRestaurantBooking booking, int shopId, string email)
        {
            _logger.LogInfo("RequestBooking" + shopId);
            Guard.NotNull(booking);
            Guard.AreEqual(booking.ShopId.Value, shopId);
            foreach (var item in booking.Details)
            {
                if ((item.SelectDateTime- DateTime.Now).Value.TotalHours < 12) {
                    return new ResponseModel { msg = "用餐时间少于12个小时", code = 200, data = null };
                }
            }


            //var findRestaurant = await _restaurantRepository.GetOneAsync(r => r.ShopId == shopId && r.Id == booking.RestaurantId);
            //if (findRestaurant == null)
            //    throw new ServiceException("Cannot Find tour");
            TourBooking newBooking;
            var createTime = _dateTimeUtil.GetCurrentTime();
            var exsitBookings = await _restaurantBookingRepository.GetManyAsync(r => r.Status == OrderStatusEnum.None && r.CustomerEmail == booking.CustomerEmail);
            var exsitBooking = exsitBookings.FirstOrDefault(a => (createTime - a.Created).Value.Hours < 2);
            if (exsitBooking != null)
            {
                bool noPay = true; ;
                foreach (var item in booking.Details)
                {
                    if (item.BillInfo.PaymentType != 2)
                        noPay = false;
                }
                if (noPay)
                {
                    booking.Status = OrderStatusEnum.PayAtProperty;
                    //SendEmail(booking);

                }
                exsitBooking.Created = _dateTimeUtil.GetCurrentTime();
                exsitBooking.Details = booking.Details;
                exsitBooking.Status = booking.Status;
                exsitBooking.CustomerName = booking.CustomerName;
                exsitBooking.CustomerPhone = booking.CustomerPhone;
                exsitBooking.CustomerEmail = booking.CustomerEmail;
                exsitBooking.PayCurrency = booking.PayCurrency;
                var savedBooking = await _restaurantBookingRepository.UpdateAsync(exsitBooking);
                if (noPay)
                {
                    SendEmail(savedBooking);
                }
                return new ResponseModel { msg = "", code = 200,  data = savedBooking };
            }
            else
            {
                bool noPay = true; ;
                foreach (var item in booking.Details)
                {
                    item.Id = Guid.NewGuid().ToString();
                    if (item.BillInfo.PaymentType != 2)
                        noPay = false;
                }
                if (noPay)
                {
                    booking.Status = OrderStatusEnum.PayAtProperty;
                    //SendEmail(booking);

                }
                booking.Id = Guid.NewGuid().ToString();
                booking.BookingRef = "GM" + SnowflakeId.getSnowId();
                booking.Created = _dateTimeUtil.GetCurrentTime();
                var opt = new OperationInfo() { Operater = email, Operation = "新增订单", UpdateTime = DateTime.Now };
                booking.Operations.Add(opt);
                var newItem = await _restaurantBookingRepository.CreateAsync(booking);
                if (noPay)
                {
                    SendEmail(newItem);
                }

                return new ResponseModel { msg = "", code = 200, data = newItem };
            }
        }
        private async void SendEmail(TrDbRestaurantBooking booking)
        {
            var shopInfo = await _shopRepository.GetOneAsync(r => r.ShopId == 13 && r.IsActive.HasValue && r.IsActive.Value);
            if (shopInfo == null)
            {
                _logger.LogInfo("----------------Cannot find shop info" + booking.Id);
                throw new ServiceException("Cannot find shop info");
            }
            EmailUtils.EmailCustomerTotal(booking, shopInfo, "new_meals", this._environment.WebRootPath, _contentBuilder, _logger);
            EmailUtils.EmailBoss(booking, shopInfo, "New Order", this._environment.WebRootPath, _twilioUtil, _contentBuilder, _logger);
            EmailUtils.EmailSupport(booking, shopInfo, "New Order", this._environment.WebRootPath, _twilioUtil, _contentBuilder, _logger);

        }
        public async Task<TrDbRestaurant> AddRestaurant(TrDbRestaurant restaurant, int shopId)
        {
            Guard.NotNull(restaurant);
            var existingRestaurant =
               await _restaurantRepository.GetOneAsync(r => r.ShopId == shopId && r.StoreName == restaurant.StoreName && r.ShopAddress == restaurant.ShopAddress);
            if (existingRestaurant != null)
                throw new ServiceException("Restaurant Already Exists");
            var newItem = restaurant.Clone();

            newItem.ShopId = shopId;
            newItem.Created = _dateTimeUtil.GetCurrentTime();
            newItem.Updated = _dateTimeUtil.GetCurrentTime();
            newItem.IsActive = true;

            var savedRestaurant = await _restaurantRepository.CreateAsync(newItem);
            return savedRestaurant;
        }
        public async Task<TrDbRestaurant> UpdateRestaurant(TrDbRestaurant restaurant, int shopId)
        {
            Guard.NotNull(restaurant);
            var existingRestaurant =
               await _restaurantRepository.GetOneAsync(r => r.ShopId == shopId && r.StoreName == restaurant.StoreName && r.ShopAddress == restaurant.ShopAddress);
            if (existingRestaurant == null)
                throw new ServiceException("Restaurant Not Exists");
            restaurant.Updated = _dateTimeUtil.GetCurrentTime();

            var savedRestaurant = await _restaurantRepository.UpdateAsync(restaurant);
            return savedRestaurant;
        }
        public async Task<bool> DeleteBooking(string bookingId, int shopId)
        {
            var booking = await _restaurantBookingRepository.GetOneAsync(a => a.Id == bookingId);
            booking.IsDeleted = true; booking.Updated = _dateTimeUtil.GetCurrentTime();
            var savedRestaurant = await _restaurantBookingRepository.UpdateAsync(booking);
            return savedRestaurant != null;
        }
    }
}