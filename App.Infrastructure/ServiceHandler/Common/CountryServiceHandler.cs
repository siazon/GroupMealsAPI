using App.Domain.Common.Shop;
using App.Domain.TravelMeals;
using App.Domain.TravelMeals.Restaurant;
using App.Infrastructure.Exceptions;
using App.Infrastructure.Repository;
using App.Infrastructure.Validation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis.Text;
using Microsoft.Extensions.Caching.Memory;
using SendGrid.Helpers.Mail;
using Stripe;
using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Threading.Tasks;
using static Pipelines.Sockets.Unofficial.SocketConnection;

namespace App.Infrastructure.ServiceHandler.Common
{
    public interface ICountryServiceHandler
    {
        Task<List<DbCountry>> GetCountries(int shopId);
        Task<DbCountry> GetCountry(string id);
        Task<DbCountry> UpsertCountry(DbCountry country);
        Task<bool> DeleteCountry(int shopId, string Id);
        Task<DbStripeEntity> UpsertStripe(DbStripeEntity stripe);
        Task<List<DbStripeEntity>> GetStripes();
    }

    public class CountryServiceHandler : ICountryServiceHandler
    {
        private readonly IDbCommonRepository<DbCountry> _countryRepository;
        private readonly IDbCommonRepository<DbStripeEntity> _stripeRepository;
        IMemoryCache _memoryCache;

        public CountryServiceHandler(IDbCommonRepository<DbCountry> countryRepository, IDbCommonRepository<DbStripeEntity> stripeRepository, IMemoryCache memoryCache)
        {
            _memoryCache = memoryCache;
            _countryRepository = countryRepository;
            _stripeRepository = stripeRepository;
        }
        public async Task<DbCountry> GetCountry(string id) { 
        return await _countryRepository.GetOneAsync(a => a.Id==id);
        }

        public async Task<List<DbCountry>> GetCountries(int shopId)
        {
            var cacheKey = string.Format("motionmedia-{1}-{0}", shopId, typeof(DbCountry).Name);
            var cacheResult = _memoryCache.Get<List<DbCountry>>(cacheKey);
            if (cacheResult != null)
            {
                return cacheResult;
            }
            var countryInfo = await _countryRepository.GetManyAsync(a=>a.IsActive==false && a.ShopId==shopId);
            var contries = countryInfo.ToList();
            _memoryCache.Set(cacheKey, contries);
            return contries;
        }
        public async Task<DbCountry> UpsertCountry(DbCountry country) {
            var cacheKey = string.Format("motionmedia-{1}-{0}", country.ShopId, typeof(DbCountry).Name);
            _memoryCache.Set<List<DbCountry>>(cacheKey, null);
           var temp= await _countryRepository.UpsertAsync(country);
            return temp;
        }

        public async Task<bool> DeleteCountry(int shopId, string Id) {
            var cacheKey = string.Format("motionmedia-{1}-{0}", shopId, typeof(DbCountry).Name);
            _memoryCache.Set<List<DbCountry>>(cacheKey, null);
            var country = await _countryRepository.GetOneAsync(a => a.Id == Id);
            var temp = await _countryRepository.DeleteAsync(country);
             return true;
        }

        public async Task<DbStripeEntity> UpsertStripe(DbStripeEntity stripe)
        {
            var cacheKey = string.Format("motionmedia-{0}",   typeof(DbStripeEntity).Name);
            _memoryCache.Set<List<DbStripeEntity>>(cacheKey, null);
            var temp = await _stripeRepository.UpsertAsync(stripe);
            return temp;
        }
        public async Task<List<DbStripeEntity>> GetStripes()
        {
            var cacheKey = string.Format("motionmedia-{0}",  typeof(DbStripeEntity).Name);
            var cacheResult = _memoryCache.Get<List<DbStripeEntity>>(cacheKey);
            if (cacheResult != null)
            {
                return cacheResult;
            }
            var countryInfo = await _stripeRepository.GetManyAsync(a => a.IsActive == true  );
            var contries = countryInfo.ToList();
            _memoryCache.Set(cacheKey, contries);
            return contries;
        }

    }
}