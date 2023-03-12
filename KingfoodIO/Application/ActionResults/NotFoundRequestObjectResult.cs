using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace KingfoodIO.Application.ActionResults
{
    public class NotFoundRequestObjectResult : ObjectResult
    {
        public NotFoundRequestObjectResult(object error)
            : base(error)
        {
            StatusCode = StatusCodes.Status404NotFound;
        }
    }
}
