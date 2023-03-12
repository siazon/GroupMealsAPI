using CommandLine;
using NewFoodCreator.Entity;
using NewFoodCreator.Processor;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace NewFoodCreator
{
    internal class Program
    {
        private static async Task Main(string[] args)
        {
            var shopId = 0;
            var country = "IE";
            var countryList = new string[] { "IE", "UK", "PL" };

            Console.WriteLine("==Welcome to create a new shop==");

            //var builder = new ConfigurationBuilder()
            //    .SetBasePath(Directory.GetCurrentDirectory())
            //    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);

            //IConfigurationRoot configuration = builder.Build();

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


            await new ShopCleaner().CleanShop(shopId);

            // await new ShopCreator().CreateNewShop(shopId, country);

            await new ShopMigrator().MigrateShop(shopId, country);

        }
    }
}