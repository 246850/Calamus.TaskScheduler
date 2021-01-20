using CronExpressionDescriptor;
using Quartz;
using System;

namespace Calamus.TaskScheduler.Infrastructure.Dtos
{
    public class JobResponse
    {
        public string Name { get; set; }
        public string Group { get; set; }
        public TriggerState TriggerState { get; set; }
        /// <summary>
        /// 请求方式
        /// </summary>
        public int HttpMethod { get; set; }
        public string HttpMethodName
        {
            get
            {
                return ((HttpMethodEnum)HttpMethod).ToText();
            }
        }
        public string RequestUrl { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public DateTime? PrevFireTime { get; set; }
        public DateTime? NextFireTime { get; set; }
        /// <summary>
        /// 执行计划/频率
        /// </summary>
        public string FirePlan { get; set; }
        /// <summary>
        /// 触发类型
        /// </summary>
        public int TriggerType { get; set; }
        public string TriggerTypeName
        {
            get
            {
                return ((TriggerTypeEnum)TriggerType).ToText();
            }
        }
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
        public string IntervalTypeName
        {
            get
            {
                return ((IntervalTypeEnum)IntervalType).ToText();
            }
        }
        /// <summary>
        /// Cron表达式
        /// </summary>
        public string Cron { get; set; }
        /// <summary>
        /// Cron表达式 说明
        /// </summary>
        public string CronDesc
        {
            get
            {
                if (TriggerType == (int)TriggerTypeEnum.Cron && !string.IsNullOrWhiteSpace(Cron))
                {
                    try
                    {
                        return ExpressionDescriptor.GetDescription(Cron, new Options()
                        {
                            Locale = "zh-Hans"
                        });
                    }
                    catch
                    {
                        // ignore
                    }
                }
                return string.Empty;
            }
        }
        public string RequestBody { get; set; }
        public string Description { get; set; }
        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreateTime { get; set; }
        /// <summary>
        /// 最后一次异常信息
        /// </summary>
        public string LastException { get; set; }
    }
}
