namespace App.Domain.Common.Address
{
    public class DbAddress : DbEntity
    {
        public string Address1 { get; set; }
        public string Address2 { get; set; }
        public string Postcode { get; set; }
        public string GoogleMap { get; set; }
        public string Latitude { get; set; }
        public string Longitude { get; set; }

        public string PlaceId { get; set; }
    }
}