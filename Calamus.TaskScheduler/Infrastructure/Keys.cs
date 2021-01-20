using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Calamus.TaskScheduler.Infrastructure
{
    internal sealed class CacheKeys
    {
        public static readonly string AllGroupKey = "AllGroupKey";
        public static readonly string SchedulerInfoKey = "SchedulerInfoKey";
    }

    internal sealed class DataKeys
    {
        public static readonly string HttpMethod = "HttpMethod";
        public static readonly string RequestUrl = "RequestUrl";
        public static readonly string TriggerType = "TriggerType";
        public static readonly string RepeatCount = "RepeatCount";
        public static readonly string Interval = "Interval";
        public static readonly string IntervalType = "IntervalType";
        public static readonly string Cron = "Cron";
        public static readonly string RequestBody = "RequestBody";
        public static readonly string CreateTime = "CreateTime";
        public static readonly string StartTime = "StartTime";
        public static readonly string EndTime = "EndTime";

        public static readonly string LastException = "LastException";
        public static readonly string LogList = "LogList";
    }

    internal sealed class EmailJobKeys
    {
        public static readonly string NameKey = "_EmailNameKey_";
        public static readonly string GroupKey = "_EmailGroupKey_";
        public static readonly string Host = "Host";
        public static readonly string Port = "Port";
        public static readonly string UserName = "UserName";
        public static readonly string Password = "Password";
        public static readonly string To = "To";
        public static readonly string NickName = "NickName";
        public static readonly string CacheExpiry = "CacheExpiry"; 
    }

    internal sealed class NLogJobKey
    {
        public static readonly string NameKey = "_NLogNameKey_";
        public static readonly string GroupKey = "_NLogGroupKey_";
    }

    public class PropertiesKeys
    {
        public static readonly string Key_1 = "Key_1";
        public static readonly string Key_2 = "Key_2";
        public static readonly string Key_3 = "Key_3";
    }
}
