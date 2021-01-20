using Calamus.TaskScheduler.Infrastructure;
using Calamus.TaskScheduler.Infrastructure.Dtos;
using Calamus.TaskScheduler.Infrastructure.Filters;
using FluentEmail.Core;
using FluentEmail.Core.Defaults;
using FluentEmail.Smtp;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Quartz;
using System;
using System.Net;
using System.Net.Mail;
using System.Text.Encodings.Web;
using System.Text.Unicode;

namespace Calamus.TaskScheduler
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllersWithViews(options =>
                {
                    options.Filters.Add<GatewayResultFilterAttribute>();    // 通用执行结果包装处理过滤器
                    options.Filters.Add<GlobalExceptionFilterAttribute>();  // 全局异常过滤器
                })
                .AddJsonOptions(options =>
                {
                    options.JsonSerializerOptions.Converters.Add(new DateTimeConverter());  // 日期格式化
                })
                .AddFluentValidation(config =>  // 请求模型参数验证
                {
                    config.RunDefaultMvcValidationAfterFluentValidationExecutes = true;    // false : 禁止默认模型验证
                    config.ValidatorOptions.CascadeMode = CascadeMode.Stop; // 不级联验证，第一个规则错误就停止
                    config.RegisterValidatorsFromAssemblyContaining<JobCreateOrUpdateValidator>();
                });
            services.AddHostedService<NLogHostService>();   // NLog 关闭服务
            services.AddDistributedMemoryCache();  // 分布式缓存接口
            services.AddSingleton(HtmlEncoder.Create(UnicodeRanges.All));// 解决中文乱码
            services.AddHttpClient();   // IHttpClientFactory

            IConfigurationSection quartzConfiguration = Configuration.GetSection("Quartz"); // Quartz配置节点

            /***********Quartz.NET*********/
            services.AddTransient<HttpJob>();   // 注册job至容器，必须步骤
            services.AddQuartz(config =>
            {
                config.UseTimeZoneConverter();
                // 使用MicrosoftDependencyInjectionJobFactory工厂类从 容器 中创建job实例
                config.UseMicrosoftDependencyInjectionJobFactory(options =>
                {
                    options.AllowDefaultConstructor = false;    // 禁止使用无参构建函数创建 job
                    options.CreateScope = false;
                });
                config.UseDefaultThreadPool(options =>
                {
                    options.MaxConcurrency = 10;    // 最大并发执行线程数
                });
                config.UsePersistentStore(options =>
                {
                    options.UseProperties = false;
                    //options.UseBinarySerializer();  // 二进制序列化
                    options.UseJsonSerializer();    // json序列化
                    options.UseMySql(ado =>
                    {
                        ado.ConnectionString = quartzConfiguration["Database"];
                        ado.TablePrefix = quartzConfiguration["TablePrefix"];  // 默认值 QRTZ_
                        ado.ConnectionStringName = "Quartz.net";
                    });
                });

                // 监听器
                config.AddSchedulerListener<DefaultSchedulerListener>();
                config.AddJobListener<DefaultJobListener>();
                config.AddTriggerListener<DefaultTriggerListener>();

                // 启动NLog日志文件清除job
                config.ScheduleJob<ClearNLogJob>(trigger =>
                {
                    trigger.WithIdentity(NLogJobKey.NameKey, NLogJobKey.GroupKey).StartNow()
                    .WithCronSchedule("0 0 0 1/3 * ? ", cron => cron.WithMisfireHandlingInstructionFireAndProceed());    // 从每月1日开始，每3天执行一次
                }, job =>
                {
                    job.WithIdentity(NLogJobKey.NameKey, NLogJobKey.GroupKey)
                    .StoreDurably(false)     // 是否持久化， 无关联触发器时是否移除，false：移除
                    .RequestRecovery()  // 重启后是否恢复任务
                    .WithDescription("每3天清空NLog日志文件");
                });
            });
            // IHostedService宿主启动 Quartz服务 services.AddSingleton<IHostedService, QuartzHostedService>()
            services.AddQuartzServer(options =>
            {
                // when shutting down we want jobs to complete gracefully
                options.WaitForJobsToComplete = true;   // 等待任务执行完，再退出
            });

            /***********FluentEmail*********/
            // 为了将邮件通知配置在job data上， 不使用自带的service注册方式
            //services.AddFluentEmail(quartzConfiguration["Smtp:UserName"], "Quartz.NET任务调度通知")
            //    .AddRazorRenderer()
            //    .AddSmtpSender(quartzConfiguration["Smtp:Host"], Convert.ToInt32(quartzConfiguration["Smtp:Port"]), quartzConfiguration["Smtp:UserName"], quartzConfiguration["Smtp:Password"]);
            services.AddTransient<IFluentEmail>(serviceProvider =>
            {
                IScheduler scheduler = serviceProvider.GetRequiredService<ISchedulerFactory>().GetScheduler().Result;

                JobKey key = new JobKey(EmailJobKeys.NameKey, EmailJobKeys.GroupKey);
                if (!scheduler.CheckExists(key).Result)
                {
                    JobDataMap dataMap = new JobDataMap();
                    dataMap.Put(EmailJobKeys.Host, "smtp.qq.com");
                    dataMap.Put(EmailJobKeys.Port, 587);    // 465端口一直尝试不通过，奇怪
                    dataMap.Put(EmailJobKeys.UserName, "390915549@qq.com"); // 作者qq，欢迎骚扰
                    dataMap.Put(EmailJobKeys.Password, "cirxjtemuzxycagf");
                    dataMap.Put(EmailJobKeys.To, string.Empty); // 接收者邮件支持多个，以 ; 隔开
                    dataMap.Put(EmailJobKeys.NickName, "Quartz.NET任务调度通知");
                    dataMap.Put(EmailJobKeys.CacheExpiry, 30);  // 默认30分钟内只通知一次
                    IJobDetail job = JobBuilder.Create<HttpJob>()
                        .StoreDurably(true)
                        .RequestRecovery()
                        .WithDescription("邮件通知配置Job，切勿删除")
                        .WithIdentity(key)
                        .UsingJobData(dataMap)
                        .Build();
                    scheduler.AddJob(job, true);   // 初始化邮件通知配置
                }

                IJobDetail emailJob = scheduler.GetJobDetail(key).Result;
                IFluentEmail fluentEmail = new Email(new ReplaceRenderer(),
                    new SmtpSender(new SmtpClient(emailJob.JobDataMap.GetString(EmailJobKeys.Host), emailJob.JobDataMap.GetInt(EmailJobKeys.Port))
                    {
                        EnableSsl = true,
                        Credentials = new NetworkCredential(emailJob.JobDataMap.GetString(EmailJobKeys.UserName),
                        emailJob.JobDataMap.GetString(EmailJobKeys.Password))
                    }),
                    emailJob.JobDataMap.GetString(EmailJobKeys.UserName),
                    emailJob.JobDataMap.GetString(EmailJobKeys.NickName));
                return fluentEmail;
            });

            IocEngine.Instance.Init(services);  // 实在没办法才弄个静态容器获取service， 监听器里无法通过构造函数 注入 ISchedulerFactory, IFluentEmail, 猜测应该是循环引用了
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }
            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}/{id?}");
            });
        }
    }
}
