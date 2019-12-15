﻿using System;
using NzbDrone.Common.Http;
using System.Collections.Generic;
using NLog;
using NzbDrone.Common.Cloud;

namespace NzbDrone.Core.NetImport.TMDb.Collection
{
    public class TMDbCollectionRequestGenerator : INetImportRequestGenerator
    { 
        public TMDbCollectionSettings Settings { get; set; }
        public IHttpClient HttpClient { get; set; }
        public IHttpRequestBuilderFactory RequestBuilder { get; set; }
        public Logger Logger { get; set; }

        public int MaxPages { get; set; }

        public TMDbCollectionRequestGenerator()
        {
        }

        public virtual NetImportPageableRequestChain GetMovies()
        {
            var pageableRequests = new NetImportPageableRequestChain();

            pageableRequests.Add(GetMoviesRequest());

            return pageableRequests;
        }

        private IEnumerable<NetImportRequest> GetMoviesRequest()
        {
            Logger.Info($"Importing TMDb movies from collection: {Settings.CollectionId}");

            yield return new NetImportRequest(RequestBuilder.Create()
                                                           .SetSegment("route", "collection")
                                                           .SetSegment("id", Settings.CollectionId)
                                                           .SetSegment("secondaryRoute", "")
                                                           .Build());
        }
    }
}