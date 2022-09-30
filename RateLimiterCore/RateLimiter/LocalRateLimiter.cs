using Microsoft.Extensions.Options;
using RateLimiterCore.Common;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace RateLimiterCore.RateLimiter
{
    public class LocalRateLimiter : IRateLimiter
    {
        private readonly RateLimitRule _rule;

        /// <summary>
        /// Stack of the last time stamps
        /// </summary>
        protected LimitedSizeStack<DateTime> _TimeStamps { get; }

        private SemaphoreSlim _Semaphore { get; } = new SemaphoreSlim(1, 1);


        public LocalRateLimiter(RateLimitRule rule, string targetKey)
        {
            _rule = rule;
            _TimeStamps = new LimitedSizeStack<DateTime>(_rule.LimitNumber);
        }

        /// <summary>
        /// returns a task that will complete once the constraint is fulfilled
        /// </summary>
        /// <param name="cancellationToken">
        /// Cancel the wait
        /// </param>
        /// <returns>
        /// A disposable that should be disposed upon task completion
        /// </returns>
        public async Task Invoke(CancellationToken cancellationToken)
        {
            await _Semaphore.WaitAsync(cancellationToken);
            var count = 0;
            var now = DateTime.Now;
            var target = now - _rule.TimeSpan;
            LinkedListNode<DateTime> element = _TimeStamps.First, last = null;
            while (element != null && element.Value > target)
            {
                last = element;
                element = element.Next;
                count++;
            }

            if (count >= _rule.LimitNumber)
            {
                Debug.Assert(element == null);
                Debug.Assert(last != null);
                var timeToWait = last.Value.Add(_rule.TimeSpan) - now;
                try
                {
                    await Task.Delay(timeToWait, cancellationToken);
                }
                catch (Exception)
                {
                    _Semaphore.Release();
                    throw;
                }

            }
               
            OnEnded();
        }

        private void OnEnded()
        {
            var now = DateTime.Now;
            _TimeStamps.Push(now);
            _Semaphore.Release();
        }


        /// <summary>
        /// LinkedList with a limited size
        /// If the size exceeds the limit older entry are removed
        /// </summary>
        /// <typeparam name="T"></typeparam>
        protected class LimitedSizeStack<T> : LinkedList<T>
        {
            private readonly int _MaxSize;

            /// <summary>
            /// Construct the LimitedSizeStack with the given limit
            /// </summary>
            /// <param name="maxSize"></param>
            public LimitedSizeStack(int maxSize)
            {
                _MaxSize = maxSize;
            }

            /// <summary>
            /// Push new entry. If he size exceeds the limit, the oldest entry is removed
            /// </summary>
            /// <param name="item"></param>
            public void Push(T item)
            {
                AddFirst(item);

                if (Count > _MaxSize)
                    RemoveLast();
            }
        }
    }
}
