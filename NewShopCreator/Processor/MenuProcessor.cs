using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using App.Domain.Shop.Menu;
using NewShopCreator.Utility;
using Newtonsoft.Json;

namespace NewShopCreator.Processor
{
    public class MenuProcessor
    {
        public List<SDbMenuCategory> ProcessCategory(int shopId, string filePath)
        {
            //Read File
            var resultDataSet = new FileReader().Read(filePath);

            if (resultDataSet == null)
                return null;


            var collection = new List<SDbMenuCategory>();

            foreach (DataRow row in resultDataSet.Tables[0].Rows)
            {
                var item = new SDbMenuCategory();


                item.Name = row["Name"].ToString();
                item.DisplayName = row["Name"].ToString();
                item.Description = row["Description"].ToString();
                item.WebImageUrl = row["WebImageUrl"].ToString();
                item.IsInternal = BooleanStringUtil.IsTrue(row["IsInternal"].ToString());

                item.CategoryGroupId = IntegerStringUtil.ConvertStringValue(row["CategoryGroupId"].ToString());

                if (IntegerStringUtil.IsInteger(row["IsSupportCategory"].ToString()))
                    item.IsSupportCategory = IntegerStringUtil.ConvertStringValue(row["IsSupportCategory"].ToString());


                item.DisplayExpression = row["DisplayExpression"].ToString();
                item.TranslateName = row["TranslateName"].ToString();

                if (IntegerStringUtil.IsInteger(row["SortOrder"].ToString()))
                    item.SortOrder = IntegerStringUtil.ConvertStringValue(row["SortOrder"].ToString());

                item.Id = Guid.NewGuid().ToString();

                item.TempId = IntegerStringUtil.ConvertStringValue(row["Id"].ToString());

                item.IsActive = true;
                item.ShopId = shopId;
                item.Created = DateTime.UtcNow;


                collection.Add(item);
            }


            return collection;
        }

        internal void ProcessCategoryItem(int shopId, List<SDbMenuCategory> menuCategoryList,
            List<SDbMenuItem> menuItemList)
        {
            foreach (var item in menuItemList)
            {
                var category = menuCategoryList.First(r => r.TempId == item.CategoryId);
                category.MenuItems.Add(item);
            }
        }

        public List<SDbCategoryGroup> ProcessCategoryGroup(int shopId, List<SDbMenuCategory> menuCategoryList)
        {
            var dinner = new SDbCategoryGroup
            {
                Name = "Dinner",
                TranslateName = "Dinner",
                MenuCategoryGroup = 0,
                Id = Guid.NewGuid().ToString(),
                ShopId = shopId,
                Created = DateTime.UtcNow
            };


            var lunch = new SDbCategoryGroup
            {
                Name = "Lunch",
                TranslateName = "Lunch",
                MenuCategoryGroup = 1,
                Id = Guid.NewGuid().ToString(),
                ShopId = shopId,
                Created = DateTime.UtcNow
            };


            dinner.MenuCategories = menuCategoryList.Where(r => r.CategoryGroupId == 0).ToList();

            lunch.MenuCategories = menuCategoryList.Where(r => r.CategoryGroupId == 1).ToList();

            //Init Group
            var categoryGroup = new List<SDbCategoryGroup>();
            categoryGroup.Add(dinner);

            categoryGroup.Add(lunch);

            return categoryGroup;
        }

        public void ProcessMenuItemSelections(int shopId, List<SDbMenuCategory> menuCategoryList,
            List<SDbMenuItem> menuItemList, string filePath)
        {
            //Read File
            var resultDataSet = new FileReader().Read(filePath);

            if (resultDataSet == null)
                return;

            foreach (DataRow row in resultDataSet.Tables[0].Rows)
            {
                var menuItemId = IntegerStringUtil.ConvertStringValue(row["MenuItemId"].ToString());
                var categoryId = IntegerStringUtil.ConvertStringValue(row["CategoryId"].ToString());
                var item = menuItemList.First(r => r.TempId == menuItemId);
                var category = menuCategoryList.First(r => r.TempId == categoryId);

                if (category.IsSupportCategory == 1)
                {
                    var selection = new SDbMenuItemSelection
                    {
                        Selections = category
                    };

                    if (IntegerStringUtil.IsInteger(row["SortOrder"].ToString()))
                        selection.SortOrder = IntegerStringUtil.ConvertStringValue(row["SortOrder"].ToString());

                    item.MenuItemSelections.Add(selection);
                }
                else if (category.IsSupportCategory == 2)
                {
                    var selection = new SDbMenuItemOption
                    {
                        Selections = category
                    };
                    if (IntegerStringUtil.IsInteger(row["SortOrder"].ToString()))
                        selection.SortOrder = IntegerStringUtil.ConvertStringValue(row["SortOrder"].ToString());

                    item.MenuItemOptions.Add(selection);
                }
            }
        }

        public List<SDbMenuItem> ProcessMenuItem(int shopId, string filePath)
        {
            //Read File
            var resultDataSet = new FileReader().Read(filePath);

            if (resultDataSet == null)
                return null;

            var collection = new List<SDbMenuItem>();

            foreach (DataRow row in resultDataSet.Tables[0].Rows)
            {
                var item = new SDbMenuItem();


                item.Name = row["Name"].ToString();
                item.DisplayName = row["Name"].ToString();
                item.Description = row["Description"].ToString();
                item.DisplayName = row["DisplayName"].ToString();
                item.TranslatedName = row["TranslatedName"].ToString();
                if (DecimalStringUtil.IsValid(row["Price"].ToString()))
                    item.Price = DecimalStringUtil.ConvertStringValue(row["Price"].ToString());
                else
                    item.Price = 0;

                item.ImageUrl = row["ImageUrl"].ToString();
                item.VideoLink = row["VideoLink"].ToString();
                item.ExtraComment = row["ExtraComment"].ToString();

                if (IntegerStringUtil.IsInteger(row["StockNumber"].ToString()))
                    item.StockNumber = IntegerStringUtil.ConvertStringValue(row["StockNumber"].ToString());

                item.FoodCategoryId = IntegerStringUtil.ConvertStringValue(row["FoodCategoryId"].ToString());
                item.CategoryId = IntegerStringUtil.ConvertStringValue(row["CategoryId"].ToString());
                item.Tag = row["Tag"].ToString();


                if (IntegerStringUtil.IsInteger(row["SortOrder"].ToString()))
                    item.SortOrder = IntegerStringUtil.ConvertStringValue(row["SortOrder"].ToString());

                item.Id = Guid.NewGuid().ToString();

                item.TempId = IntegerStringUtil.ConvertStringValue(row["Id"].ToString());

                item.IsActive = true;
                item.ShopId = shopId;
                item.Created = DateTime.UtcNow;


                collection.Add(item);
            }


            return collection;
        }


        public bool CreateFiles<T>(List<T> collection, string folderPath, string fileName)
        {
            
            var count = 1;

            foreach (var json in collection.Select(item => JsonConvert.SerializeObject(item)))
            {
                using (var writer =
                    File.AppendText(Path.Combine(folderPath, string.Format("{1}{0}.json", count, fileName))))
                {
                    writer.Write(json);
                }

                count++;
            }


            return true;
        }


        public bool CreateFile<T>(T item, string folderPath, string fileName)
        {
            var json = JsonConvert.SerializeObject(item);

            using (var writer = File.AppendText(Path.Combine(folderPath, string.Format("{0}.json", fileName))))
            {
                writer.Write(json);
            }


            return true;
        }
    }
}