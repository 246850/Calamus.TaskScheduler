using Quartz;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Calamus.TaskScheduler.Infrastructure
{
    public static class JobDataMapExtensions
    {
        public static int GetHttpMethod(this JobDataMap map)
            => map.GetInt(DataKeys.HttpMethod);
        public static string GetRequestUrl(this JobDataMap map)
            => map.GetString(DataKeys.RequestUrl);
        public static int GetTriggerType(this JobDataMap map)
            => map.GetInt(DataKeys.TriggerType);
        public static int GetRepeatCount(this JobDataMap map)
            => map.GetInt(DataKeys.RepeatCount);
        public static int GetInterval(this JobDataMap map)
            => map.GetInt(DataKeys.Interval);
        public static int GetIntervalType(this JobDataMap map)
            => map.GetInt(DataKeys.IntervalType);
        public static string GetCron(this JobDataMap map)
            => map.GetString(DataKeys.Cron) ?? string.Empty;
        public static string GetRequestBody(this JobDataMap map)
            => map.GetString(DataKeys.RequestBody) ?? string.Empty;
        public static DateTime GetCreateTime(this JobDataMap map)
            => map.GetDateTime(DataKeys.CreateTime);
        public static DateTime GetStartTime(this JobDataMap map)
            => map.GetDateTime(DataKeys.StartTime);
        public static DateTime? GetEndTime(this JobDataMap map)
            => string.IsNullOrWhiteSpace(map.GetString(DataKeys.EndTime)) ? null : map.GetDateTime(DataKeys.EndTime);
        public static string GetLastException(this JobDataMap map)
            => map.GetString(DataKeys.LastException) ?? string.Empty;
        public static List<string> GetLogList(this JobDataMap map)
            => (map[DataKeys.LogList] as List<string>) ?? new List<string>();
    }
}
