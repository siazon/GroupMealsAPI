using App.Domain.Common.Shop;
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
using System.Linq;
using System.Threading.Tasks;

namespace App.Infrastructure.ServiceHandler.Common
{
    public interface ICountryServiceHandler
    {
        Task<DbCountry> GetCountry(int shopId);
        Task<string> GetDbCountryTimezone(string countryName);
    }

    public class CountryServiceHandler : ICountryServiceHandler
    {
        private readonly IDbCommonRepository<DbCountry> _countryRepository;
        IMemoryCache _memoryCache;

        public CountryServiceHandler(IDbCommonRepository<DbCountry> countryRepository, IMemoryCache memoryCache)
        {
            _memoryCache = memoryCache;
            _countryRepository = countryRepository;
        }

        public async Task<DbCountry> GetCountry(int shopId)
        {
            var cacheKey = string.Format("motionmedia-{1}-{0}", shopId, typeof(DbCountry).Name);
            var cacheResult = _memoryCache.Get<DbCountry>(cacheKey);
            if (cacheResult != null)
            {
                return cacheResult;
            }
            var countryInfo = await _countryRepository.GetOneAsync(a=>a.ShopId==shopId);
            _memoryCache.Set(cacheKey, countryInfo);
            return countryInfo;
        }

        public async Task<string> GetDbCountryTimezone(string countryName)
        {
            var citis = await _countryRepository.GetOneAsync(a => a.ShopId == 11 && a.IsActive == true);
            string ianaCode = citis.Countries.FirstOrDefault(a => a.Name == countryName).TimeZone;
            return ianaCode;
        }

    }
}