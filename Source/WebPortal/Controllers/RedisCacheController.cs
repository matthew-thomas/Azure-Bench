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
            var defaultOptions = new RedisBenchViewModel {
                ServiceSettings = new RedisServiceSettingsViewModel {
                    ServiceName = "your-service-name-here",
                    ServiceKey  = "your-service-key-here"
                },
                StringSetParameters = new StringSetViewModel {
                    Key   = "Example-Key",
                    Value = "Example-Value"
                },
                StringGetParameters = new StringGetViewModel {
                    Key = "Example-Key"
                },
                CallSettings = new CallSettingsViewModel {
                    NumberOfRepititions      = 1,
                    MaxConcurrentSubmissions = 1,
                }
            };

            return View(defaultOptions);
        }

        public
        string
        StringSet(
            RedisServiceSettingsViewModel   serviceSettings,
            StringSetViewModel              stringSetParameters,
            CallSettingsViewModel           callSettings)
        {
            return ExecuteRedisFunction(
                serviceSettings:    serviceSettings,
                redisFunction:      redisDb => redisDb.StringSet(
                                        stringSetParameters.Key, 
                                        stringSetParameters.Value
                                    ),
                callSettings:       callSettings
            );
        }

        public
        string
        StringGet(
            RedisServiceSettingsViewModel   serviceSettings,
            StringGetViewModel              stringGetParameters,
            CallSettingsViewModel           callSettings)
        {
            return ExecuteRedisFunction(
                serviceSettings:    serviceSettings, 
                redisFunction:      redisDb => redisDb.StringGet(
                                        stringGetParameters.Key
                                    ), 
                callSettings:       callSettings
            );
        }

        private 
        static
        string 
        ExecuteRedisFunction(
            RedisServiceSettingsViewModel   serviceSettings,
            Action<IDatabase>               redisFunction,
            CallSettingsViewModel           callSettings)
        {
            try
            {
                var configurationString = string.Format(
                    "{0}.redis.cache.windows.net,password={1}",
                    serviceSettings.ServiceName, 
                    serviceSettings.ServiceKey
                );

                var redisDb = ConnectionMultiplexer
                    .Connect(configurationString)
                    .GetDatabase();

                var totalStopWatch = Stopwatch.StartNew();

                Parallel.For(
                    fromInclusive:      0,
                    toExclusive:        callSettings.NumberOfRepititions,
                    parallelOptions:    new ParallelOptions {
                                            MaxDegreeOfParallelism = callSettings.MaxConcurrentSubmissions
                                        },
                    body:               i => redisFunction(redisDb)
                );

                var totalElapsedMilliseconds    = totalStopWatch.ElapsedMilliseconds;
                var documentsPerSecond          = callSettings.NumberOfRepititions / (totalElapsedMilliseconds / 1000.0);
                var averageLatencyMilliseconds  = (double)totalElapsedMilliseconds / callSettings.NumberOfRepititions;

                return string.Format(
                    "Set {0} items in {1}ms\r\nRate: {2}/sec @ {3}ms/set avg",
                    callSettings.NumberOfRepititions,
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

    public class RedisBenchViewModel
    {
        [DisplayName("Service Settings")]   public RedisServiceSettingsViewModel    ServiceSettings     { get; set; }
        [DisplayName("StringSet Settings")] public StringSetViewModel               StringSetParameters { get; set; }
        [DisplayName("StringGet Settings")] public StringGetViewModel               StringGetParameters { get; set; }
        [DisplayName("Call Settings")]      public CallSettingsViewModel            CallSettings        { get; set; }
    }

    public class RedisServiceSettingsViewModel
    {
        public string ServiceName   { get; set; }
        public string ServiceKey    { get; set; }
    }

    public class StringSetViewModel
    {
        public string Key              { get; set; }
        public string Value            { get; set; }
    }

    public class StringGetViewModel
    {
        public string Key { get; set; }
    }

    public class CallSettingsViewModel
    {
        public int NumberOfRepititions      { get; set; }
        public int MaxConcurrentSubmissions { get; set; }        
    }
}