using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using WikiClientLibrary.Sites;
using WikiClientLibrary.Wikibase;
using WikiClientLibrary.Wikibase.DataTypes;

namespace EntitySeeding
{

    public class JsonVolume
    {
        public string Id { get; set; }
        public string En { get; set; }
        public string Cn { get; set; }
        public string Tw { get; set; }
        public string Abbr { get; set; }
        public string InstanceOf { get; set; } = "Q46";     // Book
    }

    public class JsonArc
    {
        public string Id { get; set; }
        public string En { get; set; }
        public string Cn { get; set; }
        public string CnReferAs { get; set; }
        public string Tw { get; set; }
        public IList<JsonVolume> Volumes { get; set; }
        public bool StandAlone { get; set; }
    }


    public class PopulateBooksRoutine : RoutineBase
    {

        private static readonly string[] OrdinalEn =
        {
            "first", "second", "third", "fourth", "fifth", "sixth", "seventh", "eighth", "ninth",
            "tenth", "eleventh", "twelfth"
        };

        private static readonly string[] OrdinalZh =
        {
            "一", "二", "三", "四", "五", "六", "七", "八", "九",
            "十", "十一", "十二"
        };

        private string BuildArcDescriptionEn()
        {
            return "a part of books of Warriors series";
        }

        private string BuildArcDescriptionCn()
        {
            return "《猫武士》系列的一组书籍";
        }

        private string BuildVolumeDescriptionEn(JsonArc arc, int index)
        {
            return $"the {OrdinalEn[index]} book of {arc.En} arc";
        }

        private string BuildVolumeDescriptionCn(JsonArc arc, int index)
        {
            return $"《猫武士》{arc.CnReferAs ?? arc.Cn}第{OrdinalZh[index]}册";
        }

        const string BooksPath = "Books.json";

        public static IList<JsonArc> LoadBooks()
        {
            return Utility.ReadJsonFrom<IList<JsonArc>>(BooksPath);
        }

        public async Task RunAsync()
        {
            var arcs = LoadBooks();
            for (int i = 0; i < arcs.Count; i++)
            {
                var arc = arcs[i];
                var prevArc = i > 0 ? arcs[i - 1] : null;
                Console.WriteLine(arc.En);
                if (!arc.StandAlone && arc.Id == null)
                {
                    Console.WriteLine("\tPopulating");
                    var entity = new Entity(Site, EntityType.Item);
                    var seriesClaim = new Claim("P50", KnownEntities.Warriors, BuiltInDataTypes.WikibaseItem);
                    if (prevArc != null && !prevArc.StandAlone)
                    {
                        seriesClaim.Qualifiers.Add(new Snak("P48", prevArc.Id, BuiltInDataTypes.WikibaseItem));
                    }
                    await entity.EditAsync(new[]
                    {
                        new EntityEditEntry(nameof(entity.Labels), new WbMonolingualText("en", arc.En)),
                        new EntityEditEntry(nameof(entity.Labels), new WbMonolingualText("zh-cn", arc.Cn)),
                        new EntityEditEntry(nameof(entity.Labels), new WbMonolingualText("zh-tw", arc.Tw)),
                        new EntityEditEntry(nameof(entity.Descriptions), new WbMonolingualText("en", BuildArcDescriptionEn())),
                        new EntityEditEntry(nameof(entity.Descriptions), new WbMonolingualText("zh-cn", BuildArcDescriptionCn())),
                        new EntityEditEntry(nameof(entity.SiteLinks), new EntitySiteLink("zhwarriorswiki", arc.Cn)),
                        new EntityEditEntry(nameof(entity.Claims), new Claim( "P3", "Q48", BuiltInDataTypes.WikibaseItem)),
                        new EntityEditEntry(nameof(entity.Claims), seriesClaim),
                    }, "Create entity for " + arc.En + ".", EntityEditOptions.Bot);
                    arc.Id = entity.Id;
                    Utility.WriteJsonTo(BooksPath, arcs);
                }
                for (int j = 0; j < arc.Volumes.Count; j++)
                {
                    var vol = arc.Volumes[j];
                    var prevVol = j > 0 ? arc.Volumes[j - 1] : null;
                    Console.WriteLine(vol.En);
                    if (vol.Id == null)
                    {
                        Console.WriteLine("\tPopulating");
                        var entity = new Entity(Site, EntityType.Item);
                        var seriesClaim = new Claim("P50",
                            arc.StandAlone ? KnownEntities.Warriors : arc.Id,
                            BuiltInDataTypes.WikibaseItem);
                        seriesClaim.Qualifiers.Add(new Snak("P53", (j + 1).ToString(), BuiltInDataTypes.String));
                        if (prevVol != null)
                        {
                            seriesClaim.Qualifiers.Add(new Snak("P48", prevVol.Id, BuiltInDataTypes.WikibaseItem));
                        }
                        await entity.EditAsync(new[]
                        {
                            new EntityEditEntry(nameof(entity.Labels), new WbMonolingualText("en", vol.En)),
                            new EntityEditEntry(nameof(entity.Labels), new WbMonolingualText("zh-cn", vol.Cn)),
                            new EntityEditEntry(nameof(entity.Labels), new WbMonolingualText("zh-tw", vol.Tw)),
                            new EntityEditEntry(nameof(entity.Descriptions), new WbMonolingualText("en", BuildVolumeDescriptionEn(arc, j))),
                            new EntityEditEntry(nameof(entity.Descriptions), new WbMonolingualText("zh-cn", BuildVolumeDescriptionCn(arc, j))),
                            new EntityEditEntry(nameof(entity.SiteLinks), new EntitySiteLink("zhwarriorswiki", vol.Cn)),
                            new EntityEditEntry(nameof(entity.Claims), new Claim("P3", vol.InstanceOf, BuiltInDataTypes.WikibaseItem)),
                            new EntityEditEntry(nameof(entity.Claims), seriesClaim),
                        }, "Create entity for " + vol.En + ".", EntityEditOptions.Bot);
                        vol.Id = entity.Id;
                        Utility.WriteJsonTo(BooksPath, arcs);
                    }
                    var entity1 = new Entity(Site, vol.Id);
                    await entity1.EditAsync(new[]
                    {
                        new EntityEditEntry(nameof(entity1.Aliases), new WbMonolingualText("en", vol.Abbr))
                    }, "Add en alias.", EntityEditOptions.Bot);
                }
            }
        }

        /// <inheritdoc />
        public PopulateBooksRoutine(WikiSite site, ILoggerFactory loggerFactory) : base(site, loggerFactory)
        {
        }
    }
}
