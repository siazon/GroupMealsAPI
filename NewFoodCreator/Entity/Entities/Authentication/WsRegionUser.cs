namespace Takeaway.Service.Contract.Entities.Authentication
{
    public class WsRegionUser : WsEntity
    {
        public string UserName { get; set; }
        public string Password { get; set; }
        public int RegionId { get; set; }
        public int RoleId { get; set; }
    }
}
