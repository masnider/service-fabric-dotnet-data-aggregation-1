﻿// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace System.Net.Http
{
    using HealthMetrics.Common;
    using Microsoft.ServiceFabric.Services.Client;
    using Microsoft.ServiceFabric.Services.Communication.Client;
    using Newtonsoft.Json;
    using System.Collections.Concurrent;
    using System.Fabric;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;

    public static class FabricHttpClient
    {
        private static readonly ConcurrentDictionary<Uri, bool?> addresses;
        private static readonly FabricClient fabricClient;
        private static readonly HttpClient httpClient;
        private static readonly HttpCommunicationClientFactory clientFactory;
        private static readonly JsonSerializer jSerializer;

        static FabricHttpClient()
        {
            addresses = new ConcurrentDictionary<Uri, bool?>();
            fabricClient = new FabricClient();
            HttpClientHandler handler = new HttpClientHandler();

            if (handler.SupportsAutomaticDecompression)
            {
                handler.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
            }

            httpClient = new HttpClient(handler);

            clientFactory = new HttpCommunicationClientFactory(
                ServicePartitionResolver.GetDefault(),
                "endpointName",
                TimeSpan.FromSeconds(2),
                TimeSpan.FromSeconds(2));

            jSerializer = new JsonSerializer();  //todo - see if creating this on the fly is better or not 
        }

        public static Task<TReturn> MakeGetRequest<TReturn>(
            Uri serviceName,
            ServicePartitionKey key,
            string endpointName,
            string requestPath,
            CancellationToken ct
        )
        {
            return MakeHttpRequest<TReturn, string>(
                    serviceName,
                    key,
                    endpointName,
                    requestPath,
                    null,
                    HttpVerb.GET,
                    SerializationSelector.JSON,
                    ct
                    );
        }

        public static Task<TReturn> MakePostRequest<TReturn, TPayload>(
            Uri serviceName,
            ServicePartitionKey key,
            string endpointName,
            string requestPath,
            TPayload payload,
            SerializationSelector selector,
            CancellationToken ct
        )
        {
            return MakeHttpRequest<TReturn, TPayload>(
                    serviceName,
                    key,
                    endpointName,
                    requestPath,
                    payload,
                    HttpVerb.POST,
                    selector,
                    ct
                    );
        }

        private static Task<TReturn> MakeHttpRequest<TReturn, TPayload>(
            Uri serviceName,
            ServicePartitionKey key,
            string endpointName,
            string requestPath,
            TPayload payload,
            HttpVerb verb,
            SerializationSelector selector,
            CancellationToken ct
        )
        {
            var servicePartitionClient = new ServicePartitionClient<HttpCommunicationClient>(
                clientFactory,
                serviceName,
                key,
                TargetReplicaSelector.Default,
                endpointName,
                new OperationRetrySettings()
                );

            return servicePartitionClient.InvokeWithRetryAsync(
                async client =>
                {
                    HttpResponseMessage response = null;

                    try
                    {

                        if (addresses.TryAdd(client.BaseAddress, true))
                        {
                            //https://aspnetmonsters.com/2016/08/2016-08-27-httpclientwrong/
                            //but then http://byterot.blogspot.co.uk/2016/07/singleton-httpclient-dns.html
                            //so we do this ala https://github.com/NimaAra/Easy.Common/blob/master/Easy.Common/RestClient.cs
                            ServicePointManager.FindServicePoint(client.BaseAddress).ConnectionLeaseTimeout = 60 * 1000;
                        }

                        Uri newUri = new Uri(client.BaseAddress, requestPath.TrimStart('/'));


                        switch (verb)
                        {

                            case HttpVerb.GET:
                                response = await httpClient.GetAsync(newUri, HttpCompletionOption.ResponseHeadersRead, ct);
                                break;

                            case HttpVerb.POST:

                                //works
                                //using (StringWriter writer = new StringWriter())
                                //{
                                //    using (JsonTextWriter jsonWriter = new JsonTextWriter(writer))
                                //    {
                                //        jSerializer.Serialize(jsonWriter, payload);
                                //        await jsonWriter.FlushAsync();
                                //        await writer.FlushAsync();
                                //        response = await httpClient.PostAsync(newUri, new StringContent(writer.ToString(), Encoding.UTF8, "application/json"), ct);
                                //    }
                                //}

                                //nope
                                //StreamWriter writer = null;
                                //using (MemoryStream ms = new MemoryStream())
                                //{
                                //    writer = new StreamWriter(ms);
                                //    using (JsonWriter jwriter = new JsonTextWriter(writer))
                                //    {
                                //        jSerializer.Serialize(jwriter, payload);
                                //        await jwriter.FlushAsync();
                                //        await writer.FlushAsync();

                                //        HttpContent content = new StreamContent(ms);
                                //        response = await httpClient.PostAsync(newUri, content, ct);
                                //    }
                                //}

                                //if (writer != null)
                                //{
                                //    writer.Dispose();
                                //}

                                //yes
                                response = await httpClient.PostAsJsonAsync<TPayload>(newUri, payload);
                                break;

                            default:
                                throw new ArgumentException("Unsupported HTTP Verb submitted for HTTP message in HTTPClientExtension");
                        }
                    }
                    catch (Exception e)
                    {
                        var x = e;
                    }

                    using (var stream = await response.Content.ReadAsStreamAsync())
                    {
                        using (var streamReader = new StreamReader(stream))
                        {
                            using (JsonReader jsonReader = new JsonTextReader(streamReader))
                            {
                                return jSerializer.Deserialize<TReturn>(jsonReader);
                            }
                        }
                    }
                }, ct);
        }
    }
}
