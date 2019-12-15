using NzbDrone.Common.Http;
using System.Collections.Generic;
using NLog;
using NzbDrone.Common.Extensions;

namespace NzbDrone.Core.NetImport.TMDb.List
{
    public class TMDbListRequestGenerator : INetImportRequestGenerator
    {
        public TMDbListSettings Settings { get; set; }
        public IHttpClient HttpClient { get; set; }
        public IHttpRequestBuilderFactory RequestBuilder { get; set; }
        public Logger Logger { get; set; }

        public TMDbListRequestGenerator()
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
            Logger.Info($"Importing TMDb movies from list: {Settings.ListId}");

            var minVoteCount = Settings.FilterCriteria.MinVotes;
            var minVoteAverage = Settings.FilterCriteria.MinVoteAverage;
            var ceritification = Settings.FilterCriteria.Ceritification;
            var includeGenreIds = Settings.FilterCriteria.IncludeGenreIds;
            var excludeGenreIds = Settings.FilterCriteria.ExcludeGenreIds;
            var languageCode = (TMDbLanguageCodes)Settings.FilterCriteria.LanguageCode;

            var requestBuilder = RequestBuilder.Create()
                                               .SetSegment("route", "list")
                                               .SetSegment("id", Settings.ListId)
                                               .SetSegment("secondaryRoute", "")
                                               .AddQueryParam("vote_count.gte", minVoteCount)
                                               .AddQueryParam("vote_average.gte", minVoteAverage)
                                               .AddQueryParam("with_genres", includeGenreIds)
                                               .AddQueryParam("without_genres", excludeGenreIds)
                                               .AddQueryParam("with_original_language", languageCode);

            if (ceritification.IsNotNullOrWhiteSpace())
            {
                requestBuilder.AddQueryParam("certification", ceritification)
                              .AddQueryParam("certification_country", "US");
            }

            yield return new NetImportRequest(requestBuilder.Accept(HttpAccept.Json)
                                                            .Build());

        }
    }
}