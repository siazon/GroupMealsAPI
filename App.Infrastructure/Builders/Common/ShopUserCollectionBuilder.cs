using App.Domain.Common.Shop;
using App.Infrastructure.Utility.Common;
using System;
using System.Collections.Generic;

namespace App.Infrastructure.Builders.Common
{
    public class ShopUserCollectionBuilder
    {
        private readonly List<DbShopUser> _collection;

        public ShopUserCollectionBuilder()
        {
            _collection = new List<DbShopUser>();
        }

        public ShopUserCollectionBuilder GeneralInfo()
        {
            _collection.Add(new DbShopUser()
            {
                Id = Guid.NewGuid().ToString(),
                Created = DateTime.UtcNow,
                Updated = DateTime.UtcNow,
                UserName = "admin",
                Password = new EncryptionHelper().EncryptString("password"),
                IsActive = true,
                UserGroupId = 1,
                FullName = "Administrator",
                Pin = "9192",
            });

            _collection.Add(new DbShopUser()
            {
                Id = Guid.NewGuid().ToString(),
                Created = DateTime.UtcNow,
                Updated = DateTime.UtcNow,
                UserName = "support",
                Password = new EncryptionHelper().EncryptString("indasoft.ie"),
                IsActive = true,
                UserGroupId = 2,
                FullName = "MM Support",
                Pin = "9876",
            });

            _collection.Add(new DbShopUser()
            {
                Id = Guid.NewGuid().ToString(),
                Created = DateTime.UtcNow,
                Updated = DateTime.UtcNow,
                UserName = "owner",
                Password = new EncryptionHelper().EncryptString("password01"),
                IsActive = true,
                UserGroupId = 21,
                FullName = "shop owner",
                Pin = "9999",
            });

            _collection.Add(new DbShopUser()
            {
                Id = Guid.NewGuid().ToString(),
                Created = DateTime.UtcNow,
                Updated = DateTime.UtcNow,
                UserName = "user",
                Password = new EncryptionHelper().EncryptString("user"),
                IsActive = true,
                UserGroupId = 22,
                FullName = "shop user",
                Pin = "1234",
            });

            return this;
        }

        public List<DbShopUser> Build()
        {
            return _collection;
        }

        public ShopUserCollectionBuilder Food()
        {
            return this;
        }

        public ShopUserCollectionBuilder IE()
        {
            return this;
        }

        public ShopUserCollectionBuilder UK()
        {
            return this;
        }

        public ShopUserCollectionBuilder PL()
        {
            return this;
        }
    }
}