using System.IO;
using System.Threading.Tasks;
using App.Domain.Common.Content;
using App.Domain.Common.Setting;
using App.Domain.Common.Shop;
using App.Domain.Food.Delivery;
using App.Domain.Food.Discount;
using App.Domain.Food.Menu;
using App.Domain.Food.Order;
using App.Infrastructure.Repository;
using Microsoft.Extensions.Configuration;

namespace NewFoodCreator.Processor
{
    public class ShopCleaner
    {
        protected string Mode = "LOCAL";

        public async Task CleanShop(int shopId)
        {

            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);

            IConfigurationRoot configuration = builder.Build();

            Mode = configuration.GetConnectionString("Mode");

            if (Mode != "LIVE")
                return;


            //1.Shop Info(Dummy)
            await CleanShopInfo(shopId, configuration);
            //2.Delivery Area
            await CleanDeliveryArea(shopId, configuration);
            //3.Delivery Cost
            await CleanDeliveryCost(shopId, configuration);
            //4.Delivery Option
            await CleanDeliveryOptions(shopId, configuration);

            //5.Discount Offer
            await CleanDiscountOffer(shopId, configuration);
            //6.Email Template
            await CleanEmailTemplate(shopId, configuration);
            //7.Menu Category
            await CleanMenu(shopId, configuration);
            //8.Option Item(Takeaway Internal)
            await CleanOptionItem(shopId, configuration);
            //9.Settings
            await CleanSettings(shopId, configuration);
            //10.Shop Users
            await CleanShopUser(shopId, configuration);

            //11. Clean Orders
            await CleanOrders(shopId, configuration);
        }

        private async Task CleanOrders(int shopId, IConfigurationRoot configuration)
        {
            var repository = new DbRepository<FDbOrder>();
            repository.SetUpConnection(configuration.GetConnectionString("DocumentDbEndPoint"),
                configuration.GetConnectionString("DocumentDbAuthKey"),
                configuration.GetConnectionString("DocumentDbFoodName"));

            var collections = await repository.GetManyAsync(r => r.ShopId == shopId);
            foreach (var item in collections)
            {
                await repository.DeleteAsync(item);
            }
        }

        private async Task CleanShopUser(int shopId, IConfigurationRoot configuration)
        {
            var repository = new DbRepository<DbShopUser>();
            repository.SetUpConnection(configuration.GetConnectionString("DocumentDbEndPoint"),
                configuration.GetConnectionString("DocumentDbAuthKey"),
                configuration.GetConnectionString("DocumentDbName"));

            var collections = await repository.GetManyAsync(r => r.ShopId == shopId);
            foreach (var item in collections)
            {
                await repository.DeleteAsync(item);
            }
        }

        private async Task CleanSettings(int shopId, IConfigurationRoot configuration)
        {
            var repository = new DbRepository<DbSetting>();
            repository.SetUpConnection(configuration.GetConnectionString("DocumentDbEndPoint"),
                configuration.GetConnectionString("DocumentDbAuthKey"),
                configuration.GetConnectionString("DocumentDbName"));

            var collections = await repository.GetManyAsync(r => r.ShopId == shopId);
            foreach (var item in collections)
            {
                await repository.DeleteAsync(item);
            }
        }

        private async Task CleanOptionItem(int shopId, IConfigurationRoot configuration)
        {
            
        }

        private async Task CleanMenu(int shopId, IConfigurationRoot configuration)
        {
            var repository = new DbRepository<FDbMenuCategory>();
            repository.SetUpConnection(configuration.GetConnectionString("DocumentDbEndPoint"),
                configuration.GetConnectionString("DocumentDbAuthKey"),
                configuration.GetConnectionString("DocumentDbFoodName"));

            var collections = await repository.GetManyAsync(r => r.ShopId == shopId);
            foreach (var item in collections)
            {
                await repository.DeleteAsync(item);
            }
        }

        private async Task CleanEmailTemplate(int shopId, IConfigurationRoot configuration)
        {
            var repository = new DbRepository<DbShopContent>();
            repository.SetUpConnection(configuration.GetConnectionString("DocumentDbEndPoint"),
                configuration.GetConnectionString("DocumentDbAuthKey"),
                configuration.GetConnectionString("DocumentDbName"));

            var collections = await repository.GetManyAsync(r => r.ShopId == shopId);
            foreach (var item in collections)
            {
                await repository.DeleteAsync(item);
            }
        }

        private async Task CleanDiscountOffer(int shopId, IConfigurationRoot configuration)
        {
            var repository = new DbRepository<FDbDiscountOffer>();
            repository.SetUpConnection(configuration.GetConnectionString("DocumentDbEndPoint"),
                configuration.GetConnectionString("DocumentDbAuthKey"),
                configuration.GetConnectionString("DocumentDbFoodName"));

            var collections = await repository.GetManyAsync(r => r.ShopId == shopId);
            foreach (var item in collections)
            {
                await repository.DeleteAsync(item);
            }
        }

        private async Task CleanDeliveryOptions(int shopId, IConfigurationRoot configuration)
        {
            var repository = new DbRepository<FDbDeliveryOption>();
            repository.SetUpConnection(configuration.GetConnectionString("DocumentDbEndPoint"),
                configuration.GetConnectionString("DocumentDbAuthKey"),
                configuration.GetConnectionString("DocumentDbFoodName"));

            var collections = await repository.GetManyAsync(r => r.ShopId == shopId);
            foreach (var item in collections)
            {
                await repository.DeleteAsync(item);
            }
        }

        private async Task CleanDeliveryCost(int shopId, IConfigurationRoot configuration)
        {
            var repository = new DbRepository<FDbDeliveryCost>();
            repository.SetUpConnection(configuration.GetConnectionString("DocumentDbEndPoint"),
                configuration.GetConnectionString("DocumentDbAuthKey"),
                configuration.GetConnectionString("DocumentDbFoodName"));

            var collections = await repository.GetManyAsync(r => r.ShopId == shopId);
            foreach (var item in collections)
            {
                await repository.DeleteAsync(item);
            }
        }

        private async Task CleanDeliveryArea(int shopId, IConfigurationRoot configuration)
        {
            var repository = new DbRepository<FDbDeliveryArea>();
            repository.SetUpConnection(configuration.GetConnectionString("DocumentDbEndPoint"),
                configuration.GetConnectionString("DocumentDbAuthKey"),
                configuration.GetConnectionString("DocumentDbFoodName"));

            var collections = await repository.GetManyAsync(r => r.ShopId == shopId);
            foreach (var item in collections)
            {
                await repository.DeleteAsync(item);
            }
        }

        private async Task CleanShopInfo(int shopId, IConfigurationRoot configuration)
        {
            var repository = new DbRepository<DbShop>();
            repository.SetUpConnection(configuration.GetConnectionString("DocumentDbEndPoint"),
                configuration.GetConnectionString("DocumentDbAuthKey"),
                configuration.GetConnectionString("DocumentDbName"));

            var collections = await repository.GetManyAsync(r => r.ShopId == shopId);
            foreach (var item in collections)
            {
                await repository.DeleteAsync(item);
            }
        }
    }
}