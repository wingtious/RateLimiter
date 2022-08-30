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
}
