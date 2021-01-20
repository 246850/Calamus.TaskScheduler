using Quartz;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Calamus.TaskScheduler.Infrastructure
{
    [PersistJobDataAfterExecution]  // 执行完，更新JobData
    [DisallowConcurrentExecution]   // 单个Job不允许并发 执行
    public class HttpJob : IJob
    {
        private readonly HttpClient _http;
        public HttpJob(IHttpClientFactory clientFactory)
        {
            _http = clientFactory.CreateClient();
        }
        public async Task Execute(IJobExecutionContext context)
        {
            string requestUrl = context.JobDetail.JobDataMap.GetString(DataKeys.RequestUrl);
            int httpMethod = context.JobDetail.JobDataMap.GetInt(DataKeys.HttpMethod);
            string result;
            if (httpMethod == (int)HttpMethodEnum.Post)
            {
                string requestBody = context.JobDetail.JobDataMap.GetString(DataKeys.RequestBody);
                HttpResponseMessage response = await _http.PostAsync(requestUrl, new StringContent(requestBody, Encoding.UTF8, "application/json"));
                result = await response.Content.ReadAsStringAsync();
            }
            else
            {
                result = await _http.GetStringAsync(requestUrl);
            }

            List<string> list = (context.JobDetail.JobDataMap[DataKeys.LogList] as List<string>) ?? new List<string>();
            while (list.Count >= 20)
            {
                list.RemoveAt(list.Count - 1);// 最大保存日志数量 20 条
            }

            string log = $"执行时间：{context.FireTimeUtc.LocalDateTime} - {DateTime.Now}，请求耗时：{context.JobRunTime.TotalMilliseconds}ms，返回结果：{CutString(result, 500)}";
            list.Insert(0, log);
            context.JobDetail.JobDataMap[DataKeys.LogList] = list;
        }

        /// <summary>
        /// 截取字符串长度
        /// </summary>
        /// <param name="source"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        string CutString(string source, int length)
        {
            if (source.Length <= length) return source;
            return source.Substring(0, length);
        }
    }
}
