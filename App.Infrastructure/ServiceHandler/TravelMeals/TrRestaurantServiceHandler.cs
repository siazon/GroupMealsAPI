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
using App.Domain.Common.Customer;
using Quartz.Logging;
using System.Threading;
using Microsoft.Azure.Cosmos.Linq;
using QuestPDF.Helpers;
using App.Infrastructure.ServiceHandler.Common;

namespace App.Infrastructure.ServiceHandler.TravelMeals
{
    public interface ITrRestaurantServiceHandler
    {
        Task<ResponseModel> GetRestaurants(int shopId, string country, string city, string content, DbToken userInfo, int pageSize = -1, string continuationToke = null);
        Task<ResponseModel> GetRestaurantsByAdmin(int shopId, string country, string city, string content, DbToken userInfo, int pageSize = -1, string continuationToke = null);


        Task<ResponseModel> GetRestaurant(string Id);
        Task<ResponseModel> GetCitys(int shopId);
        Task<ResponseModel> GetCities(int shopId);
        Task<ResponseModel> GetCitiesV1(int shopId);
        Task<TrDbRestaurant> AddRestaurant(TrDbRestaurant restaurant, int shopId);
        Task<ResponseModel> UpsetCities(DbCountry countries, int shopId);
        Task<ResponseModel> ExportRestaurants();
        Task<TrDbRestaurant> UpdateRestaurant(TrDbRestaurant restaurant, int shopId);
        Task<ResponseModel> DeleteRestaurant(string id, string email, string pwd, int shopId);

    }

    public class TrRestaurantServiceHandler : ITrRestaurantServiceHandler
    {
        private readonly IDbCommonRepository<TrDbRestaurant> _restaurantRepository;
        private readonly IDbCommonRepository<DbCustomer> _customerRepository;

        ICountryServiceHandler _countryRepository;
        private readonly IDateTimeUtil _dateTimeUtil;
        private readonly IDbCommonRepository<DbShop> _shopRepository;
        private readonly ITrBookingDataSetBuilder _bookingDataSetBuilder;
        private readonly IContentBuilder _contentBuilder;
        private readonly IEmailUtil _emailUtil;
        private readonly IEncryptionHelper _encryptionHelper;
        ITwilioUtil _twilioUtil;
        IMemoryCache _memoryCache;

        private ITrRestaurantBookingServiceHandler _trRestaurantBookingServiceHandler;
        ILogManager _logger;

        public TrRestaurantServiceHandler(IDbCommonRepository<TrDbRestaurant> restaurantRepository, IDateTimeUtil dateTimeUtil, ILogManager logger, ITwilioUtil twilioUtil,
           IDbCommonRepository<DbShop> shopRepository, ITrBookingDataSetBuilder bookingDataSetBuilder, IEncryptionHelper encryptionHelper, IDbCommonRepository<DbCustomer> customerRepository,
        ITrRestaurantBookingServiceHandler trRestaurantBookingServiceHandler, IMemoryCache memoryCache,
            IContentBuilder contentBuilder, IEmailUtil emailUtil, ICountryServiceHandler countryRepository)
        {
            _restaurantRepository = restaurantRepository;
            _customerRepository = customerRepository;
            _dateTimeUtil = dateTimeUtil;
            _shopRepository = shopRepository;
            _bookingDataSetBuilder = bookingDataSetBuilder;
            _contentBuilder = contentBuilder;
            _emailUtil = emailUtil;
            _twilioUtil = twilioUtil;
            _trRestaurantBookingServiceHandler = trRestaurantBookingServiceHandler;
            _logger = logger;
            _memoryCache = memoryCache;
            _encryptionHelper = encryptionHelper;
            _countryRepository = countryRepository;
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
        private async Task<KeyValuePair<string, IEnumerable<TrDbRestaurant>>> QeuryByContent(int shopId, string content, int pageSize = -1, string continuationToke = null)
        {
            List<Predicate<TrDbRestaurant>> Predicates = new List<Predicate<TrDbRestaurant>>();
            string lowContent = content.ToLower();
            KeyValuePair<string, IEnumerable<TrDbRestaurant>> currentPage = await _restaurantRepository.GetManyAsync(s => s.ShopId == shopId &&
            (s.StoreName.ToLower().Contains(lowContent) || s.Address.ToLower().Contains(lowContent) ||
                s.Description.ToLower().Contains(lowContent) || s.Tags.Any(b => b.ToLower().Contains(lowContent)) ||
                s.Attractions.Any(b => b.ToLower().Contains(lowContent))), pageSize, continuationToke);
            return currentPage;
        }
        public async Task<ResponseModel> GetRestaurantsByAdmin(int shopId, string country, string city, string content, DbToken userInfo,
            int pageSize = -1, string continuationToke = null)
        {
            _logger.LogInfo($"GetRestaurants");
            IList<TrDbRestaurant> resdata = new List<TrDbRestaurant>();
            try
            {
                bool IsAdmin = userInfo.RoleLevel.AuthVerify(8);
                bool isAllCountry = country.Trim() == "All" || country.Trim() == "全部";
                bool isAllCity = city.Trim() == "All" || city.Trim() == "全部";
                bool isContentEmpty = string.IsNullOrWhiteSpace(content);
                KeyValuePair<string, IEnumerable<TrDbRestaurant>> currentPage;
                if (isAllCountry && isAllCity && isContentEmpty)
                {
                    var cacheKey = string.Format("motionmedia-{1}-{0}-{2}", shopId, typeof(TrDbRestaurant).Name, pageSize);
                    var cacheResult = _memoryCache.Get<KeyValuePair<string, IEnumerable<TrDbRestaurant>>>(cacheKey);
                    if (cacheResult.Value != null && cacheResult.Value.Count() > 0 && string.IsNullOrWhiteSpace(continuationToke))
                    {
                        currentPage = cacheResult;
                    }
                    else
                    {
                        currentPage = await _restaurantRepository.GetManyAsync(a => a.ShopId == shopId, pageSize, continuationToke);
                        if (string.IsNullOrWhiteSpace(continuationToke))
                            _memoryCache.Set(cacheKey, currentPage);
                    }
                }
                else
                {
                    string lowContent = content?.ToLower();
                    if (isContentEmpty)
                    {
                        if (isAllCity)
                            currentPage = await _restaurantRepository.GetManyAsync(a => a.Country == country, pageSize, continuationToke);
                        else
                            currentPage = await _restaurantRepository.GetManyAsync(a => a.Country == country && a.City == (city), pageSize, continuationToke);

                    }
                    else
                        currentPage = await _restaurantRepository.GetManyAsync(s => s.StoreName.ToLower().Contains(lowContent) || s.Address.ToLower().Contains(lowContent) || s.Description.ToLower().Contains(lowContent) || s.Tags.Any(b => b.ToLower().Contains(lowContent)) || s.Attractions.Any(b => b.ToLower().Contains(lowContent)), pageSize, continuationToke);
                }

                if (!IsAdmin)
                    resdata = currentPage.Value.Where(a => a.IsActive == true).ToList();
                else
                    resdata = currentPage.Value.ToList();
                continuationToke = currentPage.Key;
            }
            catch (Exception ex)
            {
                _logger.LogInfo($"GetRestaurants" + ex.Message);
                _logger.LogError(ex.Message);
                Console.WriteLine(ex.Message);
            }
            return new ResponseModel { msg = "ok", code = 200, token = continuationToke, data = resdata };
        }
        public async Task<ResponseModel> GetRestaurants(int shopId, string country, string city, string content, DbToken userInfo,
            int pageSize = -1, string continuationToke = null)
        {
            ResponseModel response= await GetRestaurantsByAdmin(shopId, country, city, content, userInfo, pageSize, continuationToke);
            IList<TrDbRestaurant> resdata = response.data as IList<TrDbRestaurant>;
            var res= resdata.Where(a=>a.IsActive == true).ToList();
            //foreach (var item in res)
            //{
            //    if(!item.Images.Contains(item.Image))
            //    item.Images.Add(item.Image);
            //}
            response.data = res;
            return response;
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
        public async Task<ResponseModel> ExportRestaurants()
        {

            List<TrDbRestaurant> restaurants = new List<TrDbRestaurant>();
            string token = "";
            while (token != null)
            {
                var temo = await _restaurantRepository.GetManyAsync(a => (1 == 1), 500, token);
                var list = temo.Value.ToList();
                restaurants.AddRange(list);
                token = temo.Key;
            }

            return new ResponseModel { msg = "ok", code = 200, data = restaurants };
        }

        public async Task<ResponseModel> GetRestaurant(string Id)
        {
            var existingRestaurant =
               await _restaurantRepository.GetOneAsync(r => r.Id == Id && r.IsActive == true);

            //existingRestaurant.Images.Add(existingRestaurant.Image);
            if (existingRestaurant != null)
                return new ResponseModel { msg = "ok", code = 200, data = existingRestaurant };
            else
                return new ResponseModel { msg = "Restaurant not Exists", code = 501, data = existingRestaurant };

        }

        public async Task<ResponseModel> UpsetCities(DbCountry country, int shopId)
        {
            var cacheKey = string.Format("motionmedia-{1}-{0}", shopId, "cities");
            if (country == null)
                return new ResponseModel { msg = "保存失败，请检查输入的内容是否正确", code = 501, data = null };
            var Dbcountry = await _countryRepository.GetCountry(country.Id);
            var isChanged = IsCityNameChanged(country, Dbcountry);
            if (isChanged)
                return new ResponseModel { msg = "城市名称不可修改，请删除后新增", code = 501, data = null };
            var deleteDisable = await IsDeleteCity(country, Dbcountry);
            if (deleteDisable)
                return new ResponseModel { msg = "此城市不能删除，请先删除与本城市关联的餐厅", code = 501, data = null };
            var cacheKeycitys = string.Format("motionmedia-{1}-{0}", shopId, "citys");
            _memoryCache.Set<DbCountry>(cacheKey, null);
            _memoryCache.Set<Dictionary<string, List<string>>>(cacheKeycitys, null);
            if (string.IsNullOrWhiteSpace(country.Id))
                country.Id = Guid.NewGuid().ToString();
            var dbCity = await _countryRepository.UpsertCountry(country);
            var rests = await _restaurantRepository.GetManyAsync(a => a.Country == dbCity.Code);
            foreach (var item in dbCity.Cities)
            {
                var res = rests.Where(a => a.Country == dbCity.Code && a.City == item.Name).ToList();
                foreach (var city in res)
                {
                    city.Currency = dbCity.Currency;
                    city.Vat = dbCity.VAT;
                    city.TimeZone = item.TimeZone;
                    await _restaurantRepository.UpsertAsync(city);
                }
            }


            _memoryCache.Set<DbCountry>(cacheKey, null);
            _memoryCache.Set<Dictionary<string, List<string>>>(cacheKeycitys, null);
            return new ResponseModel { msg = "ok", code = 200, data = null };
        }
        private bool IsCityNameChanged(DbCountry country, DbCountry dbCountry)
        {
            if (dbCountry != null && dbCountry.Cities.Count == country.Cities.Count)
            {
                foreach (var item in dbCountry.Cities)
                {
                    var city = country.Cities.FirstOrDefault(a => a.Name == item.Name);
                    if (city == null)
                    {
                        return true;
                    }
                }
            }
            return false;
        }
        private async Task<bool> IsDeleteCity(DbCountry country, DbCountry dbCountry)
        {
            if (dbCountry != null && dbCountry.Cities.Count > country.Cities.Count)
            {
                foreach (var item in dbCountry.Cities)
                {
                    var city = country.Cities.FirstOrDefault(a => a.Name == item.Name);
                    if (city == null)
                    {
                        var res = await _restaurantRepository.GetManyAsync(a => a.City == item.Name);
                        if (res != null && res.Count() > 0)
                        {
                            return true;
                        }
                        else
                            return false;
                    }
                }

            }
            return false;
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
                temo.Add("全部");
                foreach (var city in citys)
                {
                    temo.Add(city.Key);
                }
                cityRes[country.Key] = temo;
            }

            _memoryCache.Set(cacheKey, cityRes);

            return new ResponseModel { msg = "ok", code = 200, data = cityRes };
        }
        public async Task<ResponseModel> GetCities(int shopId)
        {
            _logger.LogInfo("azure functions actioned");
            var existingCities = await _countryRepository.GetCountries(shopId);
            if (existingCities == null)
                return new ResponseModel { msg = "Cities can't fund", code = 501, };
            DbCountryold dbCountryold = new DbCountryold() { Id = Guid.NewGuid().ToString(), Countries = new List<Country>() };
            foreach (var item in existingCities)
            {
                Country country = new Country()
                {
                    Currency = item.Currency,
                    CurrencySymbol = item.CurrencySymbol,
                    ExchangeRate = item.ExchangeRate,
                    ExchangeRateExtra = item.ExchangeRateExtra,
                    Name = item.Code,
                    NameCN = item.Name,
                    TimeZone = item.Cities[0].TimeZone,
                    SortOrder = item.SortOrder ?? 0,
                    Cities = item.Cities,
                };
                dbCountryold.Countries.Add(country);
            }
            return new ResponseModel { msg = "ok", code = 200, data = dbCountryold };
        }
        public async Task<ResponseModel> GetCitiesV1(int shopId)
        {
            _logger.LogInfo("azure functions actioned");

            var existingCities = await _countryRepository.GetCountries(shopId);
            if (existingCities == null)
                return new ResponseModel { msg = "Cities can't fund", code = 501, };

            existingCities = existingCities.OrderBy(x => x.SortOrder).ToList();
            existingCities.ToList().ForEach(x => { x.Cities = x.Cities.OrderBy(a => a.SortOrder).ToList(); });

            return new ResponseModel { msg = "ok", code = 200, data = existingCities };

        }

        public async Task<TrDbRestaurant> AddRestaurant(TrDbRestaurant restaurant, int shopId)
        {
            Guard.NotNull(restaurant);
            var cacheKey = string.Format("motionmedia-{1}-{0}-{2}", shopId, typeof(TrDbRestaurant).Name, 50);
            TrDbRestaurant nullRest = null;
            _memoryCache.Set(cacheKey, nullRest);
            var existingRestaurant =
               await _restaurantRepository.GetOneAsync(r => r.ShopId == shopId && r.StoreName == restaurant.StoreName && r.Address == restaurant.Address);
            if (existingRestaurant != null)
            {
                return await UpdateRestaurant(restaurant, shopId);
            }

            restaurant.Id = Guid.NewGuid().ToString();
            restaurant.ShopId = shopId;
            restaurant.Created = DateTime.UtcNow;
            restaurant.Updated = DateTime.UtcNow;
            restaurant.IsActive = true;
            foreach (var r in restaurant.Categories)
            {
                foreach (var item in r.MenuItems)
                {
                    if (string.IsNullOrWhiteSpace(item.Id))
                        item.Id = Guid.NewGuid().ToString();
                }
            }

            var savedRestaurant = await _restaurantRepository.UpsertAsync(restaurant);

            _memoryCache.Set(cacheKey, nullRest);
            return savedRestaurant;
        }
        public async Task<TrDbRestaurant> UpdateRestaurant(TrDbRestaurant restaurant, int shopId)
        {
            Guard.NotNull(restaurant);
            var cacheKey = string.Format("motionmedia-{1}-{0}-{2}", shopId, typeof(TrDbRestaurant).Name, 50);
            KeyValuePair<string, IEnumerable<TrDbRestaurant>> cityRes = new KeyValuePair<string, IEnumerable<TrDbRestaurant>>();
            _memoryCache.Set(cacheKey, cityRes);

            var citycacheKey = string.Format("motionmedia-{1}-{0}", shopId, "cities");
            _memoryCache.Set<string>(citycacheKey, null);


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

            restaurant.Updated = DateTime.UtcNow;

            var savedRestaurant = await _restaurantRepository.UpsertAsync(restaurant);


            ClearCache(shopId, citycacheKey, cacheKey, cityRes);



            return savedRestaurant;
        }
        private void ClearCache(int shopId, string citycacheKey, string cacheKey, KeyValuePair<string, IEnumerable<TrDbRestaurant>> cityRes)
        {

            _memoryCache.Set(cacheKey, cityRes);
            _memoryCache.Set<DbStripeEntity>(string.Format("motionmedia-{0}", typeof(DbStripeEntity).Name), null);
            _memoryCache.Set<DbShop>(string.Format("motionmedia-{1}-{0}", shopId, typeof(DbShop).Name), null);
            _memoryCache.Set<string>(citycacheKey, null);
            _memoryCache.Set<DbCountry>(string.Format("motionmedia-{1}-{0}", shopId, typeof(DbCountry).Name), null);
        }

        public async Task<ResponseModel> DeleteRestaurant(string id, string email, string pwd, int shopId)
        {
            var cacheKey = string.Format("motionmedia-{1}-{0}-{2}", shopId, typeof(TrDbRestaurant).Name, 50);
            KeyValuePair<string, IEnumerable<TrDbRestaurant>> cityRes = new KeyValuePair<string, IEnumerable<TrDbRestaurant>>();
            _memoryCache.Set(cacheKey, cityRes);

            var citycacheKey = string.Format("motionmedia-{1}-{0}", shopId, "cities");
            _memoryCache.Set<string>(citycacheKey, null);

            var passwordEncode = _encryptionHelper.EncryptString(pwd);
            var customer = await _customerRepository.GetOneAsync(r =>
                r.Email == email && r.IsActive.HasValue && r.IsActive.Value
                && r.ShopId == shopId);
            if (customer.Password != passwordEncode)
            {
                return new ResponseModel { msg = "密码错误", code = 501 };
            }

            var existingItem = await _restaurantRepository.GetOneAsync(r => r.Id == id && r.ShopId == shopId);
            if (existingItem == null)
                return new ResponseModel { msg = "餐厅不存在", code = 501 };

            var item = await _restaurantRepository.DeleteAsync(existingItem);
            _memoryCache.Set(cacheKey, cityRes);
            _memoryCache.Set<string>(citycacheKey, null);
            _memoryCache.Set<DbShop>(string.Format("motionmedia-{1}-{0}", shopId, typeof(DbShop).Name), null);
            return new ResponseModel { msg = "ok", code = 200 };
        }
    }
}