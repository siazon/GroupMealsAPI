namespace App.Domain.Common.Shop
{
    public class DbShopNews : DbEntity
    {
        public string News { get; set; }
    }

    public static class DbShopNewsExt
    {
        public static DbShopNews Clone(this DbShopNews source)
        {
            var dest = new DbShopNews()
            {
                News = source.News,
            };

            return dest;
        }


        public static DbShopNews Copy(this DbShopNews source, DbShopNews copyValue)
        {
            source.News = copyValue.News;

            return source;
        }
    }
}