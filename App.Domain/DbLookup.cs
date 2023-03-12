namespace App.Domain
{
    public class DbLookup : DbEntity
    {
        public string Name { get; set; }
        public string DisplayName { get; set; }
        public string Code { get; set; }
        public string Description { get; set; }
        public string LookupValue { get; set; }
    }
}