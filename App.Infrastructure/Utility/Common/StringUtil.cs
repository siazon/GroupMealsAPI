using System.Text.RegularExpressions;

namespace App.Infrastructure.Utility.Common
{
    public interface IStringUtil
    {
        string TrimForCount(string input, int count);
    }

    public class StringUtil : IStringUtil
    {
        public string TrimForCount(string input, int count)
        {
            return Regex.Replace(input, @"[\s-]", "").Substring(0, count);
        }
    }
}