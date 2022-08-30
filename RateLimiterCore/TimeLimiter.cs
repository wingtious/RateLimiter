using ComposableAsync;
using RateLimiterCore.RateLimiter;
using StackExchange.Redis;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace RateLimiter
{
    /// <summary>
    /// TimeLimiter implementation
    /// </summary>
    public class TimeLimiter : IDispatcher
    {
        private readonly IRateLimiter _AwaitableConstraint;

        public TimeLimiter(IRateLimiter awaitableConstraint)
        {
            _AwaitableConstraint = awaitableConstraint;
        }

        /// <summary>
        /// Perform the given task respecting the time constraint
        /// returning the result of given function
        /// </summary>
        /// <param name="perform"></param>
        /// <returns></returns>
        public Task Enqueue(Func<Task> perform) 
        {
            return Enqueue(perform, CancellationToken.None);
        }

        /// <summary>
        /// Perform the given task respecting the time constraint
        /// returning the result of given function
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="perform"></param>
        /// <returns></returns>
        public Task<T> Enqueue<T>(Func<Task<T>> perform) 
        {
            return Enqueue(perform, CancellationToken.None);
        }

        /// <summary>
        /// Perform the given task respecting the time constraint
        /// </summary>
        /// <param name="perform"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task Enqueue(Func<Task> perform, CancellationToken cancellationToken) 
        {
            cancellationToken.ThrowIfCancellationRequested();
            await _AwaitableConstraint.Invoke(cancellationToken);
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
        public async Task<T> Enqueue<T>(Func<Task<T>> perform, CancellationToken cancellationToken) 
        {
            cancellationToken.ThrowIfCancellationRequested();
            await _AwaitableConstraint.Invoke(cancellationToken);
            return await perform();
        }

        public IDispatcher Clone() 
        {
            throw new Exception();
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
            return () =>  Task.FromResult(compute()); 
        }

        /// <summary>
        /// Perform the given task respecting the time constraint
        /// </summary>
        /// <param name="perform"></param>
        /// <returns></returns>
        public Task Enqueue(Action perform) 
        {
            var transformed = Transform(perform);
            return Enqueue(transformed);
        }

        /// <summary>
        ///  Perform the given task respecting the time constraint
        /// </summary>
        /// <param name="action"></param>
        public void Dispatch(Action action)
        {
            Enqueue(action);
        }

        /// <summary>
        /// Perform the given task respecting the time constraint
        /// returning the result of given function
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="perform"></param>
        /// <returns></returns>
        public Task<T> Enqueue<T>(Func<T> perform) 
        {
            var transformed = Transform(perform);
            return Enqueue(transformed);
        }

        /// <summary>
        /// Perform the given task respecting the time constraint
        /// returning the result of given function
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="perform"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public Task<T> Enqueue<T>(Func<T> perform, CancellationToken cancellationToken) 
        {
            var transformed = Transform(perform);
            return Enqueue(transformed, cancellationToken);
        }

            /// <summary>
        /// Perform the given task respecting the time constraint
        /// </summary>
        /// <param name="perform"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public Task Enqueue(Action perform, CancellationToken cancellationToken) 
        {
           var transformed = Transform(perform);
           return Enqueue(transformed, cancellationToken);
        }
    }
}
