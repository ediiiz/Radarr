using System.Collections.Generic;
using Dapper;
using NLog;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Datastore;

namespace NzbDrone.Core.Housekeeping.Housekeepers
{
    public class CleanupOrphanedMovieFiles : IHousekeepingTask
    {
        private readonly IMainDatabase _database;
        private readonly Logger _logger;

        public CleanupOrphanedMovieFiles(IMainDatabase database, Logger logger)
        {
            _database = database;
            _logger = logger;
        }

        public void Clean()
        {
            using (var mapper = _database.OpenConnection())
            {
                var toDelete = mapper.Query<string>(
                    @"SELECT RelativePath
                      FROM MovieFiles
                      WHERE Id IN (
                      SELECT MovieFiles.Id FROM MovieFiles
                      LEFT OUTER JOIN Movies
                      ON MovieFiles.Id = Movies.MovieFileId
                      WHERE Movies.Id IS NULL)");

                _logger.Trace($"Deleting MovieFiles:\n{toDelete.ConcatToString("\n")}");

                mapper.Execute(@"DELETE FROM MovieFiles
                                 WHERE Id IN (
                                 SELECT MovieFiles.Id FROM MovieFiles
                                 LEFT OUTER JOIN Movies
                                 ON MovieFiles.Id = Movies.MovieFileId
                                 WHERE Movies.Id IS NULL)");
            }
        }
    }
}
