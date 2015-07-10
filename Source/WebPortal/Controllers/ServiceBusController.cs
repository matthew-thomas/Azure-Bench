using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;
using WebGrease.Css.Extensions;
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
                ServiceSettings     = new ServiceBusServiceSettingsViewModel {
                                        Namespace               = "Your-Namespace",
                                        AccessKey               = "Your-Access-Key"
                                    },
                QueueSettings       = new ServiceBusQueueSettingsViewModel {
                                        Path                    = "Your-Queue-Name"
                                    },
                SendAsyncParameters = new SendAsyncViewModel {
                                        BrokeredMessagePayload  = "{SomeProperty:1}"
                                    },
                ReceiveParameters   = new ReceiveParametersViewModel {
                                        ReceiveMode             = ReceiveMode.PeekLock
                                    },
                ExecutionSettings   = new ExecutionSettingsViewModel {
                                        NumberOfRepititions     = 1,
                                        MaxDegreeOfParallelism  = 1
                                    }
            };

            return View(defaultOptions);
        }

        public 
        async Task<string>
        SendAsync(
            ServiceBusServiceSettingsViewModel  serviceSettings,
            ServiceBusQueueSettingsViewModel    queueSettings,
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

                if (!await namespaceManager.QueueExistsAsync(queueSettings.Path))
                    await namespaceManager.CreateQueueAsync(queueSettings.Path);

                var client = QueueClient.CreateFromConnectionString(serviceBusConnectionString, queueSettings.Path);

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
                return ex.ToString();
            }
        }

        public 
        string
        ProcessQueueMessages(
            ServiceBusServiceSettingsViewModel  serviceSettings,
            ServiceBusQueueSettingsViewModel    queueSettings,
            ReceiveParametersViewModel          receiveParameters,
            ExecutionSettingsViewModel          executionSettings)
        {
            try
            {
                var serviceBusConnectionString = FormatConnectionString(serviceSettings);
                var namespaceManager           = NamespaceManager.CreateFromConnectionString(serviceBusConnectionString);

                if (!namespaceManager.QueueExists(queueSettings.Path))
                    namespaceManager.CreateQueue(queueSettings.Path);

                var client = QueueClient.CreateFromConnectionString(
                    serviceBusConnectionString, 
                    queueSettings.Path, 
                    ReceiveMode.ReceiveAndDelete
                );

                var messages = new ConcurrentDictionary<string, string>();
                
                var totalStopWatch = Stopwatch.StartNew();

                Parallel.For(
                    fromInclusive:      0,
                    toExclusive:        executionSettings.NumberOfRepititions,
                    parallelOptions:    new ParallelOptions {
                                            MaxDegreeOfParallelism = executionSettings.MaxDegreeOfParallelism
                                        },
                    body:               i => {
                                            var message = client.Receive();

                                            if (message != null)
                                            {
                                                messages.TryAdd(message.MessageId, message.GetBody<string>());
                                            }
                                        }
                );

                var totalElapsedMilliseconds   = totalStopWatch.ElapsedMilliseconds;
                var messagesPerSecond          = executionSettings.NumberOfRepititions / (totalElapsedMilliseconds / 1000.0);
                var averageLatencyMilliseconds = (double)totalElapsedMilliseconds / executionSettings.NumberOfRepititions;

                var messageLog = new StringBuilder();

                messages.ForEach(pair => messageLog.AppendFormat("{0} {1}\r\n", pair.Key, pair.Value));

                return string.Format(
                    "Received {0} messages in {1}ms\r\nCalculated Rates: {2}/sec @ {3}ms/msg avg:\r\n{4}",
                    executionSettings.NumberOfRepititions,
                    totalElapsedMilliseconds,
                    messagesPerSecond.ToString("0.00"),
                    averageLatencyMilliseconds,
                    messageLog
                );
            }
            catch (Exception ex)
            {
                return ex.ToString();
            }
        }

        private
        string
        FormatConnectionString(
            ServiceBusServiceSettingsViewModel serviceSettings)
        {
            return string.Format(
                "Endpoint=sb://{0}.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey={1}",
                serviceSettings.Namespace,
                serviceSettings.AccessKey
            );
        }
    }

    public class ServiceBusBenchViewModel
    {
        public ServiceBusServiceSettingsViewModel   ServiceSettings     { get; set; }
        public ServiceBusQueueSettingsViewModel     QueueSettings       { get; set; }
        public SendAsyncViewModel                   SendAsyncParameters { get; set; }
        public ExecutionSettingsViewModel           ExecutionSettings   { get; set; }
        public ReceiveParametersViewModel           ReceiveParameters   { get; set; }
    }

    public class ServiceBusQueueSettingsViewModel
    {
        public string Path { get; set; }
    }

    public class ReceiveParametersViewModel
    {
        public ReceiveMode ReceiveMode { get; set; }
    }

    public class SendAsyncViewModel
    {
        public string BrokeredMessagePayload { get; set; }
    }

    public class ServiceBusServiceSettingsViewModel
    {
        public string Namespace { get; set; }
        public string AccessKey { get; set; }
    }
}