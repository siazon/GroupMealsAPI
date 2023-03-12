namespace Takeaway.Service.Contract.Entities.Payment
{
    public class WsCheckout : WsEntity
    {
        public decimal Amount { get; set; }

        public string Description { get; set; }

        public string SessionId { get; set; }
        public string OrderRef { get; set; }

        public string ContactName { get; set; }
        public string ContactEmail { get; set; }
        public string OrderDescription { get; set; }
        public string ApiKey { get; set; }
        
        public string SuccessUrl { get; set; }
        public string CancelUrl { get; set; }
    }
}
