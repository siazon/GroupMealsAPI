namespace App.Domain.Common.Shop
{
    public class DbShopUser : DbEntity
    {
        public string UserName { get; set; }

        public string Password { get; set; }

        public string Email { get; set; }

        public string ServerUrl { get; set; }

        public string AuthToken { get; set; }

        /// <summary>
        ///     1. Global Admin, 2. Global Support, 21, Shop Admin, 22, Shop User
        /// </summary>
        public int UserGroupId { get; set; }

        public string Pin { get; set; }
        
        public string FullName { get; set; }

        public string ShopCode { get; set; }
    }


    public static class DbShopUserExt
    {
        public static DbShopUser ClearForOutPut(this DbShopUser source)
        {
            source.Password = "";
            return source;
        }

        public static DbShopUser Clone(this DbShopUser source)
        {
            var dest = new DbShopUser()
            {
                UserName = source.UserName,
                Email = source.Email,
                Pin = source.Pin,
                UserGroupId= source.UserGroupId,
                IsActive= source.IsActive,
                FullName = source.FullName
            };


            return dest;
        }


        public static DbShopUser Copy(this DbShopUser source, DbShopUser copyValue)
        {
            source.UserName = copyValue.UserName;
            source.Email = copyValue.Email;
            source.Pin = copyValue.Pin;
            source.UserGroupId = copyValue.UserGroupId;
            source.IsActive = copyValue.IsActive;
            source.FullName = copyValue.FullName;

            return source;
        }
    }
}