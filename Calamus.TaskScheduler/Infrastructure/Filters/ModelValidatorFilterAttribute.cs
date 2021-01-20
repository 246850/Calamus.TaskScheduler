using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Calamus.TaskScheduler.Infrastructure.Filters
{
    /// <summary>
    /// 请求模型绑定验证
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class ModelValidatorFilterAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            if (!context.ModelState.IsValid)
            {
                StringBuilder stringBuilder = new StringBuilder(256);
                foreach (KeyValuePair<string, ModelStateEntry> item in context.ModelState)
                {
                    if (item.Value.ValidationState != ModelValidationState.Valid && item.Value.Errors.Count > 0)
                    {
                        stringBuilder.AppendFormat("{0}；", item.Value.Errors.First().ErrorMessage);
                    };
                }
                context.Result = new JsonResult(new CodeResult
                {
                    Code = -1,
                    Msg = stringBuilder.Replace("；", string.Empty, stringBuilder.Length - 1, 1).ToString()
                });
            }
            base.OnActionExecuting(context);
        }

        public override void OnActionExecuted(ActionExecutedContext context)
        {
            base.OnActionExecuted(context);
        }
    }
}
