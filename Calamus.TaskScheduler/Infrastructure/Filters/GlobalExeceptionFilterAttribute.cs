using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;
using System;

namespace Calamus.TaskScheduler.Infrastructure.Filters
{
    /// <summary>
    /// 全局异常处理过滤器 - 支持依赖注入 - TypeFilter
    /// </summary>
    public class GlobalExceptionFilterAttribute : TypeFilterAttribute
    {
        public GlobalExceptionFilterAttribute() : base(typeof(GlobalExeceptionFilter))
        {

        }

        private class GlobalExeceptionFilter : IExceptionFilter
        {
            private readonly ILogger<GlobalExeceptionFilter> _logger;
            public GlobalExeceptionFilter(ILogger<GlobalExeceptionFilter> logger)
            {
                _logger = logger;
            }
            public void OnException(ExceptionContext context)
            {
                Exception exception = context.Exception;
                while (exception.InnerException!= null)
                {
                    exception = exception.InnerException;
                }
                _logger.LogError(exception, exception.Message);


                context.ExceptionHandled = true;
                context.Result = new JsonResult(new CodeResult { Code = -1, Msg = exception.Message });
            }
        }
    }
}
