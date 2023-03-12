namespace App.Infrastructure.Exceptions
{
    public class ServiceException : CoreException
    {
        public ServiceException(System.Exception ex) : base(ex)
        {
        }

        public ServiceException(string message) : base(message)
        {
        }
    }
}