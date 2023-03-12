namespace App.Infrastructure.Utility.Common
{
    public class BooleanStringUtil
    {
        public static bool IsTrue(string stringValue)
        {
            return ConvertStringValue(stringValue);
        }

        public static bool ConvertStringValue(string stringValue)
        {
            bool returnValue;
            return bool.TryParse(stringValue, out returnValue) && returnValue;
        }
    }
}