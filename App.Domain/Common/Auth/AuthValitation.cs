using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace App.Domain.Common.Auth
{
    public static class AuthValitation
    {
        public static bool AuthVerify(this ulong AtuthValue, ulong role)
        {
            var site = Math.Pow(2, role);
            return (AtuthValue & (uint)site) == site;
        }
    }
    public enum AuthEnum
    {
        Admin = 0,
        Restaurant = 1,
        Supporter = 2,
    }
}
