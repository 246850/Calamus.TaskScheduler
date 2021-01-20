using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Calamus.TaskScheduler.Infrastructure.Dtos
{
    public class EmailConfigModel
    {
        public string Host { get; set; }
        public int Port { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }
        public string To { get; set; }
        public string NickName { get; set; }
        /// <summary>
        /// 单位：分，通过缓存控制，多少分内只通知一次
        /// </summary>
        public int CacheExpiry { get; set; }
    }

    public class EmailNoticeCreateValidator : AbstractValidator<EmailConfigModel>
    {
        public EmailNoticeCreateValidator()
        {
            RuleFor(model => model.Host).NotEmpty();
            RuleFor(model => model.Port).GreaterThan(0);
            RuleFor(model => model.UserName).NotEmpty();
            RuleFor(model => model.Password).NotEmpty();
            RuleFor(model => model.To).NotEmpty();
            RuleFor(model => model.NickName).NotEmpty();
            RuleFor(model => model.CacheExpiry).GreaterThan(0);
        }
    }
}
