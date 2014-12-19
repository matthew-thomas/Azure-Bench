using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Web.Mvc;
using StackExchange.Redis;

namespace WebPortal.Controllers
{
    public class RedisCacheController : Controller
    {
        public 
        ActionResult 
        Index()
        {
            var defaultOptions = new SubmitToRedisCache {
                ServiceName = "your-service-name-here",
                ServiceKey  = "your-service-key-here",
                CacheKey    = "Example-Key",
                CacheValue  = "Example-Value"
            };

            return View(defaultOptions);
        }

        public
        string
        SubmitToRedisCache(
            SubmitToRedisCache options)
        {
            try
            {
                var redis =
                    ConnectionMultiplexer.Connect(options.ServiceName + ".redis.cache.windows.net,password=" +
                                                  options.ServiceKey);
                var redisDb = redis.GetDatabase();

                var totalStopWatch = Stopwatch.StartNew();

                Parallel.For(
                    fromInclusive:      0,
                    toExclusive:        options.NumberOfRepititions,
                    parallelOptions:    new ParallelOptions {
                                            MaxDegreeOfParallelism = options.MaxConcurrentSubmissions
                                        },
                    body:               i => { 
                                            if (options.SetOrGet)
                                                redisDb.StringSet(options.CacheKey, options.CacheValue);
                                            else
                                                redisDb.StringGet(options.CacheKey);
                                        }
                );

                var totalElapsedMilliseconds   = totalStopWatch.ElapsedMilliseconds;
                var documentsPerSecond         = options.NumberOfRepititions / (totalElapsedMilliseconds / 1000.0);
                var averageLatencyMilliseconds = (double)totalElapsedMilliseconds/options.NumberOfRepititions;

                return string.Format(
                    "Set {0} cache items in {1}ms\r\nCalculated Rates: {2}/sec @ {3}ms/set avg",
                    options.NumberOfRepititions,
                    totalElapsedMilliseconds,
                    documentsPerSecond.ToString("0.00"),
                    averageLatencyMilliseconds
                );

            }
            catch (Exception exception)
            {
                return exception.ToString();
            }
        }
    }

    public class SubmitToRedisCache
    {
        public bool SetOrGet                { get; set; }
        public string ServiceName               { get; set; }
        public string ServiceKey                   { get; set; }
        public string CacheKey              { get; set; }
        public string CacheValue            { get; set; }
        public int NumberOfRepititions      { get; set; }
        public int MaxConcurrentSubmissions { get; set; }
    }
}