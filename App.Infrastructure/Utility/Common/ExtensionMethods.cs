using Microsoft.AspNetCore.Http;
using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using TimeZoneConverter;

namespace App.Infrastructure.Utility.Common
{
    public static class ExtensionMethods
    {
        public static async Task<string> GetRawBodyAsync(this HttpRequest request, Encoding encoding = null)
        {
            if (!request.Body.CanSeek)
            {
                // We only do this if the stream isn't *already* seekable,
                // as EnableBuffering will create a new stream instance
                // each time it's called
                request.EnableBuffering();
            }

            request.Body.Position = 0;

            var reader = new StreamReader(request.Body, encoding ?? Encoding.UTF8);

            var body = await reader.ReadToEndAsync().ConfigureAwait(false);

            request.Body.Position = 0;

            return body;
        }
        public static string CreateMD5(this string input)
        {
            // Use input string to calculate MD5 hash
            using (System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create())
            {
                byte[] inputBytes = System.Text.Encoding.ASCII.GetBytes(input);
                byte[] hashBytes = md5.ComputeHash(inputBytes);

                return Convert.ToHexString(hashBytes); // .NET 5 +

                // Convert the byte array to hexadecimal string prior to .NET 5
                // StringBuilder sb = new System.Text.StringBuilder();
                // for (int i = 0; i < hashBytes.Length; i++)
                // {
                //     sb.Append(hashBytes[i].ToString("X2"));
                // }
                // return sb.ToString();
            }
        }
        public static DateTime GetLocaTimeByIANACode(this DateTime dateTime, string IANACode)
        {
            var time = DateTime.UtcNow;
            try
            {

                time = TimeZoneInfo.ConvertTimeFromUtc(dateTime,
                 TZConvert.GetTimeZoneInfo(IANACode));
            }
            catch (Exception ex)
            {

                throw;
            }
            return time;
        }
        public static DateTime GetTimeZoneByIANACode(this DateTime dateTime, string IANACode)
        {
            var time = DateTime.UtcNow;
            try
            {

                time = TimeZoneInfo.ConvertTimeToUtc(dateTime, TZConvert.GetTimeZoneInfo(IANACode));
            }
            catch (Exception ex)
            {

                throw;
            }
            return time;
        }
        public static string GetEnumDescription(this Enum value)
        {
            FieldInfo fi = value.GetType().GetField(value.ToString());

            DescriptionAttribute[] attributes = fi.GetCustomAttributes(typeof(DescriptionAttribute), false) as DescriptionAttribute[];

            if (attributes != null && attributes.Any())
            {
                return attributes.First().Description;
            }

            return value.ToString();
        }
    }

}
