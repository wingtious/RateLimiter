using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RateLimiterCore.Common
{
    /// <summary>
    /// Fixed Window Algorithm
    /// </summary>
    public class RateLimitRule
    {
        /// <summary>
        /// The statistical time window, which counts the number of requests in this time.
        /// When using redis storage, it needs to be an integral multiple of one second.
        /// </summary>
        public TimeSpan TimeSpan { get; set; }

        /// <summary>
        /// The threshold of triggering rate limiting in the statistical time window.
        /// If less than 0, it means no limit.
        /// </summary>
        public int LimitNumber { get; set; }

        public string RedisConnectString { get; set; }
    }
}
