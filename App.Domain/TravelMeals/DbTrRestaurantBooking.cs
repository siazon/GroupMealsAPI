using App.Domain.Enum;
using App.Domain.TravelMeals.Restaurant;
using System;
using System.Collections.Generic;
using System.Net.Cache;

namespace App.Domain.TravelMeals
{
    public class TrDbRestaurantBooking : DbEntity
    {

        public OrderStatusEnum Status { get; set; } = OrderStatusEnum.None;
        public string BookingRef { get; set; }
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
        public string Updater { get; set; }
        public List<BookingDetail> Details { get; set; } = new List<BookingDetail>();
        public List<OperationInfo> Operations { get; set; } = new List<OperationInfo>();
        public List<PaymentInfo> PaymentInfos { get; set; } = new List<PaymentInfo>();

    }
    public class OperationInfo
    {
        public string Operation { get; set; }
        public string Operater { get; set; }
        public DateTime UpdateTime { get; set; }
        public int ModifyType { get; set; }//1接收，2拒绝，3取消，4修改
        public List<ModifyInfo> ModifyInfos { get; set; } = new List<ModifyInfo>();
    }
    public class ModifyInfo
    {
        public int ModifyField { get; set; }//1修改时间，2修改备注，3修改人数，4修改菜单
        public string ModifyLocation { get; set; }
        public string oldValue { get; set; }
        public string newValue { get; set; }
    }
    public class AmountInfo
    {
        public string Id { get; set; }
        public decimal Amount { get; set; }
        public decimal PaidAmount { get; set; }
    }
    public class PaymentInfo : StripeBase
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
        public string Memo { get; set; }
        public string Currency { get; set; }
        public DateTime? SelectDateTime { get; set; }
        public DetailStatusEnum Status { get; set; }//0:defult,1:canceled
        public AcceptStatusEnum AcceptStatus { get; set; }//0:Defult, 1:Accepted, 2:Declined
        public bool Modified { get; set; }
        public string AcceptReason { get; set; }
        public string ContactName { get; set; }
        public string ContactPhone { get; set; }
        public string ContactEmail { get; set; }
        public string ContactInfos { get; set; }

        public RestaurantBillInfo BillInfo { get; set; } = new RestaurantBillInfo();
        public List<BookingCourse> Courses { get; set; } = new List<BookingCourse>();
        public List<AmountInfo> AmountInfos { get; set; } = new List<AmountInfo>();
    }
    public enum DetailStatusEnum
    {
        DEFAULT, Canceled
    }
    public enum AcceptStatusEnum
    {
        DEFAULT, Accepted, Declined
    }
    public class BookingCourse : TrDbRestaurantMenuItem
    {
        public string Id { get; set; }
        public int Qty { get; set; }
        public int ChildrenQty { get; set; }
        public int TableQty { get; set; }
        public string Memo { get; set; }
        public decimal Amount { get; set; }
    }
}