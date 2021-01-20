using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Calamus.TaskScheduler.Infrastructure
{
    public class CodeResult
    {
        public int Code { get; set; }
        public string Msg { get; set; }
        public static CodeResult Success
        {
            get
            {
                return new CodeResult
                {
                    Code = 0,
                    Msg = "success"
                };
            }
        }
        public static CodeResult Failed
        {
            get
            {
                return new CodeResult
                {
                    Code = -1,
                    Msg = "failed"
                };
            }
        }
    }

    public class CodeResult<T> : CodeResult
    {
        public T Data { get; set; }
    }
}
