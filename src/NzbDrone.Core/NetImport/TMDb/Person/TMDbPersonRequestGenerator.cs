using NzbDrone.Common.Http;
using System.Collections.Generic;
using NLog;
using NzbDrone.Common.Extensions;

namespace NzbDrone.Core.NetImport.TMDb.Person
{
    public class TMDbPersonRequestGenerator : INetImportRequestGenerator
    {
        public TMDbPersonSettings Settings { get; set; }
        public IHttpClient HttpClient { get; set; }
        public IHttpRequestBuilderFactory RequestBuilder { get; set; }
        public Logger Logger { get; set; }

        public TMDbPersonRequestGenerator()
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
            Logger.Info($"Importing TMDb movies from person: {Settings.PersonId}");

            var minVoteCount = Settings.FilterCriteria.MinVotes;
            var minVoteAverage = Settings.FilterCriteria.MinVoteAverage;
            var ceritification = Settings.FilterCriteria.Ceritification;
            var includeGenreIds = Settings.FilterCriteria.IncludeGenreIds;
            var excludeGenreIds = Settings.FilterCriteria.ExcludeGenreIds;
            var languageCode = (TMDbLanguageCodes)Settings.FilterCriteria.LanguageCode;

            var requestBuilder = RequestBuilder.Create()
                                               .SetSegment("route", "person")
                                               .SetSegment("id", Settings.PersonId)
                                               .SetSegment("secondaryRoute", "/movie_credits")
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