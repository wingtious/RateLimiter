using Microsoft.Extensions.Options;
using RateLimiterCore.Common;
using StackExchange.Redis;
using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RateLimiterCore.RateLimiter
{
    public class RedisRateLimiter : IRateLimiter
    {
        protected readonly ConnectionMultiplexer _redisClient;
        private readonly RedisLuaScript _incrementLuaScript;
        private readonly string _targetKey = "wikcndPIg5js4VQ25jDsshQPwoh1";
        private readonly RateLimitRule _rule;
        private SemaphoreSlim _Semaphore { get; } = new SemaphoreSlim(1, 1);

        /// <summary>
        /// Create a new instance
        /// </summary>
        /// <param name="rules">The rate limit rules</param>
        /// <param name="redisClient">The redis client</param>
        /// <param name="timeProvider">The time provider</param>
        /// <param name="updatable">If rules can be updated</param>
        public RedisRateLimiter(RateLimitRule rule, string targetKey)
        {
            _targetKey = targetKey;
            _rule = rule;
            _redisClient = ConnectionMultiplexer.Connect(_rule.RedisConnectString);
            if (_redisClient == null)
            {
               throw new Exception("redisClient is null");
            }

            string _incrementLuaScriptText = @"local ret={}
                local cl_key='{' .. KEYS[1] .. '}'
                local lock_key=cl_key .. '-lock'
                local lock_val=redis.call('get',lock_key)
                if lock_val=='1' then
                    ret[1]=1
                    ret[2]=-1
                    ret[3]=redis.call('PTTL',lock_key)
                    return ret;
                end
                ret[1]=0
                local amount=tonumber(ARGV[1])
                local limit_number=tonumber(ARGV[3])
                local lock_seconds=tonumber(ARGV[4])
                local check_result=false
                local current=redis.call('get',KEYS[1])
                if current~=false then
                    current = tonumber(current)
                    local pttl=redis.call('PTTL',KEYS[1])
                    ret[3]=pttl
                    if(limit_number>=0 and current>=limit_number) then
                        check_result=true
                    else
                        redis.call('incrby',KEYS[1],amount)
                        current=current+amount
                    end
                else
                    current=-1
                end
                if current==-1 then
                    redis.call('set',KEYS[1],amount,'PX',ARGV[2])
                    ret[3]=ARGV[2]
                    current=amount
                end
                ret[2]=current
                if check_result then
                    ret[1]=1
                    if lock_seconds>0 then
                        redis.call('set',lock_key,'1','EX',lock_seconds,'NX')
                        ret[3]=lock_seconds*1000
                    end
                end
                return ret";

            _incrementLuaScript = new RedisLuaScript(_redisClient, "Src-IncrWithExpireSec", _incrementLuaScriptText);
        }

        public async Task Invoke(CancellationToken cancellationToken)
        {
            await _Semaphore.WaitAsync(cancellationToken);
            RuleCheckResult result;
            do
            {

                result = BuildCheckResult(_targetKey, _rule, _incrementLuaScript);
                if (result.IsLimit)
                {
                    try
                    {
                        await Task.Delay(result.Wait, cancellationToken);
                    }
                    catch (Exception)
                    {
                        _Semaphore.Release();
                        throw;
                    }
                }
            } while (result != null && result.IsLimit);

            _Semaphore.Release();
        }

        private RuleCheckResult BuildCheckResult(string target, RateLimitRule rule,RedisLuaScript luaScript)
        {
            var ret = (long[])EvaluateScript(luaScript, new RedisKey[] { target },
                         new RedisValue[] { 1, (long)rule.TimeSpan.TotalMilliseconds, rule.LimitNumber, 1 });

            var resetTime = DateTimeOffset.MaxValue;
            var ttl = ret[2];
            if (ttl >= 0)
            {
                resetTime = DateTimeOffset.Now.AddMilliseconds(ttl);
            }
            else if (ttl < -1)
            {
                resetTime = DateTimeOffset.MinValue;
            }

            return new RuleCheckResult()
            {
                IsLimit = ret[0] == 0 ? false : true,
                Count = ret[1],
                Remaining = rule.LimitNumber - ret[1],
                ResetTime = resetTime,
                Wait = resetTime - DateTimeOffset.Now
           };
        }

        /// <summary>
        /// evaluate lua script
        /// </summary>
        /// <param name="luaScript"></param>
        /// <param name="keys"></param>
        /// <param name="values"></param>
        /// <returns></returns>
        private RedisResult EvaluateScript(RedisLuaScript luaScript, RedisKey[] keys, RedisValue[] values)
        {
            RedisServerException _lastException = null;

            for (int attempt = 0; attempt < 5; attempt++)
            {
                try
                {
                    byte[] sha1 = luaScript.Load();
                    IDatabase dataBase = _redisClient.GetDatabase();
                    return dataBase.ScriptEvaluate(sha1, keys, values);
                }
                catch (RedisServerException exception)
                {
                    _lastException = exception;

                    // If the database gets reset, the script can end up cleared. This will force it to be reloaded
                    if (exception.Message.Contains("NOSCRIPT"))
                    {
                        luaScript.ResetLoadStatus();
                        continue;
                    }
                }
            }

            throw _lastException;
        }


        protected class RuleCheckResult
        {
            /// <summary>
            /// If true, it means that the current request should be limited
            /// </summary>
            /// <value></value>
            public bool IsLimit { get; set; }

            /// <summary>
            /// The time to open the next time window,
            /// or the time when the rate limiting lock ends.
            /// </summary>
            /// <value></value>
            public DateTimeOffset ResetTime { get; set; }

            /// <summary>
            /// The number of requests passed in the current time window.
            /// </summary>
            /// <value></value>
            public long Count { get; set; }

            /// <summary>
            /// The number of requests remaining in the current time window that will not be limited.
            /// 
            /// </summary>
            /// <value></value>
            public long Remaining { get; set; }

            /// <summary>
            /// The queue waiting time of the current request, which is only for the leaky bucket algorithm.
            /// With Task.Dealy, you can simulate queue processing requests.
            /// </summary>
            /// <value></value>
            public TimeSpan Wait { get; set; }
        }

        /// <summary>
        /// Define the operation mechanism for Lua script
        /// </summary>
        protected class RedisLuaScript
        {
            private byte[] _sha1;
            private readonly ConnectionMultiplexer _redisClient;
            private readonly static SemaphoreSlim _loadLock = new SemaphoreSlim(1, 1);
            private DateTimeOffset _reloadTs;
            private readonly object _reloadLocker = new object();

            /// <summary>
            /// Create a new instace
            /// </summary>
            /// <param name="redisClient"></param>
            /// <param name="name"></param>
            /// <param name="script"></param>
            public RedisLuaScript(ConnectionMultiplexer redisClient, string name, string script)
            {
                _redisClient = redisClient;
                _reloadTs = DateTimeOffset.MinValue;
                Name = name;
                Script = script;
            }

            /// <summary>
            /// The name of script
            /// </summary>
            /// <value></value>
            public string Name { get; private set; }

            /// <summary>
            /// The content of script
            /// </summary>
            /// <value></value>
            public string Script { get; private set; }

            /// <summary>
            /// Async load script in the redis server
            /// </summary>
            /// <returns></returns>
            public async ValueTask<byte[]> LoadAsync()
            {
                if (_sha1 == null)
                {
                    await _loadLock.WaitAsync().ConfigureAwait(false);

                    try
                    {
                        if (_sha1 == null)
                        {
                            var tmpSHA1 = CalcLuaSHA1();

                            var endPoints = _redisClient.GetEndPoints();
                            foreach (var endpoint in endPoints)
                            {
                                var server = _redisClient.GetServer(endpoint);
                                if (server.IsConnected)
                                {
                                    bool exists = await server.ScriptExistsAsync(tmpSHA1).ConfigureAwait(false);
                                    if (!exists)
                                    {
                                        await server.ScriptLoadAsync(Script).ConfigureAwait(false);
                                    }
                                }
                            }

                            // When reset load status, other threads may change '_sha1' to null, so 'tmpSHA1' is returned
                            _sha1 = tmpSHA1;
                            return tmpSHA1;
                        }
                    }
                    finally
                    {
                        _loadLock.Release();
                    }
                }

                return _sha1;
            }

            /// <summary>
            /// Load script in the redis server
            /// </summary>
            /// <returns></returns>
            public byte[] Load()
            {
                if (_sha1 == null)
                {
                    _loadLock.Wait();
                    try
                    {
                        if (_sha1 == null)
                        {
                            var tmpSHA1 = CalcLuaSHA1();

                            var endPoints = _redisClient.GetEndPoints();
                            Array.ForEach(endPoints, endpoint =>
                            {
                                var server = _redisClient.GetServer(endpoint);
                                if (server.IsConnected)
                                {
                                    if (!server.ScriptExists(tmpSHA1))
                                    {
                                        server.ScriptLoad(Script);
                                    }
                                }
                            });

                            // When reset load status, other threads may change '_sha1' to null, so 'tmpSHA1' is returned
                            _sha1 = tmpSHA1;
                            return tmpSHA1;
                        }
                    }
                    finally
                    {
                        _loadLock.Release();
                    }
                }

                return _sha1;
            }

            /// <summary>
            /// Reset the load status of the script, forcing it to load again next time.
            /// </summary>
            internal void ResetLoadStatus()
            {
                lock (_reloadLocker)
                {
                    var now = DateTimeOffset.Now;
                    if (now.Subtract(_reloadTs).TotalMilliseconds > 1000)
                    {
                        _sha1 = null;
                        _reloadTs = now;
                    }
                }
            }

            private byte[] CalcLuaSHA1()
            {
                using (SHA1 sha1Service = new SHA1CryptoServiceProvider())
                {
                    return sha1Service.ComputeHash(Encoding.Default.GetBytes(Script));
                }
            }
        }
    }
}
