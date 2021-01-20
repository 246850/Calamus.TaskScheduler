using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;

namespace Calamus.TaskScheduler.Infrastructure.Dtos
{
    public abstract class RequestBase
    {
        public RequestBase()
        {
            Properties = new Dictionary<string, List<SelectListItem>>();
        }
        public IDictionary<string, List<SelectListItem>> Properties { get; set; }
    }
}
