using App.Domain.Common.Shop;
using App.Domain.TravelMeals.Restaurant;
using App.Infrastructure.Exceptions;
using App.Infrastructure.Repository;
using App.Infrastructure.Validation;
using System.Threading.Tasks;

namespace App.Infrastructure.ServiceHandler.Common
{
    public interface IShopServiceHandler
    {
        Task<DbShop> GetShopInfo(int shopId);
        Task<DbShop> UpdateExchangeRate(double exRate, int shopId);
    }

    public class BookingBatchServiceHandler : IShopServiceHandler
    {
        private readonly IDbCommonRepository<DbShop> _shopRepository;

        public BookingBatchServiceHandler(IDbCommonRepository<DbShop> shopRepository)
        {
            _shopRepository = shopRepository;
        }

        public async Task<DbShop> GetShopInfo(int shopId)
        {
            var shopInfo = await GetBasicShopInfo(shopId);

            return shopInfo.ClearForOutPut();
        }

        public async Task<DbShop> GetBasicShopInfo(int shopId)
        {
            Guard.GreaterThanZero(shopId);

            var shopInfo =
                await _shopRepository.GetOneAsync(r => r.ShopId == shopId && r.IsActive.HasValue && r.IsActive.Value);
            if (shopInfo == null)
                throw new ServiceException("Cannot find shop info");
            return shopInfo;
        }
        public async Task<DbShop> UpdateExchangeRate(double exRate, int shopId)
        {
            var existShop =
               await _shopRepository.GetOneAsync(r => r.ShopId==shopId);
            if (existShop == null)
                throw new ServiceException("shop Not Exists");
            existShop.ExchangeRate = exRate;
             var savedShop= await _shopRepository.UpdateAsync(existShop);
            return savedShop;
        }

    }
}