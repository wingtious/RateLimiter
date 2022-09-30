﻿using ComposableAsync;
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
    /// TimeLimiter implementation
    /// </summary>
    public class TimeLimiter
    {
        private readonly ConcurrentDictionary<string, IRateLimiter> dic = new ConcurrentDictionary<string, IRateLimiter>();
        private readonly RateLimitRule _rule;

        public TimeLimiter(IOptions<RateLimitRule> options)
        {
            _rule = options.Value;
            dic["default_TimeLimiter"] = new LocalRateLimiter(options.Value, "default_TimeLimiter");
        }

        /// <summary>
        /// Perform the given task respecting the time constraint
        /// returning the result of given function
        /// </summary>
        /// <param name="perform"></param>
        /// <returns></returns>
        public Task Enqueue<TRateLimiter>(Func<Task> perform, string targetKey= "default_TimeLimiter", RateLimitRule rule = null) where TRateLimiter : IRateLimiter
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
        public Task<T> Enqueue<T, TRateLimiter>(Func<Task<T>> perform, string targetKey = "default_TimeLimiter", RateLimitRule rule = null) where TRateLimiter : IRateLimiter
        {
            return Binding<TRateLimiter>(targetKey, rule).Run(perform,CancellationToken.None);
        }

        /// <summary>
        /// Perform the given task respecting the time constraint
        /// returning the result of given function
        /// </summary>
        /// <param name="perform"></param>
        /// <returns></returns>
        public Task Enqueue<TRateLimiter>(Action perform, string targetKey = "default_TimeLimiter", RateLimitRule rule = null) where TRateLimiter : IRateLimiter
        {
            return Binding<TRateLimiter>(targetKey, rule).Run(perform, CancellationToken.None);
        }

        /// <summary>
        /// Perform the given task respecting the time constraint
        /// returning the result of given function
        /// </summary>
        /// <param name="perform"></param>
        /// <returns></returns>
        public Task Enqueue<T, TRateLimiter>(Func<T> perform, string targetKey = "default_TimeLimiter", RateLimitRule rule = null) where TRateLimiter : IRateLimiter
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
