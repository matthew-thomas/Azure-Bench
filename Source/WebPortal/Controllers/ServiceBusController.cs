using System;
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

                await client.SendAsync(new BrokeredMessage(sendAsyncParameters.BrokeredMessagePayload));

                return "Der!";
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