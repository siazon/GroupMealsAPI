namespace NewFoodCreator.Utility
{
    public class IntegerStringUtil
    {
        public static bool IsInteger(string value)
        {
            var retValue = 0;
            return int.TryParse(value, out retValue);

        }


        public static int ConvertStringValue(string value)
        {
            var retValue = 0;
            return int.TryParse(value, out retValue) ? retValue : 0;
        }
    }
}
