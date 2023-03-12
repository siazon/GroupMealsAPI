namespace Takeaway.Service.Contract.Entities.Payment
{
    public class WsPayment: WsEntity
    {
        public decimal Amount { get; set; }
        public string Description { get; set; }
        public string CardNumber { get; set; }
        public string CardExpirationYear { get; set; }
        public string CardExpirationMonth { get; set; }
        public string CardName { get; set; }
        public string CardCvc { get; set; }

        public string OrderRef { get; set; }
        public string ContactName { get; set; }

        public string PaymentMethodId { get; set; }
        public string PaymentIntentId { get; set; }

        public string PaymentIntentClientSecret { get; set; }
    }
}