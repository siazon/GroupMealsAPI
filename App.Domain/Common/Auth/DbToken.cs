using System;

namespace App.Domain.Common.Auth
{
    public class DbToken : DbEntity
    {
        public DateTime? ExpiredTime { get; set; }
        public int? RoleLevel { get; set; }
        public string UserId { get; set; }
        public string UserName { get; set; }
        public string UserEmail { get; set; }
        public string ServerKey { get; set; }
        public string ShopKey { get; set; }
    }
}