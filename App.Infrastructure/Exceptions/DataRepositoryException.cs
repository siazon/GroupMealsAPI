namespace App.Infrastructure.Exceptions
{
    public class DataRepositoryException : CoreException
    {
        public DataRepositoryException(System.Exception ex) : base(ex)
        {
        }

        public DataRepositoryException(string message) : base(message)
        {
        }
    }
}