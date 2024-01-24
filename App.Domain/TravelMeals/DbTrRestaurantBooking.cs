using App.Domain.Enum;
using App.Domain.TravelMeals.Restaurant;
using System;
using System.Collections.Generic;

namespace App.Domain.TravelMeals
{
    public class TrDbRestaurantBooking : DbEntity
    {
       
        public OrderStatusEnum Status { get; set; } = OrderStatusEnum.None;
        public string CustomerName { get; set; }
        public string CustomerEmail { get; set; }
        public string CustomerPhone { get; set; }
        public string BookingDate { get; set; } = DateTime.Now.ToString("yyyy-MMM-dd");
        public string BookingTime { get; set; } = DateTime.Now.ToString("HH:mm");
        public int NumberOfAdults { get; set; }
        public int NumberOfChildren { get; set; }
        public string BookingNotes { get; set; }
        public string PayCurrency { get; set; }
        public string Creater { get; set; }
        public List<BookingDetail> Details { get; set; }= new List<BookingDetail>();
        public List<OperationInfo> Operations { get; set; } = new List<OperationInfo>();

    }
    public class OperationInfo {
        public string Operation { get; set; }
        public string Operater { get; set; }
        public DateTime UpdateTime { get; set; }
    }
    public class PaymentInfo: StripeBase
    {
        public decimal Amount { get; set; }
        public decimal PaidAmount { get; set; }
    }
    public class BookingDetail
    {
        public string Id { get; set; }
        public string RestaurantId { get; set; }
        public string RestaurantName { get; set; }
        public string RestaurantPhone { get; set; }
        public string RestaurantEmail { get; set; }
        public string SupporterEmail { get; set; }
        public string RestaurantAddress { get; set; }
        public string Currency { get; set; }
        public DateTime? SelectDateTime { get; set; }
        public int AcceptStatus { get; set; }//0:Defult, 1:Accept, 2:Decline
        public string AcceptReason { get; set; }

        public RestaurantBillInfo BillInfo { get; set; } = new RestaurantBillInfo();
        public List<BookingCourses> Courses { get; set; } = new List<BookingCourses>();
        public List<PaymentInfo> PaymentInfos { get; set; } = new List<PaymentInfo>();
    }
    public class BookingCourses : TrDbRestaurantMenuItem
    {
        public int Qty { get; set; }
        public int TableQty { get; set; }
        public string Memo { get; set; }
        public decimal Amount { get; set; }
    }
}