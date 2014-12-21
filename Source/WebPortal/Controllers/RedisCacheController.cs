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
                    ServiceName = "Your-Service-Name",
                    ServiceKey  = "Your-Service-Key"
                },
                StringSetParameters = new StringSetViewModel {
                    Key   = "Example-Key",
                    Value = "Example-Value"
                },
                StringGetParameters = new StringGetViewModel {
                    Key   = "Example-Key"
                },
                ExecutionSettings = new ExecutionSettingsViewModel {
                    NumberOfRepititions      = 1,
                    MaxDegreeOfParallelism = 1,
                }
            };

            return View(defaultOptions);
        }

        public
        string
        StringSet(
            RedisServiceSettingsViewModel   serviceSettings,
            StringSetViewModel              stringSetParameters,
            ExecutionSettingsViewModel      executionSettings)
        {
            return ExecuteRedisFunction(
                serviceSettings:    serviceSettings,
                redisFunction:      redisDb => redisDb.StringSet(
                                        stringSetParameters.Key, 
                                        stringSetParameters.Value
                                    ),
                executionSettings:  executionSettings
            );
        }

        public
        string
        StringGet(
            RedisServiceSettingsViewModel   serviceSettings,
            StringGetViewModel              stringGetParameters,
            ExecutionSettingsViewModel      executionSettings)
        {
            return ExecuteRedisFunction(
                serviceSettings:    serviceSettings, 
                redisFunction:      redisDb => redisDb.StringGet(
                                        stringGetParameters.Key
                                    ), 
                executionSettings:  executionSettings
            );
        }

        private 
        static
        string 
        ExecuteRedisFunction(
            RedisServiceSettingsViewModel   serviceSettings,
            Action<IDatabase>               redisFunction,
            ExecutionSettingsViewModel      executionSettings)
        {
            try
            {
                var redisDb = GetRedisDb(serviceSettings);

                var totalStopWatch = Stopwatch.StartNew();

                Parallel.For(
                    fromInclusive:      0,
                    toExclusive:        executionSettings.NumberOfRepititions,
                    parallelOptions:    new ParallelOptions {
                                            MaxDegreeOfParallelism = executionSettings.MaxDegreeOfParallelism
                                        },
                    body:               i => redisFunction(redisDb)
                );

                var totalElapsedMilliseconds    = totalStopWatch.ElapsedMilliseconds;
                var documentsPerSecond          = executionSettings.NumberOfRepititions / (totalElapsedMilliseconds / 1000.0);
                var averageLatencyMilliseconds  = (double)totalElapsedMilliseconds / executionSettings.NumberOfRepititions;

                return string.Format(
                    "Processed: {0} items in {1}ms\r\nRate: {2}/sec\r\nLatency: {3}ms avg. per request",
                    executionSettings.NumberOfRepititions,
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

        private 
        static 
        IDatabase 
        GetRedisDb(
            RedisServiceSettingsViewModel serviceSettings)
        {
            var configurationString = string.Format(
                "{0}.redis.cache.windows.net,password={1}",
                serviceSettings.ServiceName,
                serviceSettings.ServiceKey
            );

            return ConnectionMultiplexer
                .Connect(configurationString)
                .GetDatabase();
        }
    }

    public class RedisBenchViewModel
    {
        [DisplayName("Service Settings")]   public RedisServiceSettingsViewModel    ServiceSettings     { get; set; }
        [DisplayName("StringSet Settings")] public StringSetViewModel               StringSetParameters { get; set; }
        [DisplayName("StringGet Settings")] public StringGetViewModel               StringGetParameters { get; set; }
        [DisplayName("Execution Settings")] public ExecutionSettingsViewModel       ExecutionSettings   { get; set; }
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

    public class ExecutionSettingsViewModel
    {
        public int NumberOfRepititions      { get; set; }
        public int MaxDegreeOfParallelism   { get; set; }        
    }
}