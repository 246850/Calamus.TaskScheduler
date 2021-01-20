using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Calamus.TaskScheduler.Infrastructure.Filters
{
    public class GatewayResultFilterAttribute : IResultFilter
    {
        public void OnResultExecuted(ResultExecutedContext context)
        {
        }

        public void OnResultExecuting(ResultExecutingContext context)
        {
            if (context.Result is EmptyResult)
            {
                context.Result = new JsonResult(CodeResult.Success);
            }
            else if (context.Result is ContentResult)
            {
            }
            else if (context.Result is ObjectResult temp)
            {
                if ((temp.Value as CodeResult) != null)
                {
                    return;
                }
                context.Result = new JsonResult(new CodeResult<object> { Code = 0, Msg = "success", Data = temp.Value});
            }
        }
    }
}
