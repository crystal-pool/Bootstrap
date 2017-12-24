﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MwParserFromScratch;
using MwParserFromScratch.Nodes;
using VDS.RDF;
using VDS.RDF.Nodes;
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
        private HashSet<string> processedEntities;
        private const string statusFileName = "PopulateCats1.json";
        private static readonly WikitextParser parser = new WikitextParser();

        private void WriteMissingEntity(string name)
        {
            File.AppendAllLines("MissingEntities.txt", new[] {name});
        }

        public async Task PopulateRelationsAsync()
        {
            if (File.Exists(statusFileName))
            {
                processedEntities = Utility.ReadJsonFrom<HashSet<string>>(statusFileName);
            }
            else
            {
                processedEntities = new HashSet<string>();
            }
            await zhWarriorsSite.Initialization;
            var cats = CPRepository.ExecuteQuery(@"
                    SELECT ?cat ?title {
                        ?cat wdt:P3 wd:Q622.
                        ?link   schema:isPartOf <https://warriors.huijiwiki.com/>;
                                schema:about ?cat;
                                schema:name ?title.
                    }");
            var counter = 0;
            foreach (var catg0 in cats.Buffer(50))
            {
                var catg = catg0.Select(c => (id: CPRepository.StripEntityUri(((IUriNode)c.Value("cat")).Uri), title: c.Value("title").AsValuedNode().AsString()))
                    .Where(t => !processedEntities.Contains(t.id))
                    .Select(t => (t.id, t.title, page: new WikiPage(zhWarriorsSite, t.title)))
                    .ToList();
                counter += catg0.Count - catg.Count;
                if (catg.Count == 0) continue;
                await catg.Select(t => t.page).RefreshAsync(PageQueryOptions.FetchContent);
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
                    Utility.WriteJsonTo(statusFileName, processedEntities);
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


    }
}