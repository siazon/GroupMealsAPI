using Newtonsoft.Json;
using System;

namespace App.Domain
{
    public class DbEntity
    {
        [JsonProperty(PropertyName = "id")] public string Id { get; set; }

        public string Guid { get; set; }
        public string Hash { get; set; }
        public int? TempId { get; set; }
        public int? ShopId { get; set; }

        public DateTime? Created { get; set; }
        public DateTime? Updated { get; set; }

        public int? SortOrder { get; set; }

        public bool? IsActive { get; set; }

        public bool Results { get; set; }
    }
}