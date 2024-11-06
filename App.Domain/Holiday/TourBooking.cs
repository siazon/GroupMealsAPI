using App.Domain.Enum;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace App.Domain.Holiday
{
    public class TourBooking : StripeBase
    {
        public string Comment { get; set; }

        public int? NumberOfPeople { get; set; }
        public int? NumberOfAgedOrStudent { get; set; }
        public int? NumberOfChild { get; set; }

        public string Name { get; set; }
        public string PhoneNumber { get; set; }
        public string WeChat { get; set; }
        public string Email { get; set; }
        public string Ref { get; set; }
        public OrderStatusEnum Status { get; set; } = OrderStatusEnum.None;
        public DbTour Tour { get; set; }

        public string SelectDate { get; set; }

        public TourBooking()
        {
        }

        
    }

    public static class TourBookingExt
    {
        public static TourBooking Clone(this TourBooking source)
        {
            var dest = new TourBooking();
            dest.Comment = source.Comment;
            dest.NumberOfPeople = source.NumberOfPeople;
            dest.NumberOfAgedOrStudent = source.NumberOfAgedOrStudent;
            dest.NumberOfChild = source.NumberOfChild;
            dest.Name = source.Name;
            dest.PhoneNumber = source.PhoneNumber;
            dest.WeChat = source.WeChat;
            dest.Email = source.Email;
            dest.Tour = source.Tour;
            dest.SelectDate = source.SelectDate;
            dest.StripeClientSecretKey= source.StripeClientSecretKey;
            dest.StripeCustomerId = source.StripeCustomerId;
            dest.StripePaymentMethodId = source.StripePaymentMethodId;
            dest.StripePriceId = source.StripePriceId;
            dest.StripeProductId = source.StripeProductId;
            dest.StripeReceiptUrl = source.StripeReceiptUrl;
            dest.StripeSetupIntent = source.StripeSetupIntent;
            return dest;
        }

        public static TourBooking Copy(this TourBooking source, TourBooking copyValue)
        {

            source.Comment = copyValue.Comment;
            source.NumberOfPeople = copyValue.NumberOfPeople;
            source.Name = copyValue.Name;
            source.PhoneNumber = copyValue.PhoneNumber;
            source.WeChat = copyValue.WeChat;
            source.Email = copyValue.Email;
            source.Tour = copyValue.Tour;
            source.SelectDate = copyValue.SelectDate;

            return source;
        }

    }
}