using App.Domain.Common.Shop;
using App.Domain.Food.Delivery;
using App.Domain.Food.Discount;
using App.Domain.Food.Menu;
using App.Infrastructure.Builders.Common;
using Dapper;
using Microsoft.Extensions.Configuration;
using NewFoodCreator.Utility;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Takeaway.Service.Contract.Entities.Shop;

namespace NewFoodCreator.Processor
{
    public class ShopMigrator
    {
        private readonly ShopCreator creator;

        public ShopMigrator()
        {
            creator = new ShopCreator();
        }

        public async Task MigrateShop(int shopId, string country)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);

            IConfigurationRoot configuration = builder.Build();

            DapperDataRetriver.Mode = configuration.GetConnectionString("Mode");

            //1.Shop Info(Dummy)
            /* Missing Fields
            1. Discount By Range = 2 row of discount
            2. //1. By Area, 2 By Distance
               public int? DeliveryChargeMode { get; set; }
               This is client side flag
            3. shopInfo.MaxPaymentAmount
            4.   public DbShopRegistration RegistrationInfo { get; set; }   Move to CRM
            */
            await MigrateShopInfo(shopId, configuration, country);

            //2.Delivery Area
            await MigrateDeliveryArea(shopId, configuration, country);
            //3.Delivery Cost
            await MigrateDeliveryCost(shopId, configuration, country);
            //4.Delivery Option
            await MigrateDeliveryOptions(shopId, configuration, country);

            //5.Discount Offer
            /*
             * Cannot validate Registration discount and discount by Amount
             */
            await MigrateDiscountOffer(shopId, configuration, country);
            //6.Email Template
            await MigrateEmailTemplate(shopId, configuration, country);
            //7.Menu Category //Need to validate Menu Option
            await MigrateMenu(shopId, configuration, country);
            //8.Option Item(Takeaway Internal)
            await MigrateOptionItem(shopId, configuration, country);
            //9.Settings
            await MigrateSettings(shopId, configuration, country);
            //10.Shop Users
            await MigrateShopUser(shopId, configuration, country);

            //11. Clean Orders
            await MigrateOrders(shopId, configuration, country);
        }

        private async Task MigrateOrders(int shopId, IConfigurationRoot configuration, string country)
        {
        }

        private async Task MigrateShopUser(int shopId, IConfigurationRoot configuration, string country)
        {
            await creator.CreateShopUser(shopId, country, configuration);
        }

        private async Task MigrateSettings(int shopId, IConfigurationRoot configuration, string country)
        {
            await creator.CreateSetting(shopId, country, configuration);
        }

        private async Task MigrateOptionItem(int shopId, IConfigurationRoot configuration, string country)
        {
        }

        private async Task MigrateMenu(int shopId, IConfigurationRoot configuration, string country)
        {
            var response = await new RestClient().GetService(string.Format("https://indasoftkingfoodwebapi.azurewebsites.net/api/GMenuItemApi/GetMenuItemCategoryByGroupId?groupId=0&shopId={0}", shopId));
            var resultContent = response.Content.ReadAsStringAsync().Result;
            var result = JsonConvert.DeserializeObject<List<WsMenuItemCategory>>(resultContent);

            if (result == null)
                return;

            var responseItems = await new RestClient().GetService(string.Format("https://indasoftkingfoodwebapi.azurewebsites.net/api/GMenuItemApi/GetMenuItemByCategoryGroupId?groupId=0&shopId={0}", shopId));
            var resultContentItems = responseItems.Content.ReadAsStringAsync().Result;
            var resultItems = JsonConvert.DeserializeObject<List<WsMenuItem>>(resultContentItems);

            if (resultItems == null)
                return;

            var collections = new List<FDbMenuCategory>();
            var subItemSelections = new List<FDbMenuCategory>();
            foreach (var item in result)
            {
                var category = new FDbMenuCategory
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = item.Name,
                    DisplayName = item.DisplayName,
                    Description = item.Description,
                    WebImageUrl = item.WebImageUrl,
                    IsInternal = item.IsInternal,
                    CategoryGroupId = 1,
                    DisplayExpression = item.DisplayExpression,
                    TranslateName = item.TranslateName,
                    SortOrder = item.SortOrder,
                    IsActive = true,
                    ShopId = shopId,
                    Created = DateTime.UtcNow,
                    Updated = DateTime.UtcNow
                };

                var menuItems = resultItems.Where(r => r.MenuItemCategoryId == item.Id);

                foreach (var menuItem in menuItems)
                {
                    var newMenuItem = new FDbMenuItem
                    {
                        Id = Guid.NewGuid().ToString(),
                        Name = menuItem.Name,
                        DisplayName = menuItem.DisplayName,
                        TranslatedName = menuItem.TranslatedName,
                        Price = menuItem.Price,
                        OrginalPrice = menuItem.Price,
                        InternalPrice = menuItem.Price,
                        Description = menuItem.Description,
                        ImageUrl = menuItem.ImageUrl,
                        VideoLink = menuItem.VideoLink,
                        ExtraComment = menuItem.ExtraComment,
                        Comment = menuItem.Comment,
                        SortOrder = menuItem.SortOrder,
                        FoodCategoryId = menuItem.FoodCategoryId,
                        Code = menuItem.MenuItemCategoryId.ToString(),
                        IsActive = true,
                        ShopId = shopId,
                        Created = DateTime.UtcNow,
                        Updated = DateTime.UtcNow
                    };

                    foreach (var selection in menuItem.SubItemSelections)
                    {
                        var subCategory = CreateNewCategory(subItemSelections, selection, shopId);
                        newMenuItem.MenuItemSelections.Add(new FDbMenuItemSelection()
                        {
                            Id = Guid.NewGuid().ToString(),
                            TempId = subCategory.TempId,
                            Selections = subCategory,
                            Created = DateTime.UtcNow,
                            Updated = DateTime.UtcNow,
                        });
                    }

                    category.MenuItems.Add(newMenuItem);
                }

                collections.Add(category);
            }

            collections.AddRange(subItemSelections);

            if (DapperDataRetriver.Mode == "LIVE")
                await new DataCreator().Create(collections, configuration);
            else
                new FileCreator().CreateFiles(collections, configuration.GetConnectionString("OutputPath"), "menu");
        }

        private FDbMenuCategory CreateNewCategory(List<FDbMenuCategory> subItemSelections, WsMenuSubItem selection, int shopId)
        {
            var existingSelection = subItemSelections.FirstOrDefault(r => r.TempId == selection.Id);

            if (existingSelection != null)
            {
                return existingSelection;
            }

            var newSupportCatgory = new FDbMenuCategory();
            newSupportCatgory.Id = Guid.NewGuid().ToString();
            newSupportCatgory.Name = selection.Name;
            newSupportCatgory.Description = selection.Description;
            newSupportCatgory.TempId = selection.Id;
            newSupportCatgory.IsSupportCategory = 1;
            newSupportCatgory.ShopId = shopId;
            newSupportCatgory.Created = DateTime.UtcNow;
            newSupportCatgory.Updated = DateTime.UtcNow;

            foreach (var item in selection.Selections)
            {
                var newItem = new FDbMenuItem();
                newItem.Id = Guid.NewGuid().ToString();
                newItem.Name = item.Name;
                newItem.DisplayName = item.DisplayName;
                newItem.TranslatedName = item.TranslatedName;
                newItem.Price = item.Price;
                newItem.OrginalPrice = item.Price;
                newItem.InternalPrice = item.Price;
                newItem.Description = item.Description;
                newItem.ImageUrl = item.ImageUrl;
                newItem.VideoLink = item.VideoLink;
                newItem.ExtraComment = item.ExtraComment;
                newItem.Comment = item.Comment;
                newItem.SortOrder = item.SortOrder;
                newItem.FoodCategoryId = item.FoodCategoryId;
                newItem.Code = item.MenuItemCategoryId.ToString();
                newItem.IsActive = true;
                newItem.ShopId = shopId;
                newItem.Created = DateTime.UtcNow;
                newItem.Updated = DateTime.UtcNow;
                newSupportCatgory.MenuItems.Add(newItem);
            }

            subItemSelections.Add(newSupportCatgory);
            return newSupportCatgory;
        }

        private async Task MigrateEmailTemplate(int shopId, IConfigurationRoot configuration, string country)
        {
            await creator.CreateShopContent(shopId, country, configuration);
        }

        private async Task MigrateDiscountOffer(int shopId, IConfigurationRoot configuration, string country)
        {
            var response = await new RestClient().GetService(string.Format("https://indasoftkingfoodwebapi-staging.azurewebsites.net/api/DiscountOfferApi/GetAllDiscountOffer?shopId={0}", shopId));
            var resultContent = response.Content.ReadAsStringAsync().Result;
            var result = JsonConvert.DeserializeObject<List<WsDiscountOffer>>(resultContent);

            if (result == null)
                return;

            var collections = new List<FDbDiscountOffer>();
            foreach (var item in result)
            {
                if (!item.Isactive.HasValue || item.Isactive.Value)
                    continue;

                collections.Add(new FDbDiscountOffer()
                {
                    Id = Guid.NewGuid().ToString(),
                    OrderDiscount = item.DiscountValue,
                    IsActive = true,
                    ShopId = shopId,
                    Created = DateTime.UtcNow,
                    Updated = DateTime.UtcNow,
                });
            }

            if (DapperDataRetriver.Mode == "LIVE")
                await new DataCreator().Create(collections, configuration);
            else
                new FileCreator().CreateFiles(collections, configuration.GetConnectionString("OutputPath"), "discount");
        }

        private async Task MigrateDeliveryOptions(int shopId, IConfigurationRoot configuration, string country)
        {
            await creator.CreateDeliveryOption(shopId, country, configuration);
        }

        private async Task MigrateDeliveryCost(int shopId, IConfigurationRoot configuration, string country)
        {
            var response = await new RestClient().GetService(string.Format("https://indasoftkingfoodwebapi-staging.azurewebsites.net/api/DeliveryCostApi/GetDeliveryCosts?shopId={0}", shopId));
            var resultContent = response.Content.ReadAsStringAsync().Result;
            var result = JsonConvert.DeserializeObject<List<WsDeliveryCost>>(resultContent);

            if (result == null)
                return;

            var collections = new List<FDbDeliveryCost>();
            foreach (var item in result)
            {
                collections.Add(new FDbDeliveryCost()
                {
                    Id = Guid.NewGuid().ToString(),
                    FromDistance = item.FromDistance,
                    ToDistance = item.ToDistance,
                    DeliveryCostAmount = item.DeliveryCostAmount,
                    ShopId = shopId,
                    Created = DateTime.UtcNow,
                    Updated = DateTime.UtcNow,
                });
            }

            if (DapperDataRetriver.Mode == "LIVE")
                await new DataCreator().Create(collections, configuration);
            else
                new FileCreator().CreateFiles(collections, configuration.GetConnectionString("OutputPath"), "deliveryCost");
        }

        private async Task MigrateDeliveryArea(int shopId, IConfigurationRoot configuration, string country)
        {
            var response = await new RestClient().GetService(string.Format("https://indasoftkingfoodwebapi-staging.azurewebsites.net/api/DeliveryCostApi/GetDeliveryCosts?deliveryGroupId=1&shopId={0}", shopId));
            var resultContent = response.Content.ReadAsStringAsync().Result;
            var result = JsonConvert.DeserializeObject<List<WsDeliveryArea>>(resultContent);

            if (result == null)
                return;

            var collections = new List<FDbDeliveryArea>();
            foreach (var item in result)
            {
                collections.Add(new FDbDeliveryArea()
                {
                    Id = Guid.NewGuid().ToString(),
                    Area = item.Area,
                    DeliveryCost = item.DeliveryCost,
                    ShopId = shopId,
                    Created = DateTime.UtcNow,
                    Updated = DateTime.UtcNow,
                });
            }

            if (DapperDataRetriver.Mode == "LIVE")
                await new DataCreator().Create(collections, configuration);
            else
                new FileCreator().CreateFiles(collections, configuration.GetConnectionString("OutputPath"), "deliveryArea");
        }

        private async Task MigrateShopInfo(int shopId, IConfigurationRoot configuration, string country)
        {
            var response = await new RestClient().GetService(string.Format("https://indasoftkingfoodwebapi-staging.azurewebsites.net/api/ShopApi/GetShopInfo?shopId={0}", shopId));
            var resultContent = response.Content.ReadAsStringAsync().Result;
            var result = JsonConvert.DeserializeObject<WsShop>(resultContent);

            if (result == null)
                return;

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

            shopInfo.ShopName = result.ShopName;
            shopInfo.ShopNumber = result.ShopNumber;
            shopInfo.ShopNumber2 = result.ShopNumber2;
            shopInfo.ShopMobile = result.ShopMobile;
            shopInfo.Email = result.Email;
            shopInfo.Website = result.ShopUrl;
            shopInfo.PromotionBannerText = result.PromotionBannerText;
            shopInfo.DeliveryChargeMode = 2;
            shopInfo.DeliveryMin = result.DeliveryMin;
            shopInfo.BookingHourLength = result.BookingMin;
            shopInfo.CollectionMin = result.CollectionMin;
            shopInfo.IsOpenByOwner = result.IsOpenByOwner;
            shopInfo.TakePayment = true;
            shopInfo.TakeCash = true;
            shopInfo.DeliveryOnly = result.DeliveryOnly;
            shopInfo.CollectionOnly = result.CollectionOnly;
            shopInfo.DeliveryCharge = result.DeliveryCharge;
            shopInfo.MinOrder = result.MinOrder;
            shopInfo.ServiceCharge = result.ServiceCharge;
            shopInfo.IsActive = result.Isactive;
            shopInfo.ShopPaymentInfo.Statement = result.Statement;
            shopInfo.ShopAddressInfo.Address1 = result.ShopAddress1;
            shopInfo.ShopAddressInfo.Address2 = result.ShopAddress2;
            shopInfo.ShopAddressInfo.GoogleMap = result.GoogleMap;
            shopInfo.ShopAddressInfo.Latitude = result.Latitude.ToString();
            shopInfo.ShopAddressInfo.Longitude = result.Longitude.ToString();
            shopInfo.ShopLink.IoSlink = result.IoSlink;
            shopInfo.ShopLink.Androidlink = result.Androidlink;

          
            string sql = string.Format("SELECT dateOfWeek as DayOfWeek, startingTime as ShopCollectionStartTime, endingTime as ShopDeliveryEndTime, endingTime as ShopCollectionEndTime,  deliveryStartingTime as ShopDeliveryStartTime FROM shopGeneralHour where shopid={0};", shopId);
            var collections = new List<DbShopGeneralOpenHour>();

            using (var connection = new SqlConnection(DapperDataRetriver.ConnectionString))
            {
                collections = connection.Query<DbShopGeneralOpenHour>(sql).ToList();
            }

            foreach (var item in collections)
            {
                item.ShopId = shopId;
                item.Id = Guid.NewGuid().ToString();
                item.Created = DateTime.UtcNow;
                item.Updated = DateTime.UtcNow;
            }

            shopInfo.ShopGeneralHours = collections;

            if (DapperDataRetriver.Mode == "LIVE")
                await new DataCreator().Create(shopInfo, configuration);
            else
                new FileCreator().CreateFile(shopInfo, configuration.GetConnectionString("OutputPath"), "shop");

           
        }
    }
}