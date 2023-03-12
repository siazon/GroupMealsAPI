using App.Domain.Common.Content;
using App.Domain.Common.Setting;
using App.Domain.Common.Shop;
using App.Domain.Food.Delivery;
using App.Domain.Food.Menu;
using App.Infrastructure.Repository;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.Threading.Tasks;
using App.Domain.Food.Discount;

namespace NewFoodCreator.Utility
{
    public class DataCreator
    {

        public async Task Create(List<FDbCategoryGroup> collections, IConfigurationRoot configuration)
        {
            var repository = new DbRepository<FDbCategoryGroup>();
            repository.SetUpConnection(configuration.GetConnectionString("DocumentDbEndPoint"),
                configuration.GetConnectionString("DocumentDbAuthKey"),
                configuration.GetConnectionString("DocumentDbFoodName"));

            foreach (var item in collections)
            {
                await repository.CreateAsync(item);
            }
        }




        public async Task Create(List<FDbMenuCategory> collections, IConfigurationRoot configuration)
        {
            var repository = new DbRepository<FDbMenuCategory>();
            repository.SetUpConnection(configuration.GetConnectionString("DocumentDbEndPoint"),
                configuration.GetConnectionString("DocumentDbAuthKey"),
                configuration.GetConnectionString("DocumentDbFoodName"));

            foreach (var item in collections)
            {
                await repository.CreateAsync(item);
            }
        }


        public async Task Create(List<FDbDiscountOffer> collections, IConfigurationRoot configuration)
        {
            var repository = new DbRepository<FDbDiscountOffer>();
            repository.SetUpConnection(configuration.GetConnectionString("DocumentDbEndPoint"),
                configuration.GetConnectionString("DocumentDbAuthKey"),
                configuration.GetConnectionString("DocumentDbFoodName"));

            foreach (var item in collections)
            {
                await repository.CreateAsync(item);
            }
        }

        public async Task Create(List<FDbDeliveryOption> collections, IConfigurationRoot configuration)
        {
            var repository = new DbRepository<FDbDeliveryOption>();
            repository.SetUpConnection(configuration.GetConnectionString("DocumentDbEndPoint"),
                configuration.GetConnectionString("DocumentDbAuthKey"),
                configuration.GetConnectionString("DocumentDbFoodName"));

            foreach (var item in collections)
            {
                await repository.CreateAsync(item);
            }
        }

        public async Task Create(List<FDbDeliveryCost> collections, IConfigurationRoot configuration)
        {
            var repository = new DbRepository<FDbDeliveryCost>();
            repository.SetUpConnection(configuration.GetConnectionString("DocumentDbEndPoint"),
                configuration.GetConnectionString("DocumentDbAuthKey"),
                configuration.GetConnectionString("DocumentDbFoodName"));

            foreach (var item in collections)
            {
                await repository.CreateAsync(item);
            }
        }

        public async Task Create(List<DbShopContent> collections, IConfigurationRoot configuration)
        {
            var repository = new DbRepository<DbShopContent>();
            repository.SetUpConnection(configuration.GetConnectionString("DocumentDbEndPoint"),
                configuration.GetConnectionString("DocumentDbAuthKey"),
                configuration.GetConnectionString("DocumentDbName"));

            foreach (var item in collections)
            {
                await repository.CreateAsync(item);
            }
        }

        public async Task Create(List<FDbDeliveryArea> collections, IConfigurationRoot configuration)
        {
            var repository = new DbRepository<FDbDeliveryArea>();
            repository.SetUpConnection(configuration.GetConnectionString("DocumentDbEndPoint"),
                configuration.GetConnectionString("DocumentDbAuthKey"),
                configuration.GetConnectionString("DocumentDbFoodName"));

            foreach (var item in collections)
            {
                await repository.CreateAsync(item);
            }
        }

        public async Task Create(List<DbShopUser> collections, IConfigurationRoot configuration)
        {
            var repository = new DbRepository<DbShopUser>();
            repository.SetUpConnection(configuration.GetConnectionString("DocumentDbEndPoint"),
                configuration.GetConnectionString("DocumentDbAuthKey"),
                configuration.GetConnectionString("DocumentDbName"));

            foreach (var item in collections)
            {
                await repository.CreateAsync(item);
            }
        }

        public async Task Create(List<DbSetting> collections, IConfigurationRoot configuration)
        {
            var repository = new DbRepository<DbSetting>();
            repository.SetUpConnection(configuration.GetConnectionString("DocumentDbEndPoint"),
                configuration.GetConnectionString("DocumentDbAuthKey"),
                configuration.GetConnectionString("DocumentDbName"));

            foreach (var item in collections)
            {
                await repository.CreateAsync(item);
            }
        }

        public async Task Create(DbShop item, IConfigurationRoot configuration)
        {
            var repository = new DbRepository<DbShop>();
            repository.SetUpConnection(configuration.GetConnectionString("DocumentDbEndPoint"),
                configuration.GetConnectionString("DocumentDbAuthKey"),
                configuration.GetConnectionString("DocumentDbName"));
            await repository.CreateAsync(item);
        }
    }
}