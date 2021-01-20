using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Calamus.TaskScheduler.Infrastructure.Dtos
{
    public class JobSearchRequest: RequestBase
    {
        public JobSearchRequest()
        {
            
        }
        public string Name { get; set; }
        public string Group { get; set; }

        
    }
}
