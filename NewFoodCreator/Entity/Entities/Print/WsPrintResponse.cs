namespace Takeaway.Service.Contract.Entities.Print
{
    public class WsPrintResponse:WsEntity
    {
        public string ErrorMessage { get; set; }
        public bool PrintSucceed { get; set; }
    }
}