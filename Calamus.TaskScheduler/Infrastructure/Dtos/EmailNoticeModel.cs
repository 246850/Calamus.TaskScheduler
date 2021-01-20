using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Calamus.TaskScheduler.Infrastructure.Dtos
{
    public class EmailNoticeModel
    {
        public DateTime ErrorTime { get; set; }
        public string Message { get; set; }
        public string StackTrace { get; set; }
    }
}
