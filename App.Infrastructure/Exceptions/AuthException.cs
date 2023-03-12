namespace App.Infrastructure.Exceptions
{
    public class AuthException : System.Exception
    {
        public AuthException(System.Exception ex) : base(ex.Message)
        {
        }

        public AuthException(string message) : base(message)
        {
        }
    }
}