using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using WikiClientLibrary.Sites;
using WikiClientLibrary.Wikibase;
using WikiClientLibrary.Wikibase.DataTypes;

namespace EntitySeeding
{
    public class PopulateBookInfo1Routine : RoutineBase
    {
        /// <inheritdoc />
        public PopulateBookInfo1Routine(WikiSite site, ILoggerFactory loggerFactory) : base(site, loggerFactory)
        {
        }

        public string FileName { get; set; } = "BookInfo1.tsv";

        public async Task RunAsync()
        {
            var data = File.ReadLines(FileName).Select(l => l.Split('\t'))
                .ToDictionary(f => f[0], f => (Author: f[1], Initiation: Convert.ToDateTime(f[2])));
            var entities = GatherEntitiesRoutine.Load();
            var arcs = PopulateBooksRoutine.LoadBooks();
            foreach (var arc in arcs)
            {
                foreach (var vol in arc.Volumes)
                {
                    if (!data.TryGetValue(vol.Cn, out var info))
                    {
                        Logger.LogWarning("Missing: {Book}", vol.En);
                        continue;
                    }
                    var entity = new Entity(Site, vol.Id);
                    await entity.RefreshAsync(EntityQueryOptions.FetchClaims);
                    if (entity.Claims.ContainsKey("P22"))
                    {
                        Logger.LogInformation("Skipped: {Book}", vol.En);
                        continue;
                    }
                    Logger.LogInformation("Populate: {Book}", vol.En);
                    var entries = new List<EntityEditEntry>();
                    if (!string.IsNullOrEmpty(info.Author))
                    {
                        entries.Add(
                            new EntityEditEntry(nameof(entity.Claims),
                                new Claim("P22", entities.IdFromName(info.Author), BuiltInDataTypes.WikibaseItem))
                        );
                    }
                    entries.Add(
                        new EntityEditEntry(nameof(entity.Claims),
                            new Claim("P25",
                                WbTime.FromDateTime(info.Initiation, WikibaseTimePrecision.Day),
                                BuiltInDataTypes.Time))
                    );
                    await entity.EditAsync(entries, "Populate book information.");
                }
            }
        }

    }
}
