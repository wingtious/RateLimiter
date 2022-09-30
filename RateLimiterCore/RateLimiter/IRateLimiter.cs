using System;
using System.Threading;
using System.Threading.Tasks;

namespace RateLimiterCore.RateLimiter
{
    /// <summary>
    /// Represents a time constraints that can be awaited
    /// </summary>
    public interface IRateLimiter
    {
        Task Invoke(CancellationToken cancellationToken);
    }

    public static class IRateLimiterExtend
    {

        /// <summary>
        /// Perform the given task respecting the time constraint
        /// </summary>
        /// <param name="perform"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public static Task Run(this IRateLimiter rateLimiter, Action perform, CancellationToken cancellationToken)
        {
            var transformed = Transform(perform);
            return Run(rateLimiter, transformed, cancellationToken);
        }

        /// <summary>
        /// Perform the given task respecting the time constraint
        /// returning the result of given function
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="perform"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public static Task<T> Run<T>(this IRateLimiter rateLimiter, Func<T> perform, CancellationToken cancellationToken)
        {
            return Run(rateLimiter, Transform(perform), cancellationToken);
        }

        /// <summary>
        /// Perform the given task respecting the time constraint
        /// </summary>
        /// <param name="perform"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public static async Task Run(this IRateLimiter rateLimiter, Func<Task> perform, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await rateLimiter.Invoke(cancellationToken);
            await perform();
        }

        /// <summary>
        /// Perform the given task respecting the time constraint
        /// returning the result of given function
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="perform"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public static async Task<T> Run<T>(this IRateLimiter rateLimiter, Func<Task<T>> perform, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await rateLimiter.Invoke(cancellationToken);
            return await perform();
        }

        private static Func<Task> Transform(Action act)
        {
            return () => { act(); return Task.FromResult(0); };
        }

        /// <summary>
        /// Perform the given task respecting the time constraint
        /// returning the result of given function
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="compute"></param>
        /// <returns></returns>
        private static Func<Task<T>> Transform<T>(Func<T> compute)
        {
            return () => Task.FromResult(compute());
        }
    }
}
