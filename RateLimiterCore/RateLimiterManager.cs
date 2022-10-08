using ComposableAsync;
using Microsoft.Extensions.Options;
using RateLimiterCore.Common;
using RateLimiterCore.RateLimiter;
using StackExchange.Redis;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace RateLimiter
{
    /// <summary>
    /// RateLimiter Extension implementation
    /// </summary>
    public class RateLimiterManager
    {
        private readonly ConcurrentDictionary<string, IRateLimiter> dic = new ConcurrentDictionary<string, IRateLimiter>();
        private readonly RateLimitRule _rule;

        public RateLimiterManager(IOptions<RateLimitRule> options)
        {
            _rule = options.Value;
            dic["RateLimiter_default_key"] = new LocalRateLimiter(options.Value, "RateLimiter_default_key");
        }

        /// <summary>
        /// Perform the given task respecting the time constraint
        /// returning the result of given function
        /// </summary>
        /// <param name="perform"></param>
        /// <returns></returns>
        public Task Enqueue<TRateLimiter>(Func<Task> perform, string targetKey= "RateLimiter_default_key", RateLimitRule rule = null) where TRateLimiter : IRateLimiter
        {
           return Binding<TRateLimiter>(targetKey, rule).Run(perform, CancellationToken.None);
        }

        /// <summary>
        /// Perform the given task respecting the time constraint
        /// returning the result of given function
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="perform"></param>
        /// <returns></returns>
        public Task<T> Enqueue<T, TRateLimiter>(Func<Task<T>> perform, string targetKey = "RateLimiter_default_key", RateLimitRule rule = null) where TRateLimiter : IRateLimiter
        {
            return Binding<TRateLimiter>(targetKey, rule).Run(perform,CancellationToken.None);
        }

        /// <summary>
        /// Perform the given task respecting the time constraint
        /// returning the result of given function
        /// </summary>
        /// <param name="perform"></param>
        /// <returns></returns>
        public Task Enqueue<TRateLimiter>(Action perform, string targetKey = "RateLimiter_default_key", RateLimitRule rule = null) where TRateLimiter : IRateLimiter
        {
            return Binding<TRateLimiter>(targetKey, rule).Run(perform, CancellationToken.None);
        }

        /// <summary>
        /// Perform the given task respecting the time constraint
        /// returning the result of given function
        /// </summary>
        /// <param name="perform"></param>
        /// <returns></returns>
        public Task Enqueue<T, TRateLimiter>(Func<T> perform, string targetKey = "RateLimiter_default_key", RateLimitRule rule = null) where TRateLimiter : IRateLimiter
        {
            return Binding<TRateLimiter>(targetKey, rule).Run(perform, CancellationToken.None);
        }

        public IRateLimiter Binding<T>(string targetKey, RateLimitRule rule) where T : IRateLimiter
        {
            if (dic.TryGetValue(targetKey, out var inst))
            {
                return inst;
            }
            else
            {
                if (rule == null)
                {
                    rule = _rule;
                }

                return dic.GetOrAdd(targetKey, (IRateLimiter)Activator.CreateInstance(typeof(T), new object[] { rule, targetKey })!);
            } 
        }


    }
}
