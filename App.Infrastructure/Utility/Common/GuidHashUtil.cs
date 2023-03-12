using System;

namespace App.Infrastructure.Utility.Common
{
    public class GuidHashUtil
    {
        public static string GetUniqueOrderKey()
        {
            return GetUniqueKey(8).ToUpper();
        }

        public static string GetKey(int digit)
        {
            return GetUniqueKey(digit).ToUpper();
        }

        public static string Get6DigitNumber()
        {
            Random generator = new Random();
            String r = generator.Next(0, 999999).ToString("D6");
            return r;
        }

        private static string GetUniqueKey(int length)
        {
            string guidResult = string.Empty;

            while (guidResult.Length < length)
            {
                // Get the GUID.
                guidResult += Guid.NewGuid().ToString().GetHashCode().ToString("x");
            }

            // Make sure length is valid.
            if (length <= 0 || length > guidResult.Length)
                throw new ArgumentException("Length must be between 1 and " + guidResult.Length);

            // Return the first length bytes.
            return guidResult.Substring(0, length);
        }
    }
}