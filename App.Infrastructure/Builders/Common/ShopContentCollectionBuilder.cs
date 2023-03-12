using App.Domain.Common.Content;
using System;
using System.Collections.Generic;
using App.Domain.Enum;

namespace App.Infrastructure.Builders.Common
{
    public class ShopContentCollectionBuilder
    {
        private readonly List<DbShopContent> _collection;

        public ShopContentCollectionBuilder()
        {
            _collection = new List<DbShopContent>();
        }

        public ShopContentCollectionBuilder GeneralInfo()
        {
            _collection.Add(new DbShopContent()
            {
                Id = Guid.NewGuid().ToString(),
                Created = DateTime.UtcNow,
                Updated = DateTime.UtcNow,
                Key = EmailTemplateEnum.ForgetPassword.ToString(),
                Subject = "Here is your retrieve password code",
                Content = StaticContent.FForgetPassword,
            });

            return this;
        }

        public ShopContentCollectionBuilder Food()
        {
            return this;
        }

        public ShopContentCollectionBuilder IE()
        {
            _collection.Add(new DbShopContent()
            {
                Id = Guid.NewGuid().ToString(),
                Created = DateTime.UtcNow,
                Updated = DateTime.UtcNow,
                Key = EmailTemplateEnum.BookingEmailTemplateShop.ToString(),
                Subject = "Booking",
                Content = StaticContent.FBookingEmailTemplateShop,
            });

            _collection.Add(new DbShopContent()
            {
                Id = Guid.NewGuid().ToString(),
                Created = DateTime.UtcNow,
                Updated = DateTime.UtcNow,
                Key = EmailTemplateEnum.OrderEmailTemplateContactShop.ToString(),
                Subject = "Booking",
                Content = StaticContent.FOrderEmailTemplateContactShop,
            });

            _collection.Add(new DbShopContent()
            {
                Id = Guid.NewGuid().ToString(),
                Created = DateTime.UtcNow,
                Updated = DateTime.UtcNow,
                Key = EmailTemplateEnum.OrderEmailTemplateCustomer.ToString(),
                Subject = "Thank you for your order",
                Content = StaticContent.FOrderEmailTemplateCustomer,
            });

            _collection.Add(new DbShopContent()
            {
                Id = Guid.NewGuid().ToString(),
                Created = DateTime.UtcNow,
                Updated = DateTime.UtcNow,
                Key = EmailTemplateEnum.OrderEmailTemplateShop.ToString(),
                Subject = "You have a new Order",
                Content = StaticContent.FOrderEmailTemplateShop,
            });

            _collection.Add(new DbShopContent()
            {
                Id = Guid.NewGuid().ToString(),
                Created = DateTime.UtcNow,
                Updated = DateTime.UtcNow,
                Key = EmailTemplateEnum.OrderEmailTemplateAcceptOrder.ToString(),
                Subject = "Your Order on the way",
                Content = StaticContent.FOrderEmailTemplateAcceptOrder,
            });


            _collection.Add(new DbShopContent()
            {
                Id = Guid.NewGuid().ToString(),
                Created = DateTime.UtcNow,
                Updated = DateTime.UtcNow,
                Key = EmailTemplateEnum.OrderEmailTemplateDeclineOrder.ToString(),
                Subject = "Order Declined",
                Content = StaticContent.FOrderEmailTemplateDeclineOrder,
            });

            _collection.Add(new DbShopContent()
            {
                Id = Guid.NewGuid().ToString(),
                Created = DateTime.UtcNow,
                Updated = DateTime.UtcNow,
                Key = EmailTemplateEnum.OrderEmailTemplateTrello.ToString(),
                Subject = "Order Refund",
                Content = StaticContent.FOrderEmailTemplateTrello,
            });

            _collection.Add(new DbShopContent()
            {
                Id = Guid.NewGuid().ToString(),
                Created = DateTime.UtcNow,
                Updated = DateTime.UtcNow,
                Key = EmailTemplateEnum.MenuEmailTemplateStaticMenu.ToString(),
                Subject = "Shop Menu",
                Content = StaticContent.FMenuEmailTemplateStaticMenu,
            });

            _collection.Add(new DbShopContent()
            {
                Id = Guid.NewGuid().ToString(),
                Created = DateTime.UtcNow,
                Updated = DateTime.UtcNow,
                Key = EmailTemplateEnum.MenuEmailTemplateStaticFullMenu.ToString(),
                Subject = "Shop Menu",
                Content = StaticContent.FMenuEmailTemplateStaticFullMenu,
            });

            return this;
        }

        public ShopContentCollectionBuilder UK()
        {
            return this;
        }

        public ShopContentCollectionBuilder PL()
        {
            return this;
        }

        public List<DbShopContent> Build()
        {
            return _collection;
        }
    }
}