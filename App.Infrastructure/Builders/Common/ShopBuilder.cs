using App.Domain.Common.Shop;

namespace App.Infrastructure.Builders.Common
{
    public class ShopBuilder
    {
        private DbShop _shop;

        public ShopBuilder()
        {
            _shop = new DbShop();
        }

        public ShopBuilder GeneralShopInfo()
        {
            this._shop.ShopName = "New Shop Name";

            return this;
        }

        public DbShop Build()
        {
            return _shop;
        }
    }
}