using Calamus.TaskScheduler.Infrastructure;
using Calamus.TaskScheduler.Infrastructure.Dtos;
using Calamus.TaskScheduler.Infrastructure.Filters;
using FluentEmail.Core;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Quartz;
using Quartz.Impl;
using Quartz.Impl.Matchers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Calamus.TaskScheduler.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IScheduler _scheduler;
        private readonly IDistributedCache _distributedCache;
        private readonly QuartzOptions _quartzOptions;
        private readonly IFluentEmail _fluentEmail;
        public HomeController(ILogger<HomeController> logger,
            ISchedulerFactory schedulerFactory,
            IDistributedCache distributedCache,
            IOptions<QuartzOptions> quartzOptions,
            IFluentEmail fluentEmail)
        {
            _logger = logger;
            _scheduler = schedulerFactory.GetScheduler().Result;
            _distributedCache = distributedCache;
            _quartzOptions = quartzOptions.Value;
            _fluentEmail = fluentEmail;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            JobSearchRequest model = new JobSearchRequest();

            List<SelectListItem> selects = await _distributedCache.GetOrAddAsync(CacheKeys.AllGroupKey, async () =>
            {
                IReadOnlyCollection<string> groups = await _scheduler.GetJobGroupNames();
                List<SelectListItem> items = groups.Where(x => !x.Equals(EmailJobKeys.GroupKey, StringComparison.CurrentCultureIgnoreCase) && !x.Equals(NLogJobKey.GroupKey, StringComparison.CurrentCultureIgnoreCase))    // 排除email,nlog分组
                        .Select(x => new SelectListItem { Text = x, Value = x })
                        .ToList();
                items.Insert(0, new SelectListItem { Text = "全部", Value = string.Empty });
                return items;
            }, 3600);   // 缓存1小时

            model.Properties.Add(PropertiesKeys.Key_1, selects);

            return View(model);
        }

        [HttpPost]
        public async Task<IEnumerable<JobResponse>> Jobs(JobSearchRequest request)
        {
            GroupMatcher<JobKey> matcher = GroupMatcher<JobKey>.AnyGroup();
            if (!string.IsNullOrWhiteSpace(request.Group))
            {
                matcher = GroupMatcher<JobKey>.GroupEquals(request.Group.Trim());
            }

            IReadOnlyCollection<JobKey> jobKeys = await _scheduler.GetJobKeys(matcher); // 根据分组查询条件 获取所有JobKey
            List<JobResponse> items = new List<JobResponse>();

            foreach (JobKey key in jobKeys)
            {
                // 过滤掉邮件通知配置job，NLog日志文件清除Job
                if (string.Equals(key.Name, EmailJobKeys.NameKey, StringComparison.InvariantCultureIgnoreCase) || string.Equals(key.Name, NLogJobKey.NameKey, StringComparison.InvariantCultureIgnoreCase)) continue; 

                IJobDetail job = await _scheduler.GetJobDetail(key);
                if (job == null) continue;

                JobResponse item = new JobResponse
                {
                    Name = job.Key.Name,
                    Group = job.Key.Group,
                    TriggerState = TriggerState.Complete,
                    HttpMethod = job.JobDataMap.GetInt(DataKeys.HttpMethod),
                    RequestUrl = job.JobDataMap.GetString(DataKeys.RequestUrl),
                    TriggerType = job.JobDataMap.GetInt(DataKeys.TriggerType),
                    Interval = job.JobDataMap.GetInt(DataKeys.Interval),
                    IntervalType = job.JobDataMap.GetInt(DataKeys.IntervalType),
                    RepeatCount = job.JobDataMap.GetInt(DataKeys.RepeatCount),
                    Cron = job.JobDataMap.GetString(DataKeys.Cron),
                    RequestBody = job.JobDataMap.GetString(DataKeys.RequestBody),
                    Description = job.Description,
                    CreateTime = job.JobDataMap.GetDateTime(DataKeys.CreateTime),
                    StartTime = job.JobDataMap.GetDateTime(DataKeys.StartTime),
                    EndTime = string.IsNullOrWhiteSpace(job.JobDataMap.GetString(DataKeys.EndTime)) ? null : job.JobDataMap.GetDateTime(DataKeys.EndTime),
                    LastException = job.JobDataMap.GetString(DataKeys.LastException)
                };

                IReadOnlyCollection<ITrigger> triggers = await _scheduler.GetTriggersOfJob(key);
                ITrigger trigger = triggers.FirstOrDefault();   // 获取当前job关联的第一个 trigger
                if (trigger != null)
                {
                    TriggerState triggerState = await _scheduler.GetTriggerState(trigger.Key);  // trigger 状态

                    /****计算时间差***/
                    DateTime? prevFire = trigger.GetPreviousFireTimeUtc()?.LocalDateTime;
                    DateTime? nextFire = trigger.GetNextFireTimeUtc()?.LocalDateTime;
                    TimeSpan span = TimeSpan.FromSeconds(0);
                    if (prevFire.HasValue && nextFire.HasValue)
                    {
                        span = (nextFire.Value - prevFire.Value);
                    }
                    item.TriggerState = triggerState;
                    item.FirePlan = $"{span.Days}天{span.Hours}小时{span.Minutes}分{span.Seconds}秒";    // 执行频率
                    item.PrevFireTime = prevFire;
                    item.NextFireTime = nextFire;
                };

                items.Add(item);
            }

            return items.WhereIf(!string.IsNullOrWhiteSpace(request.Name), x => x.Name.Contains(request.Name.Trim())).OrderByDescending(x => x.CreateTime);
        }

        [HttpGet]
        public async Task<IActionResult> CreateOrUpdate(string group = "", string name = "")
        {
            JobCreateOrUpdateRequest model = new JobCreateOrUpdateRequest
            {
                Group = group,
                HttpMethod = (int)HttpMethodEnum.Get,
                TriggerType = (int)TriggerTypeEnum.Simple,
                Interval = 60,
                IsUpdate = false
            };
            if (!string.IsNullOrWhiteSpace(group) && !string.IsNullOrWhiteSpace(name))
            {
                /**********不为空，说明是更新任务**********/
                JobKey key = new JobKey(name.Trim(), group.Trim());
                IJobDetail job = await _scheduler.GetJobDetail(key);

                model = new JobCreateOrUpdateRequest
                {
                    Name = job.Key.Name,
                    Group = job.Key.Group,
                    HttpMethod = job.JobDataMap.GetInt(DataKeys.HttpMethod),
                    RequestUrl = job.JobDataMap.GetString(DataKeys.RequestUrl),
                    StartTime = job.JobDataMap.GetDateTime(DataKeys.StartTime),
                    EndTime = string.IsNullOrWhiteSpace(job.JobDataMap.GetString(DataKeys.EndTime)) ? null : job.JobDataMap.GetDateTime(DataKeys.EndTime),
                    TriggerType = job.JobDataMap.GetInt(DataKeys.TriggerType),
                    Interval = job.JobDataMap.GetInt(DataKeys.Interval),
                    IntervalType = job.JobDataMap.GetInt(DataKeys.IntervalType),
                    RepeatCount = job.JobDataMap.GetInt(DataKeys.RepeatCount),
                    Cron = job.JobDataMap.GetString(DataKeys.Cron),
                    RequestBody = job.JobDataMap.GetString(DataKeys.RequestBody),
                    Description = job.Description,
                    IsUpdate = true
                };
            }

            model.Properties.Add(PropertiesKeys.Key_1, HttpMethodEnum.Get.ToSelectList());
            model.Properties.Add(PropertiesKeys.Key_2, TriggerTypeEnum.Simple.ToSelectList());
            model.Properties.Add(PropertiesKeys.Key_3, IntervalTypeEnum.Second.ToSelectList());

            return View(model);
        }
        [ModelValidatorFilter]
        [HttpPost]
        public async Task CreateOrUpdate(JobCreateOrUpdateRequest request)
        {
            request.Name = request.Name.Trim();
            request.Group = request.Group.Trim();

            JobKey key = new JobKey(request.Name, request.Group);
            if (await _scheduler.CheckExists(key))
            {
                if (!request.IsUpdate) throw new Exception("已存在相同名称的任务"); // 新增时，存在相同任务，不创建
                else
                {
                    await _scheduler.DeleteJob(key);    // 更新时，先删除，再创建
                }
            };

            /******Data*****/
            JobDataMap dataMap = new JobDataMap();
            dataMap.Put(DataKeys.HttpMethod, request.HttpMethod);
            dataMap.Put(DataKeys.RequestUrl, request.RequestUrl);
            dataMap.Put(DataKeys.TriggerType, request.TriggerType);
            dataMap.Put(DataKeys.RepeatCount, request.RepeatCount);
            dataMap.Put(DataKeys.Interval, request.Interval);
            dataMap.Put(DataKeys.IntervalType, request.IntervalType);
            dataMap.Put(DataKeys.Cron, request.Cron);
            dataMap.Put(DataKeys.RequestBody, request.RequestBody);
            dataMap.Put(DataKeys.CreateTime, DateTime.Now.ToString());
            dataMap.Put(DataKeys.StartTime, request.StartTime.ToString());
            dataMap.Put(DataKeys.EndTime, request.EndTime.HasValue ? request.EndTime.Value.ToString() : string.Empty);

            /******Job*****/
            IJobDetail job = JobBuilder.Create<HttpJob>()
                .StoreDurably(true)     // 是否持久化， 无关联触发器时是否移除，false：移除
                .RequestRecovery()  // 重启后是否恢复任务
                .WithDescription(request.Description ?? string.Empty)
                .WithIdentity(request.Name, request.Group)
                .UsingJobData(dataMap)
                .Build();

            /******Trigger*****/
            TriggerBuilder builder = TriggerBuilder.Create()
                .WithIdentity(request.Name, request.Group)
                .StartAt(request.StartTime.Value)
                .ForJob(job);
            if (request.EndTime.HasValue)
            {
                builder.EndAt(request.EndTime.Value);
            }
            if (request.TriggerType == (int)TriggerTypeEnum.Simple)
            {
                builder.WithSimpleSchedule(simple =>
                {
                    if (request.IntervalType == (int)IntervalTypeEnum.Second)
                    {
                        simple.WithIntervalInSeconds(request.Interval);
                    }
                    if (request.IntervalType == (int)IntervalTypeEnum.Minute)
                    {
                        simple.WithIntervalInMinutes(request.Interval);
                    }
                    if (request.IntervalType == (int)IntervalTypeEnum.Hour)
                    {
                        simple.WithIntervalInHours(request.Interval);
                    }
                    if (request.IntervalType == (int)IntervalTypeEnum.Day)
                    {
                        simple.WithIntervalInHours(request.Interval * 24);
                    }

                    if (request.RepeatCount > 0)
                    {
                        simple.WithRepeatCount(request.RepeatCount);
                    }
                    else
                    {
                        simple.RepeatForever();
                    }
                    simple.WithMisfireHandlingInstructionFireNow(); // 如果延迟执行了
                });
            }
            else
            {
                builder.WithCronSchedule(request.Cron, cron =>
                {
                    cron.WithMisfireHandlingInstructionFireAndProceed();
                });
            }

            ITrigger trigger = builder.Build();

            await _scheduler.ScheduleJob(job, trigger); // 加入调度，并持久化
            FlushCache();
        }

        [HttpPost]
        public async Task Delete(string name, string group)
        {
            await _scheduler.DeleteJob(new JobKey(name, group));
            FlushCache();
        }
        [HttpPost]
        public async Task Pause(string name, string group)
        {
            await _scheduler.PauseJob(new JobKey(name, group));
        }
        [HttpPost]
        public async Task Resume(string name, string group)
        {
            await _scheduler.ResumeJob(new JobKey(name, group));
        }
        [HttpPost]
        public async Task Trigger(string name, string group)
        {
            await _scheduler.TriggerJob(new JobKey(name, group));
        }

        [HttpGet]
        public async Task<IActionResult> Log(string name, string group)
        {
            JobKey key = new JobKey(name.Trim(), group.Trim());
            IJobDetail job = await _scheduler.GetJobDetail(key);
            List<string> models = (job.JobDataMap[DataKeys.LogList] as List<string>) ?? new List<string>();
            return View(models);
        }

        [HttpGet]
        public IActionResult Clear()
        {
            _scheduler.Clear();
            return Ok("清空完成");
        }

        [HttpGet]
        public async Task<IActionResult> Info()
        {
            List<PropertyConfigurationModel> items = await _distributedCache.GetOrAddAsync(CacheKeys.SchedulerInfoKey, () => Task.FromResult(new List<PropertyConfigurationModel>
            {
                new PropertyConfigurationModel
                {
                    Name = StdSchedulerFactory.PropertySchedulerInstanceId,
                    Value = _scheduler.SchedulerInstanceId
                },
                new PropertyConfigurationModel
                {
                    Name = StdSchedulerFactory.PropertySchedulerInstanceName,
                    Value = _scheduler.SchedulerName
                },
                new PropertyConfigurationModel
                {
                    Name = "quartz.plugin.timeZoneConverter.type",
                    Value = _quartzOptions["quartz.plugin.timeZoneConverter.type"]
                },
                new PropertyConfigurationModel
                {
                    Name = StdSchedulerFactory.PropertySchedulerJobFactoryType,
                    Value = _quartzOptions[StdSchedulerFactory.PropertySchedulerJobFactoryType]
                },
                new PropertyConfigurationModel
                {
                    Name = StdSchedulerFactory.PropertyThreadPoolType,
                    Value = _quartzOptions[StdSchedulerFactory.PropertyThreadPoolType]
                },
                new PropertyConfigurationModel
                {
                    Name = "quartz.threadPool.maxConcurrency",
                    Value = _quartzOptions["quartz.threadPool.maxConcurrency"]
                },
                new PropertyConfigurationModel
                {
                    Name = StdSchedulerFactory.PropertyJobStoreType,
                    Value = _quartzOptions[StdSchedulerFactory.PropertyJobStoreType]
                },
                new PropertyConfigurationModel
                {
                    Name = "quartz.serializer.type",
                    Value = _quartzOptions["quartz.serializer.type"]
                },
                new PropertyConfigurationModel
                {
                    Name = "quartz.jobStore.driverDelegateType",
                    Value = _quartzOptions["quartz.jobStore.driverDelegateType"]
                },
                new PropertyConfigurationModel
                {
                    Name = "quartz.dataSource.default.provider",
                    Value = _quartzOptions["quartz.dataSource.default.provider"]
                },
                new PropertyConfigurationModel
                {
                    Name = "quartz.jobStore.tablePrefix",
                    Value = _quartzOptions["quartz.jobStore.tablePrefix"]
                },
                new PropertyConfigurationModel
                {
                    Name = "quartz.jobStore.clustered",
                    Value = _quartzOptions["quartz.jobStore.clustered"]
                },
                new PropertyConfigurationModel
                {
                    Name = "quartz.jobStore.misfireThreshold",
                    Value = _quartzOptions["quartz.jobStore.misfireThreshold"]
                }
            }), 3600);
            return View(items);
        }

        /// <summary>
        /// 邮件通知配置
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public async Task<IActionResult> Notice()
        {
            EmailConfigModel model = new EmailConfigModel();
            IJobDetail job = await _scheduler.GetJobDetail(new JobKey(EmailJobKeys.NameKey, EmailJobKeys.GroupKey));
            if (job != null)
            {
                model.Host = job.JobDataMap.GetString(EmailJobKeys.Host);
                model.Port = job.JobDataMap.GetInt(EmailJobKeys.Port);
                model.UserName = job.JobDataMap.GetString(EmailJobKeys.UserName);
                model.Password = job.JobDataMap.GetString(EmailJobKeys.Password);
                model.To = job.JobDataMap.GetString(EmailJobKeys.To);
                model.NickName = job.JobDataMap.GetString(EmailJobKeys.NickName);
                model.CacheExpiry = job.JobDataMap.GetInt(EmailJobKeys.CacheExpiry);
            }
            return View(model);
        }

        [HttpPost]
        [ModelValidatorFilter]
        public async Task Notice(EmailConfigModel request)
        {
            JobKey key = new JobKey(EmailJobKeys.NameKey, EmailJobKeys.GroupKey);
            JobDataMap dataMap = new JobDataMap();
            dataMap.Put(EmailJobKeys.Host, request.Host);
            dataMap.Put(EmailJobKeys.Port, request.Port);
            dataMap.Put(EmailJobKeys.UserName, request.UserName);
            dataMap.Put(EmailJobKeys.Password, request.Password);
            dataMap.Put(EmailJobKeys.To, request.To);
            dataMap.Put(EmailJobKeys.NickName, request.NickName);
            dataMap.Put(EmailJobKeys.CacheExpiry, request.CacheExpiry);
            IJobDetail job = JobBuilder.Create<HttpJob>()
                .StoreDurably(true)
                .RequestRecovery()
                .WithDescription("邮件通知配置Job，切勿删除")
                .WithIdentity(key)
                .UsingJobData(dataMap)
                .Build();
            await _scheduler.AddJob(job, true);   // 更新邮件通知配置
        }

        [HttpPost]
        public async Task Email(string to, string subject, string body)
        {
            await _fluentEmail.To(to).Subject(subject).Body(body, true).SendAsync();
        }

        /// <summary>
        /// 清除缓存，由于使用的是IMemoryCache本机缓存，集群部署模式下 会出现缓存不一致问题，只影响后台数据展示，不影响Quartz服务调度。如果特别在意，可使用分布式缓存服务，如redis。
        /// </summary>
        void FlushCache()
        {
            _distributedCache.Remove(CacheKeys.AllGroupKey);
            _distributedCache.Remove(CacheKeys.SchedulerInfoKey);
        }
    }
}
