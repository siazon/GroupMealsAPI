using App.Domain.Common.Auth;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace App.Domain.Common.Customer
{
    public class DbCustomer : DbEntity
    {
        public string UserName { get; set; }
        public string ContactName { get; set; }
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
        public virtual ICollection<Role> UserRoles { get; set; }
        public bool IsVerity { get; set; }

    }


    public static class DbCustomerExt
    {

        public static DbCustomer Clone(this DbCustomer source)
        {
            var customer = new DbCustomer
            {
                UserName = source.UserName,
                Email = source.Email,
                ContactName = source.ContactName,
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
                Area = source.Area
            };
            return customer;
        }

        public static DbCustomer Copy(this DbCustomer source, DbCustomer copyValue)
        {
            source.UserName = copyValue.UserName;
            source.Email = copyValue.Email;
            source.ContactName = copyValue.ContactName;
            source.WeChat = copyValue.WeChat;
            source.Phone = copyValue.Phone;
            source.Latitude = copyValue.Latitude;
            source.Longitude = copyValue.Longitude;
            source.Address = copyValue.Address;
            source.IpAddress = copyValue.IpAddress;
            source.MarketPermission = copyValue.MarketPermission;
            source.Eircode = copyValue.Eircode;
            source.IsActive = copyValue.IsActive;
            source.DeliveryCharge = copyValue.DeliveryCharge;
            source.PinCode = copyValue.PinCode;
            source.Area = copyValue.Area;
            return source;
        }


        public static DbCustomer ClearForOutPut(this DbCustomer source)
        {
            source.Password = "";
            source.ResetCode = "";
            return source;
        }

        public static List<DbCustomer> ClearForOutPut(this List<DbCustomer> source)
        {
            foreach (var item in source)
            {
                item.Password = "";
                item.ResetCode = "";
            }
            return source;
        }
    }

}