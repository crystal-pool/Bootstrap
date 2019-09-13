using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MwParserFromScratch;
using MwParserFromScratch.Nodes;
using VDS.RDF;
using VDS.RDF.Nodes;
using VDS.RDF.Query;
using WikiClientLibrary;
using WikiClientLibrary.Pages;
using WikiClientLibrary.Sites;
using WikiClientLibrary.Wikibase;
using WikiClientLibrary.Wikibase.DataTypes;
using WikiLink = MwParserFromScratch.Nodes.WikiLink;

namespace EntitySeeding
{
    public class PopulateCats1 : RoutineBase
    {
        /// <inheritdoc />
        public PopulateCats1(WikiSite site, ILoggerFactory loggerFactory) : base(site, loggerFactory)
        {
            zhWarriorsSite = new WikiSite(site.WikiClient, "https://warriors.huijiwiki.com/api.php");
        }

        private readonly WikiSite zhWarriorsSite;
        private const string statusFileName = "PopulateCats1-{0}.json";
        private static readonly WikitextParser parser = new WikitextParser();

        private void WriteMissingEntity(string name)
        {
            File.AppendAllLines("MissingEntities.txt", new[] {name});
        }

        private SparqlResultSet GetCats()
        {
            return CPRepository.ExecuteQuery(@"
                    SELECT ?cat ?title {
                        ?cat wdt:P3 wd:Q622.
                        ?link   schema:isPartOf <https://warriors.huijiwiki.com/>;
                                schema:about ?cat;
                                schema:name ?title.
                    }");
        }

        private IEnumerable<(string Id, string Title, WikiPage ZhPage)> GetCatsToProcess(ICollection<string> processed)
        {
            var cats = GetCats();
            return cats.Select(c => (id: CPRepository.StripEntityUri(((IUriNode)c.Value("cat")).Uri), title: c.Value("title").AsValuedNode().AsString()))
                .Where(t => !processed.Contains(t.id))
                .Select(t => (t.id, t.title, page: new WikiPage(zhWarriorsSite, t.title)));
        }

        private ISet<string> GetProcessedEntities([CallerMemberName] string subName = null)
        {
            var fn = string.Format(statusFileName, subName);
            if (File.Exists(fn))
            {
                return Utility.ReadJsonFrom<HashSet<string>>(fn);
            }
            else
            {
                return new HashSet<string>();
            }
        }

        private void WriteProcessedEntities(IEnumerable<string> processedEntities, [CallerMemberName] string subName = null)
        {
            var fn = string.Format(statusFileName, subName);
            Utility.WriteJsonTo(fn, processedEntities);
        }


        public async Task PopulateRelationsAsync()
        {
            var processedEntities = GetProcessedEntities();
            await zhWarriorsSite.Initialization;
            var counter = 0;
            foreach (var catg in GetCatsToProcess(processedEntities).Buffer(50))
            {
                await catg.Select(t => t.ZhPage).RefreshAsync(PageQueryOptions.FetchContent);
                foreach (var (id, title, page) in catg)
                {
                    counter++;
                    Logger.LogInformation("[{}] Processing {} -> {}", counter, title, id);
                    try
                    {
                        await EditEntityAsync(new Entity(Site, id), page);
                        processedEntities.Add(id);
                    }
                    catch (KeyNotFoundException)
                    {
                        Logger.LogWarning("Missing entity.");
                    }
                    WriteProcessedEntities(processedEntities);
                }
            }

            async Task EditEntityAsync(Entity entity, WikiPage page)
            {
                var root = parser.Parse(page.Content);
                var infobox = root.EnumDescendants().TemplatesWithTitle("Infobox cat").FirstOrDefault();
                if (infobox == null)
                {
                    Logger.LogError("No {{Infobox cat}} found.");
                    return;
                }
                var father = infobox.Arguments["father"]?.Value.EnumDescendants().OfType<WikiLink>().FirstOrDefault()?.Target.ToPlainText();
                var mother = infobox.Arguments["mother"]?.Value.EnumDescendants().OfType<WikiLink>().FirstOrDefault()?.Target.ToPlainText();
                var mates = infobox.Arguments["mate"]?.Value.EnumDescendants().OfType<WikiLink>().Select(l => l.Target.ToPlainText()).ToList();
                var fosters = infobox.Arguments["foster_father"]?.Value.EnumDescendants()
                    .Concat(infobox.Arguments["foster_mother"]?.Value.EnumDescendants() ?? Enumerable.Empty<Node>())
                    .OfType<WikiLink>().Select(l => l.Target.ToPlainText()).ToList();
                var mentors = infobox.Arguments["mentor"]?.Value.EnumDescendants().OfType<WikiLink>().Select(l => l.Target.ToPlainText()).ToList();

                Console.WriteLine(father);
                Console.WriteLine(mother);
                Console.WriteLine(string.Join(";", mates));
                Console.WriteLine(string.Join(";", fosters));
                Console.WriteLine(string.Join(";", mentors));

                var claims = new List<Claim>();
                if (father != null)
                {
                    var f = CPRepository.EntityFromZhSiteLink(father);
                    if (f == null)
                    {
                        WriteMissingEntity(father);
                        throw new KeyNotFoundException();
                    }
                    claims.Add(new Claim("P88", f, BuiltInDataTypes.WikibaseItem));
                }
                if (mother != null)
                {
                    var m = CPRepository.EntityFromZhSiteLink(mother);
                    if (m == null)
                    {
                        WriteMissingEntity(mother);
                        throw new KeyNotFoundException();
                    }
                    claims.Add(new Claim("P89", m, BuiltInDataTypes.WikibaseItem));
                }
                if (fosters != null)
                {
                    foreach (var foster in fosters)
                    {
                        var f = CPRepository.EntityFromZhSiteLink(foster);
                        if (f == null)
                        {
                            WriteMissingEntity(foster);
                            throw new KeyNotFoundException();
                        }
                        claims.Add(new Claim("P99", f, BuiltInDataTypes.WikibaseItem));
                    }
                }
                if (mates != null)
                {
                    var index = 1;
                    foreach (var mate in mates)
                    {
                        var f = CPRepository.EntityFromZhSiteLink(mate);
                        if (f == null)
                        {
                            WriteMissingEntity(mate);
                            throw new KeyNotFoundException();
                        }
                        claims.Add(new Claim("P100", f, BuiltInDataTypes.WikibaseItem)
                        {
                            Qualifiers = {new Snak("P53", index.ToString(), BuiltInDataTypes.String)}
                        });
                        index++;
                    }
                }
                if (mentors != null)
                {
                    foreach (var mentor in mentors)
                    {
                        var f = CPRepository.EntityFromZhSiteLink(mentor);
                        if (f == null)
                        {
                            WriteMissingEntity(mentor);
                            throw new KeyNotFoundException();
                        }
                        claims.Add(new Claim("P86", f, BuiltInDataTypes.WikibaseItem));
                    }
                }
                if (claims.Any())
                {
                    await entity.EditAsync(claims.Select(c => new EntityEditEntry(nameof(entity.Claims), c)),
                        "Populate relations from zhwarriorswiki.", EntityEditOptions.Bot);
                }
            }
        }

        private ConcurrentDictionary<string, string> bookLocationCacheDict = new ConcurrentDictionary<string, string>();

        private IList<(string Name, string Book, string Location)> ExtractAffiliations(Node root)
        {
            string lastLink = null;
            var list = new List<(string, string, string)>();

            void PushLastLink(string b, string l)
            {
                if (lastLink != null)
                {
                    list.Add((lastLink, b, l));
                    lastLink = null;
                }
            }

            foreach (var node in root.EnumDescendants())
            {
                if (node is WikiLink link)
                {
                    PushLastLink(null, null);
                    lastLink = link.Target.ToString().Trim();
                } else if (node is Template template && MwParserUtility.NormalizeTitle(template.Name) == "R")
                {
                    if (lastLink != null)
                    {
                        var b = template.Arguments[1].ToString().Trim();
                        var l = template.Arguments[2]?.ToString().Trim();
                        PushLastLink(b, l);
                    }
                }
            }
            return list;
        }

        public async Task PopulateAffiliationsAsync()
        {
            var processedEntities = GetProcessedEntities();
            await zhWarriorsSite.Initialization;
            var counter = 0;
            foreach (var catg in GetCatsToProcess(processedEntities).Buffer(50))
            {
                await catg.Select(t => t.ZhPage).RefreshAsync(PageQueryOptions.FetchContent);
                foreach (var (id, title, page) in catg)
                {
                    counter++;
                    Logger.LogInformation("[{}] Processing {} -> {}", counter, title, id);
                    try
                    {
                        await EditEntityAsync(new Entity(Site, id), page);
                        processedEntities.Add(id);
                    }
                    catch (KeyNotFoundException)
                    {
                        Logger.LogWarning("Missing entity.");
                    }
                    WriteProcessedEntities(processedEntities);
                }

                async Task EditEntityAsync(Entity entity, WikiPage page)
                {
                    var root = parser.Parse(page.Content);
                    var infobox = root.EnumDescendants().TemplatesWithTitle("Infobox cat").FirstOrDefault();
                    if (infobox == null)
                    {
                        Logger.LogError("No {{Infobox cat}} found.");
                        return;
                    }
                    var pastAff = await ExtractAffiliationsEx(infobox.Arguments["past_affiliation"]?.Value);
                    var curAff = await ExtractAffiliationsEx(infobox.Arguments["current_affiliation"]?.Value);
                    var claims = new List<Claim>();
                    foreach (var (AffId, PosId) in pastAff.Concat(curAff))
                    {
                        Claim c;
                        if (AffId != null)
                            c = new Claim("P76", AffId, BuiltInDataTypes.WikibaseItem);
                        else
                            c = new Claim(new Snak("P76", SnakType.SomeValue));
                        if (PosId != null)
                            c.Qualifiers.Add(new Snak("P92", PosId, BuiltInDataTypes.WikibaseItem));
                        Logger.LogInformation("Affiliation: {}, Pos: {}", CPRepository.LabelFromEntity(AffId, "en"),
                            PosId == null ? null : CPRepository.LabelFromEntity(PosId, "en"));
                        claims.Add(c);
                    }
                    if (claims.Any())
                    {
                        await entity.EditAsync(claims.Select(c => new EntityEditEntry(nameof(entity.Claims), c)),
                            "Populate affiliations from zhwarriorswiki.", EntityEditOptions.Bot);
                    }

                    async Task<IList<(string AffId, string PosId)>> ExtractAffiliationsEx(Node afNode)
                    {
                        var rawAffiliations = afNode == null ? null : ExtractAffiliations(afNode);
                        if (rawAffiliations == null || rawAffiliations.Count == 0)
                            return new List<(string Name, string Position)>();

                        async Task<(string, string)> SubTask(string aff, string book, string location)
                        {
                            var affid = CPRepository.EntityFromZhSiteLink(aff) ?? CPRepository.EntityFromLabel(aff);
                            if (location != null && location.EndsWith("章"))
                            {
                                var pos = (await Site.SearchItemsAsync(book + "-" + location)).FirstOrDefault();
                                if (pos != null) return (affid, pos);
                            }
                            if (book != null)
                            {
                                if (!bookLocationCacheDict.TryGetValue(book, out var pos))
                                {
                                    pos = (await Site.SearchItemsAsync(book)).FirstOrDefault();
                                    bookLocationCacheDict.TryAdd(book, pos);
                                }
                                if (pos != null) return (affid, pos);
                            }
                            return (affid, null);
                        }

                        var processed = await Task.WhenAll(rawAffiliations.Select(aff => SubTask(aff.Name, aff.Book, aff.Location)));
                        return processed;
                    }
                }
            }
        }

    }
}
