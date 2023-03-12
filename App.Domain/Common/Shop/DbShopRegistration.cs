using System;

namespace App.Domain.Common.Shop
{
    public class DbShopRegistration
    {
        //Owner Info
        public string OwnerName { get; set; }

        public string VatNumber { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? ExpireDate { get; set; }

        //List of package ids
        public int? PackageId { get; set; }

        public string PackageNotes { get; set; }

        public int? AgentId { get; set; }
        public DateTime? AgentStartDate { get; set; }

        //ShopTypeEnum
        public int? ShopType { get; set; }

        public string BankName { get; set; }
        public int? IsBusiness { get; set; }
        public string NameOnAccount { get; set; }
        public string IBAN { get; set; }
        public string BIC { get; set; }
        public string AccountNumber { get; set; }
        public string SortCode { get; set; }

        public string SpecialOfferNotes { get; set; }
        public string RegistrationNotes { get; set; }
    }
}