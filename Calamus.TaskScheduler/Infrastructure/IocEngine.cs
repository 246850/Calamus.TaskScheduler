using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Calamus.TaskScheduler.Infrastructure
{
    public sealed class IocEngine
    {
        public static readonly IocEngine Instance;
        static IocEngine()
        {
            Instance = new IocEngine();
        }
        public IServiceProvider ServiceProvider { get; private set; }

        public void Init(IServiceCollection services)
        {
            ServiceProvider = services.BuildServiceProvider();
        }

    }
}
