namespace Takeaway.Service.Contract.Entities.Payment
{
    public class WsPaymentInfo : WsEntity
    {
        public bool IsSuccess { get; set; }

        public string PaymentRef { get; set; }

        public int? CostCredit { get; set; }

        public string ErrorMessage { get; set; }
    }
}