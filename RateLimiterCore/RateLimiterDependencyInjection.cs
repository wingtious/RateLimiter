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
        public static void AddRateLimiter(this IServiceCollection service, TimeSpan timeSpan, int count = 50,string redisConnectString="")
        {
            service.AddOptions().Configure<RateLimitRule>(rule =>
                 {
                     rule.LimitNumber = count;
                     rule.TimeSpan = timeSpan;
                     rule.RedisConnectString = redisConnectString;
                 });

            service.AddSingleton<RateLimiterManager>();
        }
    }
}
