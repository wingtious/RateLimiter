using Microsoft.Extensions.DependencyInjection;
using RateLimiter;
using RateLimiterCore.RateLimiter;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RateLimiterCore.Test
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var serviceScope = Register();
            var service = serviceScope.ServiceProvider.GetService<Test>();
            service.SimpleUsage();
            Console.ReadLine();
        }

        public static IServiceScope Register()
        {
            var services = new ServiceCollection();
            services.AddRateLimiter(TimeSpan.FromSeconds(1), 50, "10.45.11.168:6001,password=goatest@!$%");
            services.AddScoped<Test>();
            ServiceProviderExtensions.SetServiceProvider(services);
            return ServiceProviderExtensions.CreateScope();
        }
    }

    public class Test
    {
        private List<string> data = new List<string>();
        private readonly TimeLimiter _timeLimiter;
        public Test(TimeLimiter timeLimiter)
        {
            _timeLimiter = timeLimiter;
        }

        private void ConsoleIt(int i)
        {
            var time = DateTime.Now;
            data.Add(time.ToString("yyyyddMMHHmmss"));
            Console.WriteLine($"{i}:{time:MM/dd/yyy HH:mm:ss.fff}");
        }

        public async Task SimpleUsage()
        {
            for (int i = 0; i < 500; i++)
            {
                await _timeLimiter.Enqueue<RedisRateLimiter>(() => ConsoleIt(i), $"_timeLimiter_{i % 2}");
            }

            var l = data.GroupBy(x => x);
            foreach (var item in l)
            {
                Console.WriteLine($"{item.Key}:{item.ToList().Count}");
            }
        }
    }
}
