using System;
using NzbDrone.Common.Http;
using System.Collections.Generic;
using NLog;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Serializer;
using NzbDrone.Core.MetadataSource.SkyHook.Resource;

namespace NzbDrone.Core.NetImport.TMDb.Popular
{
    public class TMDbPopularRequestGenerator : INetImportRequestGenerator
    {
        public TMDbPopularSettings Settings { get; set; }
        public IHttpClient HttpClient { get; set; }
        public IHttpRequestBuilderFactory RequestBuilder { get; set; }
        public Logger Logger { get; set; }

        public int MaxPages { get; set; }

        public TMDbPopularRequestGenerator()
        {
            MaxPages = 3;
        }

        public virtual NetImportPageableRequestChain GetMovies()
        {
            var minVoteCount = Settings.FilterCriteria.MinVotes;
            var minVoteAverage = Settings.FilterCriteria.MinVoteAverage;
            var ceritification = Settings.FilterCriteria.Ceritification;
            var includeGenreIds = Settings.FilterCriteria.IncludeGenreIds;
            var excludeGenreIds = Settings.FilterCriteria.ExcludeGenreIds;
            var languageCode = (TMDbLanguageCodes)Settings.FilterCriteria.LanguageCode;

            var todaysDate = DateTime.Now.ToString("yyyy-MM-dd");
            var threeMonthsAgo = DateTime.Parse(todaysDate).AddMonths(-3).ToString("yyyy-MM-dd");
            var threeMonthsFromNow = DateTime.Parse(todaysDate).AddMonths(3).ToString("yyyy-MM-dd");

            // TODO: Fix this like persons
            if (ceritification.IsNotNullOrWhiteSpace())
            {
                ceritification = $"&certification_country=US&certification={ceritification}";
            }

            var requestBuilder = RequestBuilder.Create();

            switch (Settings.ListType)
            {
                case (int)TMDbPopularListType.Theaters:
                    requestBuilder = requestBuilder.Resource("/3/discover/movie")
                        .AddQueryParam("primary_release.gte", threeMonthsAgo)
                        .AddQueryParam("primary_release_date.lte", todaysDate);
                    break;
                case (int)TMDbPopularListType.Popular:
                    requestBuilder = requestBuilder.Resource("/3/discover/movie")
                        .AddQueryParam("sort_by", "popularity.desc");
                    break;
                case (int)TMDbPopularListType.Top:
                    requestBuilder = requestBuilder.Resource("/3/discover/movie")
                        .AddQueryParam("sort_by", "vote_average.desc");
                    break;
                case (int)TMDbPopularListType.Upcoming:
                    requestBuilder = requestBuilder.Resource("/3/discover/movie")
                        .AddQueryParam("primary_release.gte", todaysDate)
                        .AddQueryParam("primary_release_date.lte", threeMonthsFromNow);
                    break;
            }

            var pageableRequests = new NetImportPageableRequestChain();

            requestBuilder = requestBuilder
                .AddQueryParam("vote_count.gte", minVoteCount)
                .AddQueryParam("vote_average.gte", minVoteAverage)
                .AddQueryParam("with_genres", includeGenreIds)
                .AddQueryParam("without_genres", excludeGenreIds)
                .AddQueryParam("certification_country", "US")
                .AddQueryParam("certification", ceritification)
                .AddQueryParam("with_original_language", languageCode)
                .Accept(HttpAccept.Json);

            var request = requestBuilder.Build();
            request.Method = HttpMethod.GET;

            var response = HttpClient.Execute(request);
            var result = Json.Deserialize<MovieSearchRoot>(response.Content);

            int totalPages = result.total_pages;
            var url = requestBuilder.Build().Url.ToString();

            pageableRequests.Add(GetMovies(url, totalPages));

            return pageableRequests;
        }

        private IEnumerable<NetImportRequest> GetMovies(string tmdbParams, int totalPages)
        {
            var baseUrl = $"{Settings.Link.TrimEnd("/")}{tmdbParams}";

            for (var pageNumber = 1; pageNumber <= totalPages; pageNumber++)
            {
                // Limit the amount of pages
                if (pageNumber >= MaxPages + 1)
                {
                    Logger.Info(
                        $"Found more than {MaxPages} pages, skipping the {totalPages - (MaxPages + 1)} remaining pages");
                    break;
                }

                Logger.Info($"Importing TMDb movies from: {baseUrl}&page={pageNumber}");
                yield return new NetImportRequest($"{baseUrl}&page={pageNumber}", HttpAccept.Json);
            }

        }
    }
}