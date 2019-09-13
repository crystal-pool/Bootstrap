using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using WikiClientLibrary;
using WikiClientLibrary.Generators;
using WikiClientLibrary.Sites;
using WikiClientLibrary.Wikibase;

namespace EntitySeeding
{

    public class EntityInfo
    {

        public string Id { get; set; }

        public string NameEn { get; set; }

        public string NameCn { get; set; }

    }

    public class EntityInfoCollection : Collection<EntityInfo>
    {

        public string IdFromName(string name)
        {
            return (Items.FirstOrDefault(i => i.NameCn == name)
                    ?? Items.FirstOrDefault(i => string.Equals(i.NameEn, name, StringComparison.InvariantCultureIgnoreCase)))?.Id;
        }

    }

    public class GatherEntitiesRoutine : RoutineBase
    {

        public string FileName { get; set; } = "Entities.tsv";

        public async Task RunAsync()
        {
            var apg = new AllPagesGenerator(Site)
            {
                NamespaceId = Site.Namespaces["Item"].Id,
                PaginationSize = 100
            };
            using (var writer = File.CreateText(FileName))
            {
                await foreach (var pages in apg.EnumItemsAsync().BufferAsync(100))
                {
                    var entities = pages.Select(s => new Entity(Site, WikiLink.Parse(Site, s.Title).Title)).ToList();
                    await entities.RefreshAsync(EntityQueryOptions.FetchLabels, new[]
                    {
                        "en", "zh", "zh-cn", "zh-hans"
                    });
                    foreach (var entity in entities)
                    {
                        writer.Write(entity.Id);
                        writer.Write('\t');
                        writer.Write(entity.Labels["en"]);
                        writer.Write('\t');
                        writer.Write(entity.Labels["zh-cn"] ?? entity.Labels["zh-hans"] ?? entity.Labels["zh"]);
                        writer.WriteLine();
                    }
                }
            }
        }

        public static EntityInfoCollection Load(string fileName = "Entities.tsv")
        {
            var collection = new EntityInfoCollection();
            foreach (var line in File.ReadLines(fileName))
            {
                var fields = line.Split('\t');
                collection.Add(new EntityInfo
                {
                    Id = fields[0],
                    NameEn = fields[1],
                    NameCn = fields[2],
                });
            }
            return collection;
        }

        /// <inheritdoc />
        public GatherEntitiesRoutine(WikiSite site, ILoggerFactory loggerFactory) : base(site, loggerFactory)
        {
        }
    }
}
