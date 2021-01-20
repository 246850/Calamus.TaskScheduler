using Calamus.TaskScheduler.Infrastructure.Dtos;
using FluentEmail.Core;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Quartz;
using Quartz.Listener;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Calamus.TaskScheduler.Infrastructure
{
    public class DefaultSchedulerListener : SchedulerListenerSupport
    {
        private readonly ILogger<DefaultSchedulerListener> _logger;
        public DefaultSchedulerListener(ILogger<DefaultSchedulerListener> logger)
        {
            _logger = logger;
        }
        public override Task SchedulerError(string msg, SchedulerException cause, CancellationToken cancellationToken = default)
        {
            _logger.LogError(cause.InnerException, "Quartz.NET ISchedulerListener SchedulerError()方法日志：" + msg);    // 记录错误日志

            return base.SchedulerError(msg, cause, cancellationToken);
        }
    }

    public class DefaultJobListener : JobListenerSupport
    {
        private readonly ILogger<DefaultJobListener> _logger;
        private readonly IDistributedCache _distributedCache;
        public DefaultJobListener(
            ILogger<DefaultJobListener> logger, IDistributedCache distributedCache)
        {
            _logger = logger;
            _distributedCache = distributedCache;
        }
        public override string Name => "Calamus.JobListener";

        /// <summary>
        /// 执行被否决
        /// </summary>
        /// <param name="context"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public override Task JobExecutionVetoed(IJobExecutionContext context, CancellationToken cancellationToken = default)
        {
            _logger.LogError($"Quartz.NET IJobListener JobExecutionVetoed()方法日志：Name = {context.JobDetail.Key.Name}, Group = {context.JobDetail.Key.Group}");    // 记录错误日志
            return base.JobExecutionVetoed(context, cancellationToken);
        }

        /// <summary>
        /// 执行完毕
        /// </summary>
        /// <param name="context"></param>
        /// <param name="jobException"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public override Task JobWasExecuted(IJobExecutionContext context, JobExecutionException jobException, CancellationToken cancellationToken = default)
        {
            string cacheKey = $"{context.JobDetail.Key.Group}.{context.JobDetail.Key.Name}.Exception";  // 缓存键

            if (jobException != null && !string.IsNullOrWhiteSpace(jobException.Message))
            {
                Exception ex = jobException.InnerException;
                while (ex.InnerException != null)
                {
                    ex = ex.InnerException;
                }
                context.JobDetail.JobDataMap[DataKeys.LastException] = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "<br/>" + ex.Message + "<br/>" + ex.StackTrace.Replace("\r\n", "<br/>");  // 设置最后一次异常信息

                try
                {
                    if (string.IsNullOrWhiteSpace(_distributedCache.GetString(cacheKey)))
                    {
                        // 这两个接口 不能通过构造函数注入，无解...
                        var _scheduler = IocEngine.Instance.ServiceProvider.GetRequiredService<ISchedulerFactory>().GetScheduler().Result;
                        var _fluentEmail = IocEngine.Instance.ServiceProvider.GetRequiredService<IFluentEmail>();
                        IJobDetail emailJob = _scheduler.GetJobDetail(new JobKey(EmailJobKeys.NameKey, EmailJobKeys.GroupKey)).Result;

                        _fluentEmail.To(emailJob.JobDataMap.GetString(EmailJobKeys.To)).Subject($"任务调度异常：Name = {context.JobDetail.Key.Name}, Group = {context.JobDetail.Key.Group}")
                            .UsingTemplate(@"<p>##ErrorTime##</p>
                                        <p><strong>##Message##</strong></p>
                                        <pre>##StackTrace##</pre>", new EmailNoticeModel
                            {
                                ErrorTime = DateTime.Now,
                                Message = ex.Message,
                                StackTrace = ex.StackTrace
                            }, true)
                            .Send();
                        // 通过设置缓存，控制每多少分钟内只发一封通知邮件
                        _distributedCache.SetString(cacheKey, cacheKey, new DistributedCacheEntryOptions { SlidingExpiration = TimeSpan.FromMinutes(emailJob.JobDataMap.GetInt(EmailJobKeys.CacheExpiry) <= 0 ? 30 : emailJob.JobDataMap.GetInt(EmailJobKeys.CacheExpiry)) });
                    }
                }
                catch (Exception ex1)
                {
                    _logger.LogError($"发送邮件通知失败：Name = {context.JobDetail.Key.Name}, Group = {context.JobDetail.Key.Group}，Message = {ex1.Message}");
                }
            }
            else
            {
                _distributedCache.Remove(cacheKey);
            }
            return base.JobWasExecuted(context, jobException, cancellationToken);
        }
    }

    public class DefaultTriggerListener : TriggerListenerSupport
    {
        private readonly ILogger<DefaultTriggerListener> _logger;
        public DefaultTriggerListener(ILogger<DefaultTriggerListener> logger)
        {
            _logger = logger;
        }
        public override string Name => "Calamus.TriggerListener";

        /// <summary>
        /// 错过触发，可能原因是 任务逻辑处理时间太长，应保持job业务逻辑尽量过短
        /// </summary>
        /// <param name="trigger"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public override Task TriggerMisfired(ITrigger trigger, CancellationToken cancellationToken = default)
        {
            _logger.LogError($"Quartz.NET ITriggerListener TriggerMisfired()方法日志：Name = {trigger.JobKey.Name}，Group = {trigger.JobKey.Group}");    // 记录错误日志
            return base.TriggerMisfired(trigger, cancellationToken);
        }
    }
}
