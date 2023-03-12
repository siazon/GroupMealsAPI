using System;

namespace App.Domain.Common.Shop
{
    public class DbShopSyncStatus: DbEntity
    {
        public bool Result { get; set; }
        public DateTime? CheckTime { get; set; }
        public DateTime? LastCheckTime { get; set; }
    }
}