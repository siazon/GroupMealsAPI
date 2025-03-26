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
using System.Linq;
using System.Threading.Tasks;

namespace App.Infrastructure.ServiceHandler.Common
{
    public interface IOperationServiceHandler
    {
        Task<List<DbOpearationInfo>> GetOpearations(string referenceId);
        Task<List<DbOpearationInfo>> GetOpearationsformat(string referenceId);
    }

    public class OperationServiceHandler : IOperationServiceHandler
    {
        private readonly IDbCommonRepository<DbOpearationInfo> _opearationRepository;
        IMemoryCache _memoryCache;

        public OperationServiceHandler(IDbCommonRepository<DbOpearationInfo> countryRepository, IMemoryCache memoryCache)
        {
            _memoryCache = memoryCache;
            _opearationRepository = countryRepository;
        }

        public async Task<List<DbOpearationInfo>> GetOpearations(string referenceId)
        {
          
            var countryInfo = await _opearationRepository.GetManyAsync(a=>a.ReferenceId==referenceId);
            return countryInfo.ToList();
        }
        public async Task<List<DbOpearationInfo>> GetOpearationsformat(string referenceId)
        {

            var countryInfo = await _opearationRepository.GetManyAsync(a => a.ReferenceId == referenceId);

            foreach (var item in countryInfo)
            {
                foreach (var course in item.ModifyInfos)
                {
                    if (course.ModifyField == "Courses") {
                        var list = course.newValue.Split(",");
                        course.newValue = list[1].Split(":")[1] + "*" + list[2].Split(":")[1];
                        list = course.oldValue.Split(",");
                        course.oldValue = list[1].Split(":")[1] + "*" + list[2].Split(":")[1];
                    }
                }
            }
            return countryInfo.ToList();
        }


    }
}