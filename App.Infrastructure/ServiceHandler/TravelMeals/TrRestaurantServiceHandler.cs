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
using Microsoft.Extensions.Caching.Memory;
using Stripe;
using System.Linq.Expressions;
using Quartz.Util;
using static FluentValidation.Validators.PredicateValidator;
using System.Reflection;
using Microsoft.AspNetCore.Mvc;

namespace App.Infrastructure.ServiceHandler.TravelMeals
{
    public interface ITrRestaurantServiceHandler
    {
        Task<ResponseModel> GetRestaurants(int shopId, string country, string city, string content, DbToken userInfo, int pageSize = -1, string continuationToke = null);


        Task<ResponseModel> GetRestaurant(string Id);
        Task<ResponseModel> GetCitys(int shopId);
        Task<TrDbRestaurant> AddRestaurant(TrDbRestaurant restaurant, int shopId);
        Task<TrDbRestaurant> UpdateRestaurant(TrDbRestaurant restaurant, int shopId);

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
        IMemoryCache _memoryCache;

        private ITrRestaurantBookingServiceHandler _trRestaurantBookingServiceHandler;
        ILogManager _logger;

        public TrRestaurantServiceHandler(IDbCommonRepository<TrDbRestaurant> restaurantRepository, IDateTimeUtil dateTimeUtil, ILogManager logger, ITwilioUtil twilioUtil,
           IDbCommonRepository<DbShop> shopRepository, ITrBookingDataSetBuilder bookingDataSetBuilder,
            IDbCommonRepository<TrDbRestaurantBooking> restaurantBookingRepository, ITrRestaurantBookingServiceHandler trRestaurantBookingServiceHandler, IMemoryCache memoryCache,
            IContentBuilder contentBuilder, IEmailUtil emailUtil)
        {
            _restaurantRepository = restaurantRepository;
            _dateTimeUtil = dateTimeUtil;
            _shopRepository = shopRepository;
            _bookingDataSetBuilder = bookingDataSetBuilder;
            _restaurantBookingRepository = restaurantBookingRepository;
            _contentBuilder = contentBuilder;
            _emailUtil = emailUtil;
            _twilioUtil = twilioUtil;
            _trRestaurantBookingServiceHandler = trRestaurantBookingServiceHandler;
            _logger = logger;
            _memoryCache = memoryCache;
        }

        public static Expression<Func<TrDbRestaurant, bool>> CreateContainsExpression(string propertyName, string value)
        {
            var param = Expression.Parameter(typeof(TrDbRestaurant), "p");
            var member = Expression.Property(param, propertyName);
            var constant = Expression.Constant(value);
            var body = Expression.Call(member, "Contains", Type.EmptyTypes, constant);
            return Expression.Lambda<Func<TrDbRestaurant, bool>>(body, param);
        }
        public static Expression CreateEqualExpression(string propertyName, object? value, Type type)
        {
            var param = Expression.Parameter(typeof(TrDbRestaurant));
            var member = Expression.Property(param, propertyName);
            var constant = Expression.Constant(value, type);
            var body = Expression.Equal(member, constant);
            return body;
        }
        public async Task<ResponseModel> GetRestaurants(int shopId, string country, string city, string content, DbToken userInfo, int pageSize = -1, string continuationToke = null)
        {
         
            _logger.LogInfo($"GetRestaurants");
            List<TrDbRestaurant> data = new List<TrDbRestaurant>();
            try
            {


                bool IsAdmin = userInfo.RoleLevel.AuthVerify(8);
                bool isAllCountry = country.Trim() == "All" || country.Trim() == "全部";
                bool isAllCity = city.Trim() == "All" || city.Trim() == "全部";
                bool isContentEmpty = string.IsNullOrWhiteSpace(content);
                KeyValuePair<string, IEnumerable<TrDbRestaurant>> currentPage;

                var cacheKey = string.Format("motionmedia-{1}-{0}", shopId, typeof(TrDbRestaurant).Name);
                var cacheResult = _memoryCache.Get<KeyValuePair<string, IEnumerable<TrDbRestaurant>>>(cacheKey);
                if (cacheResult.Value != null&&cacheResult.Value.Count()>0)
                {
                    currentPage = cacheResult;
                }
                else
                {
                    currentPage = await _restaurantRepository.GetManyAsync(a => a.ShopId == shopId, pageSize, continuationToke);
                    _memoryCache.Set(cacheKey, currentPage);
                }


                //currentPage = await _restaurantRepository.GetManyAsync(a => a.ShopId == shopId, pageSize, continuationToke);
                var resdata = currentPage.Value.ToList();


                List<Predicate<TrDbRestaurant>> Predicates = new List<Predicate<TrDbRestaurant>>();
                Predicates.Add(s => s.ShopId == shopId);

                if (!isContentEmpty)
                {
                    string lowContent = content.ToLower();
                    Predicates.Add(s => s.StoreName.ToLower().Contains(lowContent) || s.Address.ToLower().Contains(lowContent) || s.Description.ToLower().Contains(lowContent) || s.Tags.Any(b => b.ToLower().Contains(lowContent)) || s.Attractions.Any(b => b.ToLower().Contains(lowContent)));

                }

                if (!IsAdmin)
                {
                    Predicates.Add(a => a.IsActive == true);
                }

                if (!isAllCountry)
                {
                    Predicates.Add(a => a.Country == country);
                }
                if (!isAllCity)
                {
                    Predicates.Add(a => a.City.Contains(city));
                }


                data = resdata.FindAll(Predicates).ClearForOutPut().OrderByDescending(a => a.Created).OrderBy(a => a.SortOrder).ToList();

                continuationToke = currentPage.Key;
            }
            catch (Exception ex)
            {
                _logger.LogInfo($"GetRestaurants"+ex.Message);
                _logger.LogError(ex.Message);
                Console.WriteLine(ex.Message);
            }
            return new ResponseModel { msg = "ok", code = 200, token = continuationToke, data = data };

        }

        public async Task<ResponseModel> GetRestaurantsOld(int shopId, string country, string city, string content, DbToken userInfo, int pageSize = -1, string continuationToke = null)
        {
            _logger.LogInfo($"GetRestaurantsOld");
            KeyValuePair<string, IEnumerable<TrDbRestaurant>> currentPage;
            List<TrDbRestaurant> data = new List<TrDbRestaurant>();
            try
            {


                bool IsAdmin = userInfo.RoleLevel.AuthVerify(7);
                bool isAllCountry = country.Trim() == "All" || country.Trim() == "全部";
                bool isAllCity = city.Trim() == "All" || city.Trim() == "全部";
                bool isContentEmpty = string.IsNullOrWhiteSpace(content);
                Expression expr = null;
                ParameterExpression parameterExp = Expression.Parameter(typeof(TrDbRestaurant));

                Expression memberExpression = null;
                Expression constantExpression = null;
                Expression expressionBody = null;

                if (!isContentEmpty)
                {
                    //content = content.ToLower().Trim();
                    memberExpression = Expression.Property(parameterExp, "StoreName");
                    constantExpression = Expression.Constant(content);
                    expressionBody = Expression.Call(memberExpression, "Contains", Type.EmptyTypes, constantExpression, Expression.Constant(StringComparison.OrdinalIgnoreCase));
                    if (expr != null)
                        expr = Expression.And(expr, expressionBody);
                    else
                        expr = expressionBody;

                    memberExpression = Expression.Property(parameterExp, "Address");
                    constantExpression = Expression.Constant(content);
                    expressionBody = Expression.Call(memberExpression, "Contains", Type.EmptyTypes, constantExpression, Expression.Constant(StringComparison.OrdinalIgnoreCase));
                    expr = Expression.OrElse(expr, expressionBody);

                    memberExpression = Expression.Property(parameterExp, "Description");
                    constantExpression = Expression.Constant(content);
                    expressionBody = Expression.Call(memberExpression, "Contains", Type.EmptyTypes, constantExpression, Expression.Constant(StringComparison.OrdinalIgnoreCase));
                    expr = Expression.OrElse(expr, expressionBody);

                    memberExpression = Expression.Property(parameterExp, "City");
                    constantExpression = Expression.Constant(content);
                    expressionBody = Expression.Call(memberExpression, "Contains", Type.EmptyTypes, constantExpression, Expression.Constant(StringComparison.OrdinalIgnoreCase));
                    expr = Expression.OrElse(expr, expressionBody);


                    memberExpression = Expression.Property(parameterExp, "Tags");
                    constantExpression = Expression.Constant(content);
                    expressionBody = Expression.Call(memberExpression, "Contains", Type.EmptyTypes, constantExpression, Expression.Constant(StringComparison.OrdinalIgnoreCase));
                    expr = Expression.OrElse(expr, expressionBody);


                    memberExpression = Expression.Property(parameterExp, "Attractions");
                    constantExpression = Expression.Constant(content);
                    expressionBody = Expression.Call(memberExpression, "Contains", Type.EmptyTypes, constantExpression, Expression.Constant(StringComparison.OrdinalIgnoreCase));
                    expr = Expression.OrElse(expr, expressionBody);

                }


                memberExpression = Expression.Property(parameterExp, "ShopId");
                constantExpression = Expression.Constant(shopId, typeof(int?));
                expressionBody = Expression.Equal(memberExpression, constantExpression);
                if (expr != null)
                    expr = Expression.AndAlso(expr, expressionBody);
                else
                    expr = expressionBody;

                if (!IsAdmin)
                {
                    memberExpression = Expression.Property(parameterExp, "IsActive");
                    constantExpression = Expression.Constant(true, typeof(bool?));
                    expressionBody = Expression.Equal(memberExpression, constantExpression);
                    expr = Expression.AndAlso(expr, expressionBody);
                    //expr = Expression.AndAlso(expr, CreateEqualExpression("IsActive", true,typeof(bool?)));
                }

                if (!isAllCountry)
                {
                    memberExpression = Expression.Property(parameterExp, "Country");
                    constantExpression = Expression.Constant(country, typeof(string));
                    expressionBody = Expression.Equal(memberExpression, constantExpression);
                    expr = Expression.AndAlso(expr, expressionBody);
                    //expr = Expression.AndAlso(expr, CreateEqualExpression("Country", country, typeof(string)));
                }
                if (!isAllCity)
                {
                    memberExpression = Expression.Property(parameterExp, "City");
                    constantExpression = Expression.Constant(city);
                    expressionBody = Expression.Call(memberExpression, "Contains", Type.EmptyTypes, constantExpression, Expression.Constant(StringComparison.OrdinalIgnoreCase));
                    expr = Expression.AndAlso(expr, expressionBody);

                    //expr = Expression.AndAlso(expr, CreateEqualExpression("City", city, typeof(string)));
                }
                Expression<Func<TrDbRestaurant, bool>> lambdaExpr = Expression.Lambda<Func<TrDbRestaurant, bool>>(expr, parameterExp);



                currentPage = await _restaurantRepository.GetManyAsync(lambdaExpr, pageSize, continuationToke);

                data = currentPage.Value.ClearForOutPut().OrderByDescending(a => a.Created).OrderBy(a => a.SortOrder).ToList();
                continuationToke = currentPage.Key;


            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                Console.WriteLine(ex.StackTrace + ex.Message);
            }
            if (data.Count() == 0)
            {
                _logger.LogError($"country:{country}, city:{city}, content:{content}, userInfo:{userInfo}");
                Console.WriteLine($"country:{country}, city:{city}, content:{content}, userInfo:{userInfo}");
            }

            return new ResponseModel { msg = "ok", code = 200, token = continuationToke, data = data };

        }

        public async Task<ResponseModel> GetRestaurant(string Id)
        {
            var existingRestaurant =
               await _restaurantRepository.GetOneAsync(r => r.Id == Id);


            if (existingRestaurant != null)
                return new ResponseModel { msg = "ok", code = 200, data = existingRestaurant };
            else
                return new ResponseModel { msg = "Restaurant Already Exists", code = 501, data = existingRestaurant };

        }


        public async Task<ResponseModel> GetCitys(int shopId)
        {
            var cacheKey = string.Format("motionmedia-{1}-{0}", shopId, "citys");
            var cacheResult = _memoryCache.Get<Dictionary<string, List<string>>>(cacheKey);
            if (cacheResult != null)
            {
                return new ResponseModel { msg = "ok", code = 200, data = cacheResult };
            }

            var existingRestaurants = await _restaurantRepository.GetManyAsync(r => r.ShopId == shopId);
            if (existingRestaurants == null)
                return new ResponseModel { msg = "Restaurants can find", code = 501, };
            var countrys = existingRestaurants.GroupBy(a => a.Country.Trim());
            var countryList = new List<string>();
            Dictionary<string, List<string>> cityRes = new Dictionary<string, List<string>>();
            foreach (var country in countrys)
            {
                List<string> temo = new List<string>();
                var temp = existingRestaurants.Where(a => a.Country.Trim() == country.Key);
                var citys = temp.GroupBy(a => a.City.Trim());
                foreach (var city in citys)
                {
                    temo.Add(city.Key);
                }
                cityRes[country.Key] = temo;
            }

            _memoryCache.Set(cacheKey, cityRes);

            return new ResponseModel { msg = "ok", code = 200, data = cityRes };

        }

        public async Task<TrDbRestaurant> AddRestaurant(TrDbRestaurant restaurant, int shopId)
        {
            Guard.NotNull(restaurant);
            var cacheKey = string.Format("motionmedia-{1}-{0}", shopId, typeof(TrDbRestaurant).Name);
            TrDbRestaurant nullRest = null;
            _memoryCache.Set(cacheKey, nullRest);
            var existingRestaurant =
               await _restaurantRepository.GetOneAsync(r => r.ShopId == shopId && r.StoreName == restaurant.StoreName && r.Address == restaurant.Address);
            if (existingRestaurant != null)
                throw new ServiceException("Restaurant Already Exists");

            restaurant.Id = Guid.NewGuid().ToString();
            restaurant.ShopId = shopId;
            restaurant.Created = _dateTimeUtil.GetCurrentTime();
            restaurant.Updated = _dateTimeUtil.GetCurrentTime();
            restaurant.IsActive = true;

            var savedRestaurant = await _restaurantRepository.UpsertAsync(restaurant);

            _memoryCache.Set(cacheKey, nullRest);
            return savedRestaurant;
        }
        public async Task<TrDbRestaurant> UpdateRestaurant(TrDbRestaurant restaurant, int shopId)
        {
            Guard.NotNull(restaurant);

            string cacheKey = string.Format("motionmedia-{1}-{0}", shopId, typeof(TrDbRestaurant).Name);
            KeyValuePair<string, IEnumerable<TrDbRestaurant>> cityRes = new KeyValuePair<string, IEnumerable<TrDbRestaurant>>();
            _memoryCache.Set(cacheKey, cityRes);

            var citycacheKey = string.Format("motionmedia-{1}-{0}", shopId, "citys");
            string k = null;
            _memoryCache.Set(citycacheKey, k);
            var existingRestaurant =
               await _restaurantRepository.GetOneAsync(r => r.ShopId == shopId && r.Id == restaurant.Id);
            if (existingRestaurant == null)
                throw new ServiceException("Restaurant Not Exists");
            foreach (var r in restaurant.Categories)
            {
                foreach (var item in r.MenuItems)
                {
                    if (string.IsNullOrWhiteSpace(item.Id))
                        item.Id = Guid.NewGuid().ToString();
                }
            }

            restaurant.Updated = _dateTimeUtil.GetCurrentTime();

            var savedRestaurant = await _restaurantRepository.UpsertAsync(restaurant);

           
            _memoryCache.Set(cacheKey, cityRes);
            _memoryCache.Set(citycacheKey, k);

            return savedRestaurant;
        }

    }
}