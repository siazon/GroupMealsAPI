using App.Domain.Common.Content;
using App.Domain.Common.Setting;
using App.Domain.Common.Shop;
using App.Domain.Food.Delivery;
using App.Domain.Food.Menu;
using App.Infrastructure.Builders.Common;
using App.Infrastructure.Builders.Food;
using Microsoft.Extensions.Configuration;
using NewFoodCreator.Utility;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace NewFoodCreator.Processor
{
    public class ShopCreator
    {
      
        public async Task CreateNewShop(int shopId, string country)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);

            IConfigurationRoot configuration = builder.Build();

            DapperDataRetriver.Mode = configuration.GetConnectionString("Mode");

            //1. Create Shop Info (Dummy)
            Console.WriteLine("===Creating ShopInfo===");
            await CreateShopInfo(shopId, country, configuration);

            //2. Create Shop Content
            Console.WriteLine("===Creating Shop Content===");
            await CreateShopContent(shopId, country, configuration);  //TODO: More work to be done

            //2. Create Support Categories
            Console.WriteLine("===Creating Shop Menu===");
            await CreateShopMenu(shopId, country, configuration);


            //2. Create Support Categories
            Console.WriteLine("===Creating Shop Group Category===");
            await CreateShopGroupCategory(shopId, country, configuration);

            //3. Create Delivery Area Default
            Console.WriteLine("===Creating Delivery Area===");
            await CreateDeliveryArea(shopId, country, configuration);

            //3. Create Delivery Cost
            Console.WriteLine("===Creating Delivery Cost===");
            await CreateDeliveryCost(shopId, country, configuration);

            //4. Create deliveryOption
            Console.WriteLine("===Creating Delivery Option===");
            await CreateDeliveryOption(shopId, country, configuration);

            //5. Create optionItems
            //TODO: this is later for POS

            //6. ServerSetting
            Console.WriteLine("===Creating Settings===");
            await CreateSetting(shopId, country, configuration);

            //8. shopUser
            Console.WriteLine("===Creating Shop User===");
            await CreateShopUser(shopId, country, configuration);
        }

        private async Task CreateShopGroupCategory(int shopId, string country, IConfigurationRoot configuration)
        {
            var collections =
            new FCategoryGroupCollectionBuilder().GeneralInfo().Build();

            foreach (var item in collections)
            {
                item.ShopId = shopId;
            }

            if (DapperDataRetriver.Mode == "LIVE")
                await new DataCreator().Create(collections, configuration);
            else
                new FileCreator().CreateFiles(collections, configuration.GetConnectionString("OutputPath"), "categoryGroup");


        }

        public async Task CreateShopMenu(int shopId, string country, IConfigurationRoot configuration)
        {
            var collections = new List<FDbMenuCategory>();

            if (country == "IE")
            {
                collections = new FMenuCollectionBuilder().GeneralInfo().IE().Build();
            }
            else if (country == "UK")
            {
                collections = new FMenuCollectionBuilder().GeneralInfo().UK().Build();
            }
            else if (country == "PL")
            {
                collections = new FMenuCollectionBuilder().GeneralInfo().PL().Build();
            }

            foreach (var item in collections)
            {
                item.ShopId = shopId;
            }

            if (DapperDataRetriver.Mode == "LIVE")
                await new DataCreator().Create(collections, configuration);
            else
                new FileCreator().CreateFiles(collections, configuration.GetConnectionString("OutputPath"), "menu");
        }

        public async Task CreateDeliveryOption(int shopId, string country, IConfigurationRoot configuration)
        {
            var collections = new List<FDbDeliveryOption>();

            if (country == "IE")
            {
                collections = new FDeliveryOptionCollectionBuilder().GeneralInfo().IE().Build();
            }
            else if (country == "UK")
            {
                collections = new FDeliveryOptionCollectionBuilder().GeneralInfo().UK().Build();
            }
            else if (country == "PL")
            {
                collections = new FDeliveryOptionCollectionBuilder().GeneralInfo().PL().Build();
            }

            foreach (var item in collections)
            {
                item.ShopId = shopId;
            }

            if (DapperDataRetriver.Mode == "LIVE")
                await new DataCreator().Create(collections, configuration);
            else
                new FileCreator().CreateFiles(collections, configuration.GetConnectionString("OutputPath"), "deliveryOption");
        }

        public async Task CreateDeliveryCost(int shopId, string country, IConfigurationRoot configuration)
        {
            var collections = new List<FDbDeliveryCost>();

            if (country == "IE")
            {
                collections = new FDeliveryCostCollectionBuilder().GeneralInfo().IE().Build();
            }
            else if (country == "UK")
            {
                collections = new FDeliveryCostCollectionBuilder().GeneralInfo().UK().Build();
            }
            else if (country == "PL")
            {
                collections = new FDeliveryCostCollectionBuilder().GeneralInfo().PL().Build();
            }
            foreach (var item in collections)
            {
                item.ShopId = shopId;
            }

            if (DapperDataRetriver.Mode == "LIVE")
                await new DataCreator().Create(collections, configuration);
            else
                new FileCreator().CreateFiles(collections, configuration.GetConnectionString("OutputPath"), "deliveryCost");
        }

        public async Task CreateDeliveryArea(int shopId, string country, IConfigurationRoot configuration)
        {
            var collections = new List<FDbDeliveryArea>();

            if (country == "IE")
            {
                collections = new FDeliveryAreaCollectionBuilder().GeneralInfo().IE().Build();
            }
            else if (country == "UK")
            {
                collections = new FDeliveryAreaCollectionBuilder().GeneralInfo().UK().Build();
            }
            else if (country == "PL")
            {
                collections = new FDeliveryAreaCollectionBuilder().GeneralInfo().PL().Build();
            }
            foreach (var item in collections)
            {
                item.ShopId = shopId;
            }
            if (DapperDataRetriver.Mode == "LIVE")
                await new DataCreator().Create(collections, configuration);
            else
                new FileCreator().CreateFiles(collections, configuration.GetConnectionString("OutputPath"), "deliveryArea");
        }

        public async Task CreateShopContent(int shopId, string country, IConfigurationRoot configuration)
        {
            var collections = new List<DbShopContent>();

            if (country == "IE")
            {
                collections = new ShopContentCollectionBuilder().GeneralInfo().Food().IE().Build();
            }
            else if (country == "UK")
            {
                collections = new ShopContentCollectionBuilder().GeneralInfo().Food().UK().Build();
            }
            else if (country == "PL")
            {
                collections = new ShopContentCollectionBuilder().GeneralInfo().Food().PL().Build();
            }

            foreach (var item in collections)
            {
                item.ShopId = shopId;
            }
            if (DapperDataRetriver.Mode == "LIVE")
                await new DataCreator().Create(collections, configuration);
            else
                new FileCreator().CreateFiles(collections, configuration.GetConnectionString("OutputPath"), "shopContent");
        }

        public async Task CreateShopUser(int shopId, string country, IConfigurationRoot configuration)
        {
            var collections = new List<DbShopUser>();

            if (country == "IE")
            {
                collections = new ShopUserCollectionBuilder().GeneralInfo().Food().IE().Build();
            }
            else if (country == "UK")
            {
                collections = new ShopUserCollectionBuilder().GeneralInfo().Food().UK().Build();
            }
            else if (country == "PL")
            {
                collections = new ShopUserCollectionBuilder().GeneralInfo().Food().PL().Build();
            }

            foreach (var item in collections)
            {
                item.ShopId = shopId;
            }
            if (DapperDataRetriver.Mode == "LIVE")
                await new DataCreator().Create(collections, configuration);
            else
                new FileCreator().CreateFiles(collections, configuration.GetConnectionString("OutputPath"), "shopUser");
        }

        public async Task CreateSetting(int shopId, string country, IConfigurationRoot configuration)
        {
            var collections = new List<DbSetting>();

            if (country == "IE")
            {
                collections = new SettingCollectionBuilder().GeneralInfo().Food().IE().Build();
            }
            else if (country == "UK")
            {
                collections = new SettingCollectionBuilder().GeneralInfo().Food().UK().Build();
            }
            else if (country == "PL")
            {
                collections = new SettingCollectionBuilder().GeneralInfo().Food().PL().Build();
            }

            foreach (var item in collections)
            {
                item.ShopId = shopId;
            }
            if (DapperDataRetriver.Mode == "LIVE")
                await new DataCreator().Create(collections, configuration);
            else
                new FileCreator().CreateFiles(collections, configuration.GetConnectionString("OutputPath"), "setting");
        }

        public async Task CreateShopInfo(int shopId, string country, IConfigurationRoot configuration)
        {
            var shopInfo = new DbShop();
            if (country == "IE")
            {
                shopInfo = new ShopBuilder().GeneralShopInfo().Food().IE().Build();
            }
            else if (country == "UK")
            {
                shopInfo = new ShopBuilder().GeneralShopInfo().Food().UK().Build();
            }
            else if (country == "PL")
            {
                shopInfo = new ShopBuilder().GeneralShopInfo().Food().PL().Build();
            }

            shopInfo.ShopId = shopId;
            if (DapperDataRetriver.Mode == "LIVE")
                await new DataCreator().Create(shopInfo, configuration);
            else
                new FileCreator().CreateFile(shopInfo, configuration.GetConnectionString("OutputPath"), "shop");
        }
    }
}