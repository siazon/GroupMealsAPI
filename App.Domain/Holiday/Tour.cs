using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace App.Domain.Holiday
{
    public class Tour : DbEntity
    {
        public string Name { get; set; }
        public string NameCn { get; set; }

        public string DepartFromCn { get; set; }
        public string DepartFromTc { get; set; }
        public string CountryCn { get; set; }
        public string CountryTc { get; set; }
       
        public string NameEn { get; set; }
        public string NameTc { get; set; }

        [Column(TypeName = "decimal(18, 2)")]
        public decimal? Price { get; set; }

        public int? CountryId { get; set; }

        public int? RatingNumber { get; set; }

        //Top Rate or Popular
        public int? TagCat { get; set; }
        public string Category { get; set; }

        public string Content { get; set; }

        public string ContentCn { get; set; }

        public string ContentEn { get; set; }

        public string ContentTc { get; set; }

        public string Image { get; set; }
        public List<string> Images { get; set; }

        public List<string> StopPointCn { get; set; }

        public List<string> Tags { get; set; }

        public List<string> StopPointTc { get; set; }
        public List<DateTime?> AvailableDates { get; set; }

        public List<string> BreadCrumbHeaderTc { get; set; }
        public List<string> BreadCrumbHeaderCn { get; set; }

        public Tour()
        {
            Images = new List<string>();
            StopPointCn = new List<string>();
            Tags = new List<string>();
            StopPointTc = new List<string>();
            BreadCrumbHeaderTc = new List<string>();
            BreadCrumbHeaderCn = new List<string>();
            AvailableDates = new List<DateTime?>();
        }
    }
}