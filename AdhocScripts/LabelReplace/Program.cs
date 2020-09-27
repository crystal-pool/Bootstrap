using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using WikiClientLibrary.Client;
using WikiClientLibrary.Generators;
using WikiClientLibrary.Sites;
using WikiClientLibrary.Wikibase;
using WikiClientLibrary.Wikibase.DataTypes;
using System.Linq;
using System.Diagnostics;

namespace MovingClaimsAsQualifiers
{
    class Program
    {
        static async Task Main(string[] args)
        {
            using var client = new WikiClient();
            var site = new WikiSite(client, "https://crystalpool.cxuesong.com/api.php");
            await site.Initialization;
            site.ModificationThrottler.ThrottleTime = TimeSpan.FromSeconds(1);
            await site.LoginAsync("", "");
            Console.WriteLine("Logged in to {0} as {1}.", site, site.AccountInfo);
            var gen = new BacklinksGenerator(site, "Item:Q171")
            {
                PaginationSize = 50,
                NamespaceIds = new[] { 120 }
            };
            const string replaceFrom = "族群情仇";
            const string replaceTo = "雷影交加";
            await foreach (var batch in gen.EnumItemsAsync().Select(stub => new Entity(site, stub.Title.Split(":")[1])).Buffer(50))
            {
                await batch.RefreshAsync(EntityQueryOptions.FetchLabels | EntityQueryOptions.FetchDescriptions | EntityQueryOptions.FetchAliases,
                    new[] { "en", "zh", "zh-cn", "zh-tw" });
                foreach (var item in batch)
                {
                    Console.WriteLine("Processing {0}.", item);
                    var edits = new List<EntityEditEntry>();
                    foreach (var label in item.Labels)
                    {
                        if (label.Text.Contains(replaceFrom))
                            edits.Add(new EntityEditEntry(nameof(item.Labels), new WbMonolingualText(label.Language, label.Text.Replace(replaceFrom, replaceTo))));
                    }
                    foreach (var desc in item.Descriptions)
                    {
                        if (desc.Text.Contains(replaceFrom))
                            edits.Add(new EntityEditEntry(nameof(item.Descriptions), new WbMonolingualText(desc.Language, desc.Text.Replace(replaceFrom, replaceTo))));
                    }
                    foreach (var alias in item.Aliases)
                    {
                        if (alias.Text.Contains(replaceFrom))
                        {
                            edits.Add(new EntityEditEntry(nameof(item.Aliases), new WbMonolingualText(alias.Language, alias.Text.Replace(replaceFrom, replaceTo))));
                        }
                    }
                    if (edits.Count > 0)
                    {
                        Console.WriteLine("Pending edits:");
                        foreach (var edit in edits)
                        {
                            Console.WriteLine("    {0} {1} {2}", edit.PropertyName, edit.State, edit.Value);
                        }
                        await item.EditAsync(edits, $"Bot: Text replace: {replaceFrom} -> {replaceTo}.", EntityEditOptions.Bot);
                    }
                }
            }
            await site.LogoutAsync();
        }
    }
}
