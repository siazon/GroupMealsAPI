using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using App.Domain.TravelMeals.Restaurant;
using Newtonsoft.Json;
using NewTravelMealCreator.Utility;

namespace NewTravelMealCreator.Processor
{
    public class MenuProcessor
    {
        public MenuProcessor()
        {
        }
        
        public List<TrDbRestaurantMenuCategory> ProcessRestaurantMenuItem(int shopId, string filePath, List<TrDbRestaurantMenuCategory> categories)
        {
            var resultDataSet = new FileReader().Read(filePath);
            if (resultDataSet == null)
                return null;
            var collection = new List<TrDbRestaurantMenuItem>();
            
            foreach (DataRow row in resultDataSet.Tables[0].Rows)
            {
                var item = new TrDbRestaurantMenuItem();
                item.MenuItemName=row["Name"].ToString();
                item.MenuItemNameCn=row["NameCn"].ToString();
                item.MenuItemDescription = row["Description"].ToString();
                item.MenuItemDescriptionCn = row["DescriptionCn"].ToString();
                item.CategoryId = IntegerStringUtil.ConvertStringValue(row["CategoryId"].ToString());
                item.Id = Guid.NewGuid().ToString();
                item.Guid = Guid.NewGuid().ToString();
                item.ShopId = shopId;
                item.Created = DateTime.UtcNow;
                collection.Add(item);
            }
            
            foreach (var item in categories)
            {
                item.MenuItems.AddRange(collection.FindAll(r=>r.CategoryId==item.TempId));
            }
            
            return categories;
        }
        
        public List<TrDbRestaurantMenuCategory> ProcessRestaurantCategories(int shopId, string filePath)
        {
            var resultDataSet = new FileReader().Read(filePath);
            if (resultDataSet == null)
                return null;
            var collection = new List<TrDbRestaurantMenuCategory>();
            foreach (DataRow row in resultDataSet.Tables[0].Rows)
            {
                var item = new TrDbRestaurantMenuCategory();
                item.CategoryName= row["Name"].ToString();
                item.CategoryNameCn= row["NameCn"].ToString();
                item.CategoryDescription= row["Description"].ToString();
                item.CategoryDescriptionCn = row["DescriptionCn"].ToString();
                item.RestaurantId= IntegerStringUtil.ConvertStringValue(row["RestaurantId"].ToString());
                item.TempId= IntegerStringUtil.ConvertStringValue(row["Id"].ToString());
                item.Id = Guid.NewGuid().ToString();
                item.Guid = Guid.NewGuid().ToString();
                item.ShopId = shopId;
                item.Created= DateTime.UtcNow;
                collection.Add(item);
            }
            

            return collection;
        }
        
        public List<TrDbRestaurant> ProcessRestaurants(int shopId, string filePath)
        {
            var resultDataSet = new FileReader().Read(filePath);
            if (resultDataSet == null)
                return null;
            var collection = new List<TrDbRestaurant>();
            foreach (DataRow row in resultDataSet.Tables[0].Rows)
            {
                var item = new TrDbRestaurant();
                item.Id = Guid.NewGuid().ToString();
                item.StoreName= row["Name"].ToString();
                item.StoreNameCn= row["NameCN"].ToString();
                item.ShopAddress1= row["Address1"].ToString();
                item.ShopAddress2= row["Address2"].ToString();
                item.Website= row["Website"].ToString();
                item.Description=row["Description"].ToString();
                item.DescriptionCn=row["DescriptionCn"].ToString();
                item.DescriptionHtml=row["DescriptionHtml"].ToString();
                item.DescriptionHtmlCn=row["DescriptionHtmlCN"].ToString();
                item.Image =row["Image"].ToString();
                item.City= row["City"].ToString();
                item.Country =row["Country"].ToString();
                item.PhoneNumber= row["PhoneNumber"].ToString();
                item.Email= row["Email"].ToString();
                item.ContactEmail= row["ContactEmail"].ToString();
                item.ShopOpenHours= row["ShopOpenHours"].ToString();
                item.GoogleMap= row["Googlemap"].ToString();
                if(!string.IsNullOrEmpty(row["Image1"].ToString()))
                    item.ImageList.Add(row["Image1"].ToString());
                if(!string.IsNullOrEmpty(row["Image2"].ToString()))
                    item.ImageList.Add(row["Image2"].ToString());
                if(!string.IsNullOrEmpty(row["Image3"].ToString()))
                    item.ImageList.Add(row["Image3"].ToString());
                if(!string.IsNullOrEmpty(row["Image4"].ToString()))
                    item.ImageList.Add(row["Image4"].ToString());
                item.BookingHourLength=IntegerStringUtil.ConvertStringValue(row["BookingHourLength"].ToString());
                item.Rating= row["Rating"].ToString();
                item.FoodCategory= row["FoodCategory"].ToString();
                item.Guid = Guid.NewGuid().ToString();
                item.ShopId = shopId;
                item.Created= DateTime.UtcNow;
                item.IsActive = true;
                item.TempId= IntegerStringUtil.ConvertStringValue(row["Id"].ToString());
                collection.Add(item);
            }

            return collection;
        }
        

        public bool CreateFile(List<TrDbRestaurant> collection, string folderPath)
        {
            var json = JsonConvert.SerializeObject(collection);

            using (var writer = System.IO.File.AppendText(Path.Combine(folderPath, "restaurants.json")))
            {
                writer.Write(json);

            }

            return true;

        }
        
    }
}
