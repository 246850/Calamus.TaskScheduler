using Quartz;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Calamus.TaskScheduler.Infrastructure
{
    /// <summary>
    /// 每3天清空 NLog 产生的日志文件
    /// </summary>
    public class ClearNLogJob : IJob
    {
        public Task Execute(IJobExecutionContext context)
        {
            string path = Path.Combine(AppContext.BaseDirectory, "logs");
            if (Directory.Exists(path))
            {
                Directory.Delete(path, true);
            }

            return Task.CompletedTask;
        }
    }
}
