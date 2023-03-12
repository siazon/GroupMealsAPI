using Microsoft.Azure.Documents;

namespace NewFoodCreator.Utility
{
    public static class DapperDataRetriver
    {
        public const string ConnectionString = "Data Source=w9p2t7yzm5.database.windows.net;Initial Catalog=takeaway_online;Persist Security Info=True;User ID=sibo;Password=123456Abc;MultipleActiveResultSets=True";

        public static string Mode = "Local";
    }
}