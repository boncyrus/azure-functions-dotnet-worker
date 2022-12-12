// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace SampleApp
{
    public class BlobInputBindingSamples
    {
        private readonly ILogger<BlobInputBindingSamples> _logger;

        public BlobInputBindingSamples(ILogger<BlobInputBindingSamples> logger)
        {
            _logger = logger;
        }

        [Function(nameof(BlobInputClientFunction))]
        public async Task<HttpResponseData> BlobInputClientFunction(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequestData req,
            [BlobInput("input-container/sample1.txt", Connection = "AzureWebJobsStorage")] BlobClient client)
        {
            var downloadResult = await client.DownloadContentAsync();
            var content = downloadResult.Value.Content.ToString();
            _logger.LogInformation("Blob content: {content}", content);

            return req.CreateResponse(HttpStatusCode.OK);
        }

        [Function(nameof(BlobInputClientFunction1))]
        public async Task<HttpResponseData> BlobInputClientFunction1(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequestData req,
            [BlobInput("input-container/sample1.txt", Connection = "Storage")] BlobClient client)
        {
            var downloadResult = await client.DownloadContentAsync();
            var content = downloadResult.Value.Content.ToString();
            _logger.LogInformation("Blob content: {content}", content);

            return req.CreateResponse(HttpStatusCode.OK);
        }

        [Function(nameof(BlobCollectionFunction))]
        public HttpResponseData BlobCollectionFunction(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequestData req,
            [BlobInput("input-container")] IEnumerable<BlobClient> blobs)
        {
            _logger.LogInformation("Blobs within container:");
            foreach (BlobClient blob in blobs)
            {
                _logger.LogInformation(blob.Name);
            }

            return req.CreateResponse(HttpStatusCode.OK);
        }

        [Function(nameof(BlobCollectionStringFunction))]
        public HttpResponseData BlobCollectionStringFunction(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequestData req,
            [BlobInput("input-container")] IEnumerable<string> blobs)
        {
            _logger.LogInformation("Blobs within container:");
            foreach (string blob in blobs)
            {
                _logger.LogInformation(blob);
            }

            return req.CreateResponse(HttpStatusCode.OK);
        }

        
        [Function(nameof(BlobMSIFunction))]
        public void BlobMSIFunction(
        [BlobTrigger("blobtest-trigger/{name}", Connection = "")] string myBlob, string name)
        {
            _logger.LogInformation($"C# Blob trigger function Processed blob\n Name: {name} \n Data: {myBlob}");
        }
    }
}
