using System;
using System.IO;
using CommandLine;
using Microsoft.Extensions.Configuration;
using NewTravelMealCreator.Entity;
using NewTravelMealCreator.Processor;

namespace NewTravelMealCreator
{
    class Program
    {
        static void Main(string[] args)
        {
            var shopId = 0;

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
                   });
            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

            Console.WriteLine("==Processing Menus==");
            var menuProcessor = new MenuProcessor();
            
            
            var restaurants = menuProcessor.ProcessRestaurants(shopId,configuration.GetConnectionString("RestaurantPath"));
            
            var categories =
                menuProcessor.ProcessRestaurantCategories(shopId, configuration.GetConnectionString("CategoryPath"));

            var finalCategories =
                menuProcessor.ProcessRestaurantMenuItem(shopId, configuration.GetConnectionString("MenuItemPath"),categories);


            foreach (var item in restaurants)
            {
                item.Categories.AddRange(finalCategories.FindAll(r=>r.RestaurantId== item.TempId));
            }
            
           var completed= menuProcessor.CreateFile(restaurants, configuration.GetConnectionString("OutputPath"));

           

        }
    }
}
