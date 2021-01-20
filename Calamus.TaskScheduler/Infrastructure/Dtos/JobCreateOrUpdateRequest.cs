using FluentValidation;
using Quartz;
using System;

namespace Calamus.TaskScheduler.Infrastructure.Dtos
{
    public class JobCreateOrUpdateRequest : RequestBase
    {
        /// <summary>
        /// 任务名称
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// 分组名称
        /// </summary>
        public string Group { get; set; }
        /// <summary>
        /// 请求方式
        /// </summary>
        public int HttpMethod { get; set; }
        /// <summary>
        /// 任务名称
        /// </summary>
        public string RequestUrl { get; set; }
        /// <summary>
        /// 触发类型
        /// </summary>
        public int TriggerType { get; set; }
        /// <summary>
        /// 重复次数，0：无限
        /// </summary>
        public int RepeatCount { get; set; }
        /// <summary>
        /// 间隔时间
        /// </summary>
        public int Interval { get; set; }
        /// <summary>
        /// 间隔类型
        /// </summary>
        public int IntervalType { get; set; }
        /// <summary>
        /// Cron表达式
        /// </summary>
        public string Cron { get; set; }
        /// <summary>
        /// 开始时间
        /// </summary>
        public DateTime? StartTime { get; set; }
        /// <summary>
        /// 结束时间
        /// </summary>
        public DateTime? EndTime { get; set; }
        /// <summary>
        /// 请求参数
        /// </summary>
        public string RequestBody { get; set; }
        /// <summary>
        /// 描述信息
        /// </summary>
        public string Description { get; set; }
        /// <summary>
        /// true：更新，false：新增
        /// </summary>
        public bool IsUpdate { get; set; }
    }

    public class JobCreateOrUpdateValidator : AbstractValidator<JobCreateOrUpdateRequest>
    {
        public JobCreateOrUpdateValidator()
        {
            RuleFor(model => model.Name).NotEmpty();
            RuleFor(model => model.Group).NotEmpty();
            RuleFor(model => model.HttpMethod).Must(x => HttpMethodEnum.Get.ToValueList().Contains(x));
            RuleFor(model => model.RequestUrl).Matches(@"^((http|https)://)(([a-zA-Z0-9\._-]+\.[a-zA-Z]{2,6})|([0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}))(:[0-9]{1,4})*(/[a-zA-Z0-9\&%_\./-~-]*)?$").WithMessage("请输入正确的请求地址");
            RuleFor(model => model.StartTime).NotNull();
            RuleFor(model => model.TriggerType).Must(x => TriggerTypeEnum.Simple.ToValueList().Contains(x));
            When(model => model.TriggerType == (int)TriggerTypeEnum.Simple, () =>
            {
                RuleFor(model => model.IntervalType).Must(x => IntervalTypeEnum.Second.ToValueList().Contains(x));
                RuleFor(model => model.Interval).GreaterThan(0);
            });
            When(model => model.TriggerType == (int)TriggerTypeEnum.Cron, () => RuleFor(model => model.Cron)
                                                                                .NotEmpty()
                                                                                .Must(x=> CronExpression.IsValidExpression(x)).WithMessage("不正确的Cron表达式"));
            When(model => model.EndTime.HasValue, () => RuleFor(model => model.EndTime).GreaterThan(DateTime.Now).WithMessage("结束时间必须大于当前时间"));
        }
    }
}
