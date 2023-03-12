using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace KingfoodIO.Application.ActionResults
{
    public class UnauthoriedRequestObjectResult : ObjectResult
    {
        public UnauthoriedRequestObjectResult(object error)
            : base(error)
        {
            StatusCode = StatusCodes.Status401Unauthorized;
        }
    }
}
