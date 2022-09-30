using Microsoft.Extensions.DependencyInjection;
using RateLimiter;
using RateLimiterCore.Common;
using RateLimiterCore.RateLimiter;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RateLimiterCore
{
    /// <summary>
    /// .
    /// </summary>
    public static class RateLimiterDependencyInjection
    {
        /// <summary>
        /// Adds the rate limiter.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="service">The service.</param>
        /// <param name="timeSpan">The time span.</param>
        /// <param name="count">The count.</param>
        /// <param name="redisConnectString">The redis connect string.</param>
        public static void AddRateLimiter<T>(this IServiceCollection service, TimeSpan timeSpan, int count = 50,string redisConnectString="") where T : class, IRateLimiter
        {
            if (count <= 0)
            {
                throw new ArgumentException("count should be strictly positive", nameof(count));
            }

            if (timeSpan.TotalMilliseconds <= 0)
            {
                throw new ArgumentException("timeSpan should be strictly positive", nameof(timeSpan));
            }

            if (string.IsNullOrEmpty(redisConnectString) && typeof(T) == typeof(RedisRateLimiter))
            {
                throw new ArgumentException("redisConnectString is empty", nameof(redisConnectString));
            }

            service.AddOptions().Configure<RateLimitRule>(rule =>
                 {
                     rule.LimitNumber = count;
                     rule.TimeSpan = timeSpan;
                     rule.RedisConnectString = redisConnectString;
                 });

            service.AddScoped<IRateLimiter, T>();
            service.AddSingleton<TimeLimiter>();
        }
    }
}
