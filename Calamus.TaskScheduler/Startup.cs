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
                    options.Filters.Add<GatewayResultFilterAttribute>();    // ͨ��ִ�н����װ���������
                    options.Filters.Add<GlobalExceptionFilterAttribute>();  // ȫ���쳣������
                })
                .AddJsonOptions(options =>
                {
                    options.JsonSerializerOptions.Converters.Add(new DateTimeConverter());  // ���ڸ�ʽ��
                })
                .AddFluentValidation(config =>  // ����ģ�Ͳ�����֤
                {
                    config.RunDefaultMvcValidationAfterFluentValidationExecutes = true;    // false : ��ֹĬ��ģ����֤
                    config.ValidatorOptions.CascadeMode = CascadeMode.Stop; // ��������֤����һ����������ֹͣ
                    config.RegisterValidatorsFromAssemblyContaining<JobCreateOrUpdateValidator>();
                });
            services.AddHostedService<NLogHostService>();   // NLog �رշ���
            services.AddDistributedMemoryCache();  // �ֲ�ʽ����ӿ�
            services.AddSingleton(HtmlEncoder.Create(UnicodeRanges.All));// �����������
            services.AddHttpClient();   // IHttpClientFactory

            IConfigurationSection quartzConfiguration = Configuration.GetSection("Quartz"); // Quartz���ýڵ�

            /***********Quartz.NET*********/
            services.AddTransient<HttpJob>();   // ע��job�����������벽��
            services.AddQuartz(config =>
            {
                config.UseTimeZoneConverter();
                // ʹ��MicrosoftDependencyInjectionJobFactory������� ���� �д���jobʵ��
                config.UseMicrosoftDependencyInjectionJobFactory(options =>
                {
                    options.AllowDefaultConstructor = false;    // ��ֹʹ���޲ι����������� job
                    options.CreateScope = false;
                });
                config.UseDefaultThreadPool(options =>
                {
                    options.MaxConcurrency = 10;    // ��󲢷�ִ���߳���
                });
                config.UsePersistentStore(options =>
                {
                    options.UseProperties = false;
                    //options.UseBinarySerializer();  // ���������л�
                    options.UseJsonSerializer();    // json���л�
                    options.UseMySql(ado =>
                    {
                        ado.ConnectionString = quartzConfiguration["Database"];
                        ado.TablePrefix = quartzConfiguration["TablePrefix"];  // Ĭ��ֵ QRTZ_
                        ado.ConnectionStringName = "Quartz.net";
                    });
                });

                // ������
                config.AddSchedulerListener<DefaultSchedulerListener>();
                config.AddJobListener<DefaultJobListener>();
                config.AddTriggerListener<DefaultTriggerListener>();

                // ����NLog��־�ļ����job
                config.ScheduleJob<ClearNLogJob>(trigger =>
                {
                    trigger.WithIdentity(NLogJobKey.NameKey, NLogJobKey.GroupKey).StartNow()
                    .WithCronSchedule("0 0 0 1/3 * ? ", cron => cron.WithMisfireHandlingInstructionFireAndProceed());    // ��ÿ��1�տ�ʼ��ÿ3��ִ��һ��
                }, job =>
                {
                    job.WithIdentity(NLogJobKey.NameKey, NLogJobKey.GroupKey)
                    .StoreDurably(false)     // �Ƿ�־û��� �޹���������ʱ�Ƿ��Ƴ���false���Ƴ�
                    .RequestRecovery()  // �������Ƿ�ָ�����
                    .WithDescription("ÿ3�����NLog��־�ļ�");
                });
            });
            // IHostedService�������� Quartz���� services.AddSingleton<IHostedService, QuartzHostedService>()
            services.AddQuartzServer(options =>
            {
                // when shutting down we want jobs to complete gracefully
                options.WaitForJobsToComplete = true;   // �ȴ�����ִ���꣬���˳�
            });

            /***********FluentEmail*********/
            // Ϊ�˽��ʼ�֪ͨ������job data�ϣ� ��ʹ���Դ���serviceע�᷽ʽ
            //services.AddFluentEmail(quartzConfiguration["Smtp:UserName"], "Quartz.NET�������֪ͨ")
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
                    dataMap.Put(EmailJobKeys.Port, 587);    // 465�˿�һֱ���Բ�ͨ�������
                    dataMap.Put(EmailJobKeys.UserName, "390915549@qq.com"); // ����qq����ӭɧ��
                    dataMap.Put(EmailJobKeys.Password, "cirxjtemuzxycagf");
                    dataMap.Put(EmailJobKeys.To, string.Empty); // �������ʼ�֧�ֶ������ ; ����
                    dataMap.Put(EmailJobKeys.NickName, "Quartz.NET�������֪ͨ");
                    dataMap.Put(EmailJobKeys.CacheExpiry, 30);  // Ĭ��30������ֻ֪ͨһ��
                    IJobDetail job = JobBuilder.Create<HttpJob>()
                        .StoreDurably(true)
                        .RequestRecovery()
                        .WithDescription("�ʼ�֪ͨ����Job������ɾ��")
                        .WithIdentity(key)
                        .UsingJobData(dataMap)
                        .Build();
                    scheduler.AddJob(job, true);   // ��ʼ���ʼ�֪ͨ����
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

            IocEngine.Instance.Init(services);  // ʵ��û�취��Ū����̬������ȡservice�� ���������޷�ͨ�����캯�� ע�� ISchedulerFactory, IFluentEmail, �²�Ӧ����ѭ��������
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
