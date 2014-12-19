using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Microsoft.Azure.Documents.Linq;
using Newtonsoft.Json.Linq;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace WebPortal.Controllers
{
    public class DocumentDbController : Controller
    {
        public 
        ActionResult 
        Index()
        {
            var defaultSettings = new SubmitToDocumentDb {
                DocumentDbServiceName    = "your-service-name-here",
                DocumentDbServiceKey     = "your-service-key-here",
                DatabaseId               = "Example-Db",
                CollectionId             = "Example-Collection",
                DocumentPayload          = "{'FirstName':'John','LastName':'Doe','DateOfBirth':'1980-01-01'}",
                NumberOfDocuments        = 1,
                MaxConcurrentSubmissions = 1
            };

            return View(defaultSettings);
        }

        public 
        async Task<string>
        SubmitToDocumentDb(
            SubmitToDocumentDb options)
        {
            try
            {
                var document            = JObject.Parse(options.DocumentPayload);
                var client              = GetDocumentClient(options);
                var documentCollection  = await GetDocumentCollectionAsync(
                    client:       client,
                    databaseId:   options.DatabaseId, 
                    collectionId: options.CollectionId
                );

                var totalStopWatch = Stopwatch.StartNew();

                Parallel.For(
                    fromInclusive:      0,
                    toExclusive:        options.NumberOfDocuments,
                    parallelOptions:    new ParallelOptions {
                                            MaxDegreeOfParallelism = options.MaxConcurrentSubmissions
                                        },
                    body:               i => Task.WaitAll(
                                            client.CreateDocumentAsync(
                                                documentCollection.DocumentsLink, 
                                                document
                                            )
                                        )
                );

                var totalElapsedMilliseconds   = totalStopWatch.ElapsedMilliseconds;
                var documentsPerSecond         = options.NumberOfDocuments / (totalElapsedMilliseconds / 1000.0);
                var averageLatencyMilliseconds = (double)totalElapsedMilliseconds/options.NumberOfDocuments;

                return string.Format(
                    "Submitted {0} documents in {1}ms\r\nCalculated Rates: {2}/sec @ {3}ms/doc avg",
                    options.NumberOfDocuments,
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

        static
        async Task<DocumentCollection>
        GetDocumentCollectionAsync(
            DocumentClient  client,
            string          databaseId,
            string          collectionId)
        {
            var database = client
                .CreateDatabaseQuery()
                .AsEnumerable()
                .FirstOrDefault(db => db.Id == databaseId) ??
                    await client.CreateDatabaseAsync(new Database { Id = databaseId });

            var collectionFeed = await client
                .ReadDocumentCollectionFeedAsync(database.CollectionsLink);

            var documentCollection = new DocumentCollection {Id = collectionId};
            documentCollection.IndexingPolicy.Automatic = false;

            return collectionFeed.SingleOrDefault(c => c.Id == collectionId) ??
                await client.CreateDocumentCollectionAsync(
                    database.SelfLink,
                    documentCollection
                );
        }

        static
        DocumentClient 
        GetDocumentClient(
            SubmitToDocumentDb options)
        {
            return new DocumentClient(
                new Uri(string.Format("https://{0}.documents.azure.com:443/", options.DocumentDbServiceName)),
                options.DocumentDbServiceKey
            );
        }
    }

    public class SubmitToDocumentDb
    {
        public string   DocumentDbServiceName       { get; set; }
        public string   DocumentDbServiceKey        { get; set; }
        public string   DatabaseId                  { get; set; }
        public string   CollectionId                { get; set; }
        public int      NumberOfDocuments           { get; set; }
        public string   DocumentPayload             { get; set; }
        public int      MaxConcurrentSubmissions    { get; set; }
    }
}