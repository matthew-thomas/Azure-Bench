using System;
using System.Diagnostics;
using System.Linq;
using Microsoft.ServiceBus.Messaging;
using System.Threading.Tasks;
using System.Web.Mvc;
using Microsoft.ServiceBus;
using WebPortal.Models;

namespace WebPortal.Controllers
{
    public class ServiceBusController : Controller
    {
        public 
        ActionResult
        Index()
        {
            var defaultOptions = new ServiceBusBenchViewModel
            {
                ServiceSettings = new ServiceBusServiceSettingsViewModel {
                    Namespace = "Your-Namespace",
                    AccessKey = "Your-Access-Key"
                },
                SendAsyncParameters = new SendAsyncViewModel {
                    Path                    = "Your-Queue-Name",
                    BrokeredMessagePayload  = "{SomeProperty:1}"
                },
                ExecutionSettings = new ExecutionSettingsViewModel {
                    NumberOfRepititions    = 1,
                    MaxDegreeOfParallelism = 1
                }
            };

            return View(defaultOptions);
        }

        public 
        async Task<string>
        SendAsync(
            ServiceBusServiceSettingsViewModel  serviceSettings,
            SendAsyncViewModel                  sendAsyncParameters,
            ExecutionSettingsViewModel          executionSettings)
        {
            try
            {
                var serviceBusConnectionString = string.Format(
                    "Endpoint=sb://{0}.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey={1}",
                    serviceSettings.Namespace,
                    serviceSettings.AccessKey
                    );

                var namespaceManager = NamespaceManager.CreateFromConnectionString(serviceBusConnectionString);

                if (!await namespaceManager.QueueExistsAsync(sendAsyncParameters.Path))
                    await namespaceManager.CreateQueueAsync(sendAsyncParameters.Path);

                var client = QueueClient.CreateFromConnectionString(serviceBusConnectionString, sendAsyncParameters.Path);

                var totalStopWatch = Stopwatch.StartNew();

                Parallel.For(
                    fromInclusive:      0,
                    toExclusive:        executionSettings.NumberOfRepititions,
                    parallelOptions:    new ParallelOptions {
                                            MaxDegreeOfParallelism = executionSettings.MaxDegreeOfParallelism
                                        },
                    body:               i => client.Send(new BrokeredMessage(sendAsyncParameters.BrokeredMessagePayload) { MessageId = i.ToString() })
                );

                var totalElapsedMilliseconds   = totalStopWatch.ElapsedMilliseconds;
                var messagesPerSecond          = executionSettings.NumberOfRepititions / (totalElapsedMilliseconds / 1000.0);
                var averageLatencyMilliseconds = (double)totalElapsedMilliseconds / executionSettings.NumberOfRepititions;
                
                return string.Format(
                    "Submitted {0} messages in {1}ms\r\nCalculated Rates: {2}/sec @ {3}ms/msg avg",
                    executionSettings.NumberOfRepititions,
                    totalElapsedMilliseconds,
                    messagesPerSecond.ToString("0.00"),
                    averageLatencyMilliseconds
                );
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }
    }

    public class ServiceBusBenchViewModel
    {
        public ServiceBusServiceSettingsViewModel   ServiceSettings     { get; set; }
        public SendAsyncViewModel                   SendAsyncParameters { get; set; }
        public ExecutionSettingsViewModel           ExecutionSettings   { get; set; }
    }

    public class SendAsyncViewModel
    {
        public string Path                   { get; set; }
        public string BrokeredMessagePayload { get; set; }
    }

    public class ServiceBusServiceSettingsViewModel
    {
        public string Namespace { get; set; }
        public string AccessKey { get; set; }
    }
}