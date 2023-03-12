using App.Domain.Common.Setting;
using App.Infrastructure.Utility.Common;
using System.Collections.Generic;
using System.Linq;
using App.Domain.Enum;

namespace App.Infrastructure.Extensions.Entity
{
    public static class DbSettingExt
    {
        public static string GetSettingPublicApiKey(this List<DbSetting> source)
        {
            return GetSettingByKey(source, ServerSettingEnum.AppPublicKey);
        }

        public static string GetSettingPaymentApiKey(this List<DbSetting> source)
        {
            return GetSettingByKey(source, ServerSettingEnum.AppStripKey);
        }

        public static bool GetEmailSettingsAllowEmailClient(this List<DbSetting> source)
        {
            var settingValue = GetSettingByKey(source, ServerSettingEnum.AppAllowEmailClient);
            return BooleanStringUtil.ConvertStringValue(settingValue);
        }

        public static bool GetEmailSettingsAllowEmailShop(this List<DbSetting> source)
        {
            var settingValue = GetSettingByKey(source, ServerSettingEnum.AppAllowEmailShop);
            return BooleanStringUtil.ConvertStringValue(settingValue);
        }

        public static bool GetEmailSettingsSmtpEnabled(this List<DbSetting> source)
        {
            var settingValue = GetSettingByKey(source, ServerSettingEnum.AppSmtpenabled);
            return BooleanStringUtil.ConvertStringValue(settingValue);
        }

        public static string GetEmailSettingsAPIKey(this List<DbSetting> source)
        {
            return GetSettingByKey(source, ServerSettingEnum.AppEmailApiKey);
        }

        public static string GetEmailSettingsSmtpUserName(this List<DbSetting> systemSetting)
        {
            return GetSettingByKey(systemSetting, ServerSettingEnum.AppSmtpUsername);
        }

        public static string GetEmailSettingsSmtpPassword(this List<DbSetting> systemSetting)
        {
            return GetSettingByKey(systemSetting, ServerSettingEnum.AppSmtppassword);
        }

        public static string GetEmailSettingsSmtpServer(this List<DbSetting> systemSetting)
        {
            return GetSettingByKey(systemSetting, ServerSettingEnum.AppSmtpServer);
        }

        private static string GetSettingByKey(List<DbSetting> systemSetting, string systemKey)
        {
            var setting = systemSetting.FirstOrDefault(r => r.SettingKey == systemKey);
            return setting == null ? string.Empty : setting.SettingValue;
        }

    }
}