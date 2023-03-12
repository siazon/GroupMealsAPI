using App.Domain.Exception;
using App.Infrastructure.Exceptions;
using App.Infrastructure.Utility.Common;
using FluentValidation;
using KingfoodIO.Application.ActionResults;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;

namespace KingfoodIO.Application.Filter
{
    public class HttpGlobalExceptionFilter : IExceptionFilter
    {
        private readonly IHostingEnvironment _env;
        private readonly ILogger<HttpGlobalExceptionFilter> _logger;
        private readonly ILogManager _nlogger;

        public HttpGlobalExceptionFilter(IHostingEnvironment env, ILogger<HttpGlobalExceptionFilter> logger, ILogManager nlogger)
        {
            _env = env;
            _logger = logger;
            _nlogger = nlogger;
        }

        public void OnException(ExceptionContext context)
        {
            _logger.LogError(new EventId(context.Exception.HResult),
                context.Exception,
                context.Exception.Message);

            _nlogger.LogError($"ResultCode:{context.Exception.HResult}, Ex: {context.Exception.Message}-{context.Exception.StackTrace}");


            if (context.Exception.GetType() == typeof(ValidationException))
            {
                var json = new JsonErrorResponse
                {
                    Messages = new[] { context.Exception.Message }
                };

                context.Result = new BadRequestObjectResult(json);
            }
            else if (context.Exception.GetType() == typeof(ServiceException)
                     || context.Exception.GetType() == typeof(DataRepositoryException))
            {
                var json = new JsonErrorResponse
                {
                    Messages = new[] { context.Exception.Message }
                };

                context.Result = new NotFoundRequestObjectResult(json);
            }
            else if (context.Exception.GetType() == typeof(AuthException))

            {
                var json = new JsonErrorResponse
                {
                    Messages = new[] { context.Exception.Message }
                };

                context.Result = new UnauthoriedRequestObjectResult(json);
            }
            else
            {
                var json = new JsonErrorResponse
                {
                    Messages = new[] { "An error occurred. Try it again." }
                };

                if (_env.IsDevelopment())
                {
                    json.DeveloperMessage = context.Exception;
                }

                context.Result = new InternalServerErrorObjectResult(json);
            }


            _nlogger.LogError($"Result:{context.Result}");
            context.ExceptionHandled = true;
        }
    }
}