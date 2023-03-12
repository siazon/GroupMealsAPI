using System;
namespace NewShopCreator.Utility
{
    public class DecimalStringUtil
    {
        public static bool IsValid(string value)
        {
            bool isValid = decimal.TryParse(value, out decimal retValue);
            return isValid;

        }


        public static decimal ConvertStringValue(string value)
        {
            if (decimal.TryParse(value, out decimal retValue))
                return retValue;
            else
                throw new Exception("Not Valid");

        }
    }
}
