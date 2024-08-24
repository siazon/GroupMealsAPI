using App.Domain.Enum;
using App.Domain.TravelMeals.Restaurant;
using System;
using System.Collections.Generic;
using System.Net.Cache;
using System.Text.Json.Serialization;

namespace App.Domain.TravelMeals
{
    public class TrDbRestaurantBooking : DbEntity
    {
        [JsonIgnore]
        public OrderStatusEnum Status { get; set; } = OrderStatusEnum.None;
        public string BookingRef { get; set; }
        public string CustomerName { get; set; }
        public string CustomerEmail { get; set; }
        public string CustomerPhone { get; set; }
        public string PayCurrency { get; set; }
        public double Rebate { get; set; }
        public List<DbBooking> Details { get; set; } = new List<DbBooking>();
        public List<OperationInfo> Operations { get; set; } = new List<OperationInfo>();
        public List<PaymentInfo> PaymentInfos { get; set; } = new List<PaymentInfo>();

    }
    public class DbPaymentInfo: PaymentInfo {
    
    }
    public class DbOpearationInfo: OperationInfo
    { 
    
    }
    public class OperationInfo: DbEntity
    {
        public string ReferenceId { get; set; }
        public string Operation { get; set; }
        public string Operater { get; set; }
        public DateTime UpdateTime { get; set; }
        public int ModifyType { get; set; }//1接收，2拒绝，3取消，4修改
        public List<ModifyInfo> ModifyInfos { get; set; } = new List<ModifyInfo>();
    }
    public class ModifyInfo
    {
        public string ModifyField { get; set; }
        public string ModifyLocation { get; set; }
        public string oldValue { get; set; }
        public string newValue { get; set; }
    }
    public class AmountInfo
    {
        public string Id { get; set; }
        public string PaymentId { get; set; }
        public decimal Amount { get; set; }
        public decimal PaidAmount { get; set; }
    }
    public class PaymentInfo : StripeBase
    {
        public string PaymentType { get; set; }
        public decimal Amount { get; set; }
        public decimal PaidAmount { get; set; }
        public decimal RefundAmount { get; set; }
        public string Currency { get; set; }
        public DateTime PayTime { get; set; }
        
    }
    public class DbBooking: BookingDetail
    { 
    

    }
    public class BookingDetail: DbEntity
    {
        public string BookingRef { get; set; }
        public string PaymentId { get; set; }
        public BookingRestaurantInfo Restaurant { get; set; }=new BookingRestaurantInfo();
        public string RestaurantId { get; set; }
        public string RestaurantName { get; set; }
        public string RestaurantAddress { get; set; }
        public string RestaurantPhone { get; set; }
        public string EmergencyPhone { get; set; }
        public string RestaurantWechat { get; set; }
        public string RestaurantEmail { get; set; }
        public string RestaurantCountry { get; set; }
        public string SupporterEmail { get; set; }
        public string Memo { get; set; }
        public string PayCurrency { get; set; }
        public string Currency { get; set; }
        public DateTime? SelectDateTime { get; set; }
        public OrderStatusEnum Status { get; set; }//0:defult,1:canceled
        public AcceptStatusEnum AcceptStatus { get; set; }//0:Defult, 1:Accepted, 2:Declined
        public bool Modified { get; set; }
        public string AcceptReason { get; set; }
        public BookingCustomerInfo Customer { get; set; } =new BookingCustomerInfo();

        public string ContactName { get; set; }
        public string ContactPhone { get; set; }
        public string ContactEmail { get; set; }
        public string ContactWechat { get; set; }
        public string ContactInfos { get; set; }
        public string GroupRef { get; set; }
        public string Remark { get; set; }

        public RestaurantBillInfo BillInfo { get; set; } = new RestaurantBillInfo();
        public List<BookingCourse> Courses { get; set; } = new List<BookingCourse>();
        public List<AmountInfo> AmountInfos { get; set; } = new List<AmountInfo>();
    }
    public class BookingRestaurantInfo {
        public string RestaurantId { get; set; }
        public string RestaurantName { get; set; }
        public string RestaurantAddress { get; set; }
        public string RestaurantPhone { get; set; }
        public string EmergencyPhone { get; set; }
        public string RestaurantWechat { get; set; }
        public string RestaurantEmail { get; set; }
        public string RestaurantCountry { get; set; }
    }
    public class BookingCustomerInfo {
        public string ContactName { get; set; }
        public string ContactPhone { get; set; }
        public string ContactEmail { get; set; }
        public string ContactWechat { get; set; }
        public string ContactInfos { get; set; }
    }
    public enum DetailStatusEnum
    {
        DEFAULT, Canceled
    }

    public class BookingCourse : TrDbRestaurantMenuItem
    {
        public string Id { get; set; }
        /// <summary>计算时必填</summary>
        public int Qty { get; set; }
        /// <summary>计算时必填</summary>
        public int ChildrenQty { get; set; }
        public int TableQty { get; set; }
        public string Memo { get; set; }
        public decimal Amount { get; set; }

        public override string ToString()
        {
            return  $"id:{Id},name:{MenuItemName},qty:{Qty},childrenQty:{ChildrenQty},price:{Price},childrenPrice{ChildrenPrice},MenuCalculateType:{MenuCalculateType},Category:{Category}" ;
        }
    }
}