using System;
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
            await site.LoginAsync("", "");
            Console.WriteLine("Logged in to {0} as {1}.", site, site.AccountInfo);
            var gen = new BacklinksGenerator(site, "Property:P97")
            {
                PaginationSize = 50,
                NamespaceIds = new[] { 120 }
            };
            await foreach (var batch in gen.EnumItemsAsync().Select(stub => new Entity(site, stub.Title.Split(":")[1])).Buffer(50))
            {
                await batch.RefreshAsync(EntityQueryOptions.FetchClaims | EntityQueryOptions.FetchLabels, new[] { "en" });
                foreach (var item in batch)
                {
                    Console.WriteLine("Processing {0}.", item);
                    var mannerClaim = item.Claims["P97"].SingleOrDefault();
                    if (mannerClaim == null) continue;
                    var timeClaim = item.Claims["P96"].SingleOrDefault();
                    if (timeClaim == null)
                    {
                        Console.WriteLine("No death time claim for {0}.", item);
                        continue;
                    }
                    timeClaim.Qualifiers.Add(new Snak("P97", mannerClaim.MainSnak.RawDataValue, BuiltInDataTypes.WikibaseItem));
                    foreach (var q in mannerClaim.Qualifiers) timeClaim.Qualifiers.Add(q);
                    await item.EditAsync(new[] {
                        new EntityEditEntry(nameof(item.Claims), timeClaim),
                        new EntityEditEntry(nameof(item.Claims), mannerClaim, EntityEditEntryState.Removed)
                    }, "Bot: Moving P97 as a qualifier of P96.", EntityEditOptions.Bot);
                }
            }
            await site.LogoutAsync();
        }
    }
}
