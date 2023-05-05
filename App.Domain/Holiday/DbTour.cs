using App.Domain.Enum;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace App.Domain.Holiday
{
    public class DbTour : DbEntity
    {


        [Column(TypeName = "decimal(18, 2)")]
        public decimal? Price { get; set; }
        [Column(TypeName = "decimal(18, 2)")]
        public decimal? ChildPrice { get; set; }
        [Column(TypeName = "decimal(18, 2)")]
        public decimal? ConcessionPrice { get; set; }

        public int? CountryId { get; set; }

        public int? RatingNumber { get; set; }

        //Top Rate or Popular
        public int? TagCat { get; set; }
        public string Category { get; set; }

        public string Content { get; set; }

        public string ContentCn { get; set; }

        public string ContentEn { get; set; }

        public string ContentTc { get; set; }
        public TourInfo TourInfo { get; set; }
        public double TourTimeSpan { get; set; }
        public int FreeRefundDays { get; set; }
        public string Image { get; set; }
        public List<string> Images { get; set; }


        public List<string> Tags { get; set; }

        public List<DateTime?> AvailableDates { get; set; }

        public List<string> BreadCrumbHeader { get; set; }
        public List<string> BreadCrumbHeaderTc { get; set; }
        public List<string> BreadCrumbHeaderCn { get; set; }

        public DbTour()
        {
            Images = new List<string>();
            Tags = new List<string>();
            BreadCrumbHeaderTc = new List<string>();
            BreadCrumbHeaderCn = new List<string>();
            AvailableDates = new List<DateTime?>();
        }
    }
}