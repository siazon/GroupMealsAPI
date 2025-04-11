using App.Domain.Common.Auth;
using App.Domain.TravelMeals;
using App.Domain.TravelMeals.Restaurant;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace App.Domain.Common.Customer
{
    public class DbCustomer : DbEntity
    {
        public string UserName { get; set; }
        //public string ContactName { get; set; }
        public string Email { get; set; }
        public string WeChat { get; set; }
        public string Phone { get; set; }

        public string Password { get; set; }
        public string ResetCode { get; set; }
        public string Latitude { get; set; }
        public string Longitude { get; set; }
        public string Address { get; set; }
        public string IpAddress { get; set; }
        public bool? MarketPermission { get; set; }
        public string Eircode { get; set; }
        public DateTime? FirstPurchaseDate { get; set; }
        public string Area { get; set; }
        [Column(TypeName = "decimal(18, 2)")] 
        public decimal? DeliveryCharge { get; set; }
        public string PinCode { get; set; }
        public string  InitPassword { get; set; }
        public virtual ICollection<string> UserRoles { get; set; }
        public bool IsVerity { get; set; }
        public bool IsBoss { get; set; }
        public bool IsOldCustomer { get; set; }
        public string StripeCustomerId { get; set; }
        public string Restaurants { get; set; }
        public ulong AuthValue { get; set; }
        public int BookingQty { get; set; }
        public List<CommonParam> Favorites { get; set; }
        public List<DbBooking> CartInfos { get; set; }=new List<DbBooking>();
        public PaymentTypeEnum RewardType { get; set; } = PaymentTypeEnum.Percentage;
        public double Reward { get; set; }
        public string DeviceToken { get; set; }
    }


    public static class DbCustomerExt
    {

        public static DbCustomer Clone(this DbCustomer source)
        {
            var customer = new DbCustomer
            {
                UserName = source.UserName,
                Email = source.Email,
                //ContactName = source.ContactName,
                WeChat = source.WeChat,
                DeliveryCharge = source.DeliveryCharge,
                Phone = source.Phone,
                Latitude = source.Latitude,
                Longitude = source.Longitude,
                Address = source.Address,
                IpAddress = source.IpAddress,
                MarketPermission = source.MarketPermission,
                Eircode = source.Eircode,
                IsActive = source.IsActive,
                PinCode = source.PinCode,
                Area = source.Area,
                 IsVerity = source.IsVerity,
                  AuthValue = source.AuthValue,
                   IsBoss = source.IsBoss,
                   IsOldCustomer=source.IsOldCustomer,
                   StripeCustomerId=source.StripeCustomerId,
                  
                    
            };
            return customer;
        }

        public static DbCustomer Copy(this DbCustomer source, DbCustomer copyValue)
        {
            source = new DbCustomer()
            {
                 Id=copyValue.Id,
                UserName = copyValue.UserName,
                Email = copyValue.Email,
                WeChat = copyValue.WeChat,
                Phone = copyValue.Phone,
                Password = source.Password,
                ResetCode= source.ResetCode,
                PinCode = source.PinCode,
                Latitude = copyValue.Latitude,
                Longitude = copyValue.Longitude,
                Address = copyValue.Address,
                IpAddress = copyValue.IpAddress,
                MarketPermission = copyValue.MarketPermission,
                Eircode = copyValue.Eircode,
                FirstPurchaseDate = copyValue.FirstPurchaseDate,
                Area = copyValue.Area,
                DeliveryCharge = copyValue.DeliveryCharge,
                IsVerity = copyValue.IsVerity,
                IsActive = copyValue.IsActive,
                IsBoss = copyValue.IsBoss,
                IsOldCustomer = copyValue.IsOldCustomer,
                StripeCustomerId = copyValue.StripeCustomerId,
                AuthValue = copyValue.AuthValue,
                Favorites = copyValue.Favorites,
                CartInfos = copyValue.CartInfos,
                RewardType = copyValue.RewardType,
                Reward = copyValue.Reward,
                Created = copyValue.Created,
                Creater = copyValue.Creater,
                IsDeleted = copyValue.IsDeleted,
                ShopId = copyValue.ShopId,
                SortOrder = copyValue.SortOrder,
                Updated = copyValue.Updated,
                Updater = copyValue.Updater,
                UserRoles = copyValue.UserRoles,
                DeviceToken = copyValue.DeviceToken,
            };

            return source;
        }


        public static DbCustomer ClearForOutPut(this DbCustomer item)
        {
            item.Password = "";
            item.ResetCode = "";
            item.PinCode = "";
            item.DeviceToken = "";
            //item.CartInfos.Clear();
            //source.InitPassword = "";
            return item;
        }

        public static List<DbCustomer> ClearForOutPut(this List<DbCustomer> source)
        {
            foreach (var item in source)
            {
                item.ClearForOutPut();
            }
            return source;
        }
    }

}