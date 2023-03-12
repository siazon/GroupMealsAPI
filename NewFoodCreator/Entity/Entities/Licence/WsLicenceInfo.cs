namespace Takeaway.Service.Contract.Entities.Licence
{
    public class WsLicenceInfo:WsEntity
    {
        public bool IsValidLicence { get; set; }
        public string Hwid { get; set; }
    }
}