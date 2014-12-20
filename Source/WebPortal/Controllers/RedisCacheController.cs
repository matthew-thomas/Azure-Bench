using System;
using System.ComponentModel;
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
            var defaultOptions = new RedisBenchView {
                ServiceSettings = new RedisServiceSettings {
                    ServiceName = "your-service-name-here",
                    ServiceKey  = "your-service-key-here"
                },
                StringSetParameters = new StringSetParameters {
                    Key   = "Example-Key",
                    Value = "Example-Value"
                },
                CallSettings = new CallSettings {
                    NumberOfRepititions      = 1,
                    MaxConcurrentSubmissions = 1,
                }
            };

            return View(defaultOptions);
        }

        public
        string
        StringSet(
            RedisBenchView options)
        {
            try
            {
                var redis =
                    ConnectionMultiplexer.Connect(options.ServiceSettings.ServiceName + ".redis.cache.windows.net,password=" +
                                                  options.ServiceSettings.ServiceKey);
                var redisDb = redis.GetDatabase();

                var totalStopWatch = Stopwatch.StartNew();

                Parallel.For(
                    fromInclusive:      0,
                    toExclusive:        options.CallSettings.NumberOfRepititions,
                    parallelOptions:    new ParallelOptions {
                                            MaxDegreeOfParallelism = options.CallSettings.MaxConcurrentSubmissions
                                        },
                    body:               i => redisDb.StringSet(options.StringSetParameters.Key, options.StringSetParameters.Value)                                        
                );

                var totalElapsedMilliseconds    = totalStopWatch.ElapsedMilliseconds;
                var documentsPerSecond          = options.CallSettings.NumberOfRepititions / (totalElapsedMilliseconds / 1000.0);
                var averageLatencyMilliseconds  = (double)totalElapsedMilliseconds / options.CallSettings.NumberOfRepititions;

                return string.Format(
                    "Set {0} cache items in {1}ms\r\nCalculated Rates: {2}/sec @ {3}ms/set avg",
                    options.CallSettings.NumberOfRepititions,
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

    public class RedisBenchView
    {
        [DisplayName("Service Settings")]   public RedisServiceSettings ServiceSettings     { get; set; }
        [DisplayName("StringSet Settings")] public StringSetParameters  StringSetParameters { get; set; }
        [DisplayName("Call Settings")]      public CallSettings         CallSettings        { get; set; }
    }

    public class RedisServiceSettings
    {
        public string ServiceName   { get; set; }
        public string ServiceKey    { get; set; }
    }

    public class StringSetParameters
    {
        public string Key              { get; set; }
        public string Value            { get; set; }
    }

    public class CallSettings
    {
        public int NumberOfRepititions      { get; set; }
        public int MaxConcurrentSubmissions { get; set; }        
    }
}