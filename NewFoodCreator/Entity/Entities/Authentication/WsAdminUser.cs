namespace Takeaway.Service.Contract.Entities.Authentication
{
    public class WsAdminUser : WsEntity
    {
        /// <summary>
        /// User name for the login user
        /// </summary>
        public string UserName { get; set; }

        /// <summary>
        /// Password for the login user. Note: this is only used for validation purpose
        /// </summary>
        public string Password { get; set; }
       

        /// <summary>
        /// URL of the server
        /// </summary>
        public string ServerUrl { get; set; }

        /// <summary>
        /// Token for authentication
        /// </summary>
        public string AuthToken { get; set; }

        public int UserGroupId { get; set; }

        public string Pin { get; set; }

        
    }
}