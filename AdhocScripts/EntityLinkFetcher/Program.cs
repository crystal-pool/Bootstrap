using System.Text.Json;
using EntityLabelUpdater;
using WikiClientLibrary;
using WikiClientLibrary.Client;
using WikiClientLibrary.Generators;
using WikiClientLibrary.Pages;
using WikiClientLibrary.Pages.Queries;
using WikiClientLibrary.Pages.Queries.Properties;
using WikiClientLibrary.Sites;
using WikiClientLibrary.Wikibase;

namespace EntityLinkFetcher;

internal static class Program
{

    public static async Task Main(string[] args)
    {
        var config = JsonSerializer.Deserialize<Config>(await File.ReadAllTextAsync("Config._private.json"));
        using var client = new WikiClient();
        var site = new WikiSite(client, config.ApiEndpoint);
        await site.Initialization;
        await site.LoginAsync(config.UserName, config.Password);
        config.Password = "";
        Console.WriteLine("Logged in as {0} on {1}.", site.AccountInfo, site);
        site.ModificationThrottler.ThrottleTime = TimeSpan.FromSeconds(1);
        var interwikiSites = new Dictionary<string, WikiSite>();

        async Task<WikiSite> GetExternalSiteAsync(string lang)
        {
            if (interwikiSites.TryGetValue(lang, out var s))
                return s;
            var targetLink = site.InterwikiMap["warriors"].Url;
            targetLink = targetLink.Replace("$1", lang == "en" ? "" : (lang + ":"));
            var endpoint = await WikiSite.SearchApiEndpointAsync(client, targetLink);
            Console.WriteLine("Resolved {0}warriorswiki into {1}.", lang, endpoint);
            s = new WikiSite(client, endpoint);
            await s.Initialization;
            interwikiSites.Add(lang, s);
            return s;
        }
        await FetchLabels(site, GetExternalSiteAsync, "Item:Q116");
        await FetchLabels(site, GetExternalSiteAsync, "Item:Q46");
        await FetchLabels(site, GetExternalSiteAsync, "Item:Q622");
        await site.LogoutAsync();
    }

    private static async Task FetchLabels(WikiSite site, Func<string, Task<WikiSite>> getExternalSiteAsync, string originEntity)
    {
        var apg = new BacklinksGenerator(site, originEntity)
        {
            NamespaceIds = new[] { site.Namespaces["Item"].Id },
            RedirectsFilter = PropertyFilterOption.WithoutProperty,
            PaginationSize = 100
        };
        var itemCounter = 0;
        var enSite = await getExternalSiteAsync("en");
        await foreach (var stubs in apg.EnumItemsAsync().Buffer(100))
        {
            var items = stubs.Select(s => new Entity(site, WikiLink.Parse(site, s.Title).Title)).ToList();
            await items.RefreshAsync(EntityQueryOptions.FetchLabels | EntityQueryOptions.FetchSiteLinks, new List<string> { "en" });
            var enwwPages = items.ToDictionary(i => i, i =>
            {
                var enwwtitle = i.SiteLinks.ContainsKey("enwarriorswiki") ? i.SiteLinks["enwarriorswiki"].Title : null;
                if (string.IsNullOrEmpty(enwwtitle))
                {
                    Console.WriteLine("{0}: No enww sitelink available.", i);
                    return null;
                }
                return new WikiPage(enSite, enwwtitle);
            });
            Console.WriteLine("Fetching language links from enww.");
            await enwwPages.Values.Where(p => p != null).RefreshAsync(new WikiPageQueryProvider { Properties = { new LanguageLinksPropertyProvider() } });
            foreach (var item in items)
            {
                var enPage = enwwPages[item];
                if (enPage == null) continue;
                foreach (var langLink in enPage.GetPropertyGroup<LanguageLinksPropertyGroup>().LanguageLinks
                             // Wikia returns duplicate language links
                             .Select(l => (l.Language, l.Title)).Distinct())
                {
                    if (langLink.Language == "zh") continue;
                    var siteName = WWSiteNameFromLanguageCode(langLink.Language);
                    if (item.SiteLinks.ContainsKey(siteName)) continue;
                    Console.Write("{0}: Add {1}:{2}...", item, siteName, langLink.Title);
                    try
                    {
                        await item.EditAsync(new[]
                            {
                                new EntityEditEntry(nameof(item.SiteLinks),
                                    new EntitySiteLink(siteName, langLink.Title))
                            },
                            "Add sitelink from enwarriorswiki.", EntityEditOptions.Bot);
                        Console.WriteLine("Success.");
                    }
                    catch (OperationFailedException ex) when (ex.ErrorCode == "badvalue")
                    {
                        Console.WriteLine(ex.Message);
                    }
                    catch (OperationFailedException ex) when (ex.ErrorCode == "no-external-page")
                    {
                        Console.WriteLine("No external page.");
                    }
                    catch (OperationFailedException ex) when (ex.ErrorCode == "failed-save")
                    {
                        Console.WriteLine("Save failed.");
                        Console.WriteLine(ex.ErrorMessage);
                    }
                }
            }
            itemCounter += items.Count;
            Console.WriteLine("Processed {0} items.", itemCounter);
        }
    }

    private static string WWSiteNameFromLanguageCode(string languageCode)
    {
        return languageCode.Replace("-", "_") + "warriorswiki";
    }

}