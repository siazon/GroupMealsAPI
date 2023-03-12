using System;

namespace Takeaway.Service.Contract.Entities.Authentication
{
    public class WsToken: WsEntity
    {
        public DateTime? ExpiredTime { get; set; }
        public int? RoleLevel { get; set; }
        public int? UserId { get; set; }
        public string UserName { get; set; }
        public string ServerKey { get; set; }
    }
}