using System.Collections.Generic;
using System.Net;
using Newtonsoft.Json;
using NzbDrone.Core.NetImport.Exceptions;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.MetadataSource.SkyHook.Resource;
using NzbDrone.Core.MetadataSource;

namespace NzbDrone.Core.NetImport.TMDb.List
{
    public class TMDbListParser : IParseNetImportResponse
    {
        private NetImportResponse _importResponse;
        private readonly ISearchForNewMovie _skyhookProxy;

        public TMDbListParser(ISearchForNewMovie skyhookProxy)
        {
            _skyhookProxy = skyhookProxy;
        }

        public IList<Movies.Movie> ParseResponse(NetImportResponse importResponse)
        {
            _importResponse = importResponse;

            var movies = new List<Movies.Movie>();

            if (!PreProcess(_importResponse))
            {
                return movies;
            }

            var jsonResponse = JsonConvert.DeserializeObject<ListResponseRoot>(_importResponse.Content);
            // no movies were return
            if (jsonResponse == null)
            {
                return movies;
            }

            foreach (var movie in jsonResponse.items)
            {
                // Movies with no Year Fix
                if (string.IsNullOrWhiteSpace(movie.release_date))
                {
                    continue;
                }
                
                movies.AddIfNotNull(_skyhookProxy.MapMovie(movie));
            }


            return movies;
        }

        protected virtual bool PreProcess(NetImportResponse indexerResponse)
        {
            if (indexerResponse.HttpResponse.StatusCode != HttpStatusCode.OK)
            {
                throw new NetImportException(indexerResponse,
                    "Indexer API call resulted in an unexpected StatusCode [{0}]",
                    indexerResponse.HttpResponse.StatusCode);
            }

            if (indexerResponse.HttpResponse.Headers.ContentType != null &&
                indexerResponse.HttpResponse.Headers.ContentType.Contains("text/json") &&
                indexerResponse.HttpRequest.Headers.Accept != null &&
                !indexerResponse.HttpRequest.Headers.Accept.Contains("text/json"))
            {
                throw new NetImportException(indexerResponse,
                    "Indexer responded with html content. Site is likely blocked or unavailable.");
            }

            return true;
        }
    }
}