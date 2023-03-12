namespace App.Domain.Common.Setting
{
    public class DbSetting : DbEntity
    {
        public string SettingKey { get; set; }
        public string SettingValue { get; set; }

        public bool? IsServer { get; set; }
    }

    public static class DbSettingExt
    {
        public static DbSetting Clone(this DbSetting source)
        {
            var dest = new DbSetting()
            {
                SettingKey = source.SettingKey,
                SettingValue = source.SettingValue,
                IsServer = source.IsServer,
            };

            return dest;
        }


        public static DbSetting Copy(this DbSetting source, DbSetting copyValue)
        {
            source.SettingKey = copyValue.SettingKey;
            source.SettingValue = copyValue.SettingValue;
            source.IsServer = copyValue.IsServer;

            return source;
        }

    }
}