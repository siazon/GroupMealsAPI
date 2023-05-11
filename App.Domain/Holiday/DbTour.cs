using App.Domain.Enum;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace App.Domain.Holiday
{
    public class DbTour : DbEntity
    {


        [Column(TypeName = "decimal(18, 2)")]
        public decimal? Price { get; set; } = 0;
        [Column(TypeName = "decimal(18, 2)")]
        public decimal? ChildPrice { get; set; } = 0;
        [Column(TypeName = "decimal(18, 2)")]
        public decimal? ConcessionPrice { get; set; } = 0;

        public int? CountryId { get; set; } = 0;

        public int? RatingNumber { get; set; } = 0;

        //Top Rate or Popular
        public int? TagCat { get; set; } = 0;
        public string Category { get; set; } = "Example Text";

        public string ContentCn { get; set; } = "Example Text";

        public string ContentEn { get; set; } = "Example Text";

        public string ContentTc { get; set; } = "Example Text";
        public TourInfo TourInfo { get; set; }=new TourInfo();
        public double TourTimeSpan { get; set; } = 0;
        public int FreeRefundDays { get; set; } = 0;
        public string Image { get; set; } = "Example Text";
        public List<string> Images { get; set; }


        public List<string> Tags { get; set; }

        public List<DateTime?> AvailableDates { get; set; }

        public List<string> BreadCrumbHeaderEn { get; set; }
        public List<string> BreadCrumbHeaderTc { get; set; }
        public List<string> BreadCrumbHeaderCn { get; set; }

        public DbTour()
        {
            Images = new List<string>();
            Tags = new List<string>();
            BreadCrumbHeaderEn=new List<string>();
            BreadCrumbHeaderTc = new List<string>();
            BreadCrumbHeaderCn = new List<string>();
            AvailableDates = new List<DateTime?>();
        }
    }
}