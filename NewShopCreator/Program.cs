using System;
using System.IO;
using System.Linq;
using App.Infrastructure.Builders;
using App.Infrastructure.Builders.Common;
using App.Infrastructure.Builders.Food;
using CommandLine;
using Microsoft.Extensions.Configuration;
using NewShopCreator.Entity;
using NewShopCreator.Processor;

namespace NewShopCreator
{
    class Program
    {
        static void Main(string[] args)
        {
            var shopId = 0;
            var country = "IE";
            var countryList = new string[]{"IE","UK", "PL"};

            Console.WriteLine("==Welcome to create a new shop==");

            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);

            IConfigurationRoot configuration = builder.Build();
            
            Parser.Default.ParseArguments<Options>(args)
                   .WithParsed<Options>(o =>
                   {
                       if (o.ShopId > 0)
                       {
                           Console.WriteLine($"ShopId:{o.ShopId}. Please press enter to proceed");
                           Console.ReadKey();
                           shopId = o.ShopId;
                       }
                       else
                       {
                           Console.WriteLine("Invalid input press any key to exit");
                           Console.ReadKey();
                           Environment.Exit(-1);
                       }
                       
                       if (!string.IsNullOrEmpty(o.Country) && countryList.Contains(o.Country))
                       {
                           Console.WriteLine($"Country:{o.Country}. Please press enter to proceed");
                           Console.ReadKey();
                           country = o.Country;
                       }
                       else
                       {
                           Console.WriteLine("Invalid input press any key to exit");
                           Console.ReadKey();
                           Environment.Exit(-1);
                       }
                   });
            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

            Console.WriteLine("==Processing Menu==");
            var menuProcessor = new MenuProcessor();

            var menuItemList= menuProcessor.ProcessMenuItem(shopId, configuration.GetConnectionString("MenuItemPath"));
            var menuCategoryList = menuProcessor.ProcessCategory(shopId,configuration.GetConnectionString("MenuPath"));
            menuProcessor.ProcessMenuItemSelections(shopId, menuCategoryList, menuItemList, configuration.GetConnectionString("MenuItemSelectionPath"));
            menuProcessor.ProcessCategoryItem(shopId, menuCategoryList, menuItemList);

            var completed= menuProcessor.CreateFiles(menuCategoryList, configuration.GetConnectionString("OutputPath"),"menu");

            Console.WriteLine("==Processing Shop Info==");
            if (country == "IE")
            {
                var shopInfo = new ShopBuilder().GeneralShopInfo().Build();
                shopInfo.ShopId = shopId;
                menuProcessor.CreateFile(shopInfo, configuration.GetConnectionString("OutputPath"),"shop");
            }
            
            
            Console.WriteLine("==Processing Delivery Options ==");
            if (country == "IE")
            {
                var deliveryOptions = new FDeliveryOptionCollectionBuilder().GeneralInfo().Build();
                foreach (var option in deliveryOptions)
                {
                    option.ShopId = shopId;
                }
                menuProcessor.CreateFiles(deliveryOptions, configuration.GetConnectionString("OutputPath"),"deliveryoption");
            }

            Console.WriteLine("==Processing Setting Options ==");
            if (country == "IE")
            {
                var settings = new SettingCollectionBuilder().GeneralInfo().Build();
                foreach (var option in settings)
                {
                    option.ShopId = shopId;
                }
                menuProcessor.CreateFiles(settings, configuration.GetConnectionString("OutputPath"),"setting");
            }
            
            
            Console.WriteLine("==Processing Shop Content Options ==");
            if (country == "IE")
            {
                var collection = new ShopContentCollectionBuilder().GeneralInfo().Build();
                foreach (var option in collection)
                {
                    option.ShopId = shopId;
                }
                menuProcessor.CreateFiles(collection, configuration.GetConnectionString("OutputPath"), "shopcontent");
            }
            
            
            Console.WriteLine("==Processing Shop User Collection ==");
            if (country == "IE")
            {
                var collection = new ShopUserCollectionBuilder().GeneralInfo().Build();
                foreach (var option in collection)
                {
                    option.ShopId = shopId;
                }
                menuProcessor.CreateFiles(collection, configuration.GetConnectionString("OutputPath"), "shopUser");
            }
            
            Console.WriteLine("==Processing Delivery Cost Collection ==");
            if (country == "IE")
            {
                var collection = new FDeliveryCostCollectionBuilder().GeneralInfo().Build();
                foreach (var option in collection)
                {
                    option.ShopId = shopId;
                }
                menuProcessor.CreateFiles(collection, configuration.GetConnectionString("OutputPath"), "deliverycost");
            }

        }
    }
}
