namespace App.Infrastructure.Exceptions
{
    public class CoreException : System.Exception
    {
        public CoreException(System.Exception ex) : base(ex.Message)
        {
        }

        public CoreException(string message) : base(message)
        {
        }
    }
}