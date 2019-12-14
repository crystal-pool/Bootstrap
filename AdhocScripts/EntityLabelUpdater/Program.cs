using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Newtonsoft.Json;
using WikiClientLibrary;
using WikiClientLibrary.Client;
using WikiClientLibrary.Generators;
using WikiClientLibrary.Sites;
using WikiClientLibrary.Wikibase;
using WikiClientLibrary.Wikibase.DataTypes;

namespace EntityLabelUpdater
{
    internal static class Program
    {

        public static async Task Main(string[] args)
        {
            var config = JsonConvert.DeserializeObject<Config>(File.ReadAllText("Config._private.json"));
            using var client = new WikiClient();
            var site = new WikiSite(client, config.ApiEndpoint);
            await site.Initialization;
            await site.LoginAsync(config.UserName, config.Password);
            config.Password = "";
            Console.WriteLine("Logged in as {0} on {1}.", site.AccountInfo, site);
            site.ModificationThrottler.ThrottleTime = TimeSpan.FromSeconds(1);
            await UpdateLabels(site);
            await site.LogoutAsync();
        }

        public static string LabelFromTitle(string title)
        {
            return Regex.Replace(title, @"(?!<=^)\s*\(.+", "");
        }

        private static async Task UpdateLabels(WikiSite site)
        {
            var apg = new BacklinksGenerator(site, "Item:Q622")
            {
                NamespaceIds = new[] { site.Namespaces["Item"].Id },
                RedirectsFilter = PropertyFilterOption.WithoutProperty,
                PaginationSize = 100
            };
            var itemCounter = 0;
            await foreach (var stubs in apg.EnumItemsAsync().Buffer(100))
            {
                var items = stubs.Select(s => new Entity(site, WikiLink.Parse(site, s.Title).Title)).ToList();
                await items.RefreshAsync(EntityQueryOptions.FetchLabels | EntityQueryOptions.FetchAliases | EntityQueryOptions.FetchSiteLinks);
                foreach (var item in items)
                {
                    var enLabels = new HashSet<string>();
                    {
                        var enSiteLink = item.SiteLinks.FirstOrDefault(sl => sl.Site == "enwarriorswiki")?.Title;
                        if (!string.IsNullOrEmpty(enSiteLink))
                            enLabels.Add(LabelFromTitle(enSiteLink));
                        enLabels.Add(item.Labels["en"]);
                        enLabels.Add(item.Labels["en-us"]);
                        enLabels.Add(item.Labels["en-gb"]);
                        enLabels.Remove(null);
                    }
                    foreach (var siteLink in item.SiteLinks.Where(sl => sl.Site.EndsWith("warriorswiki")))
                    {
                        var language = siteLink.Site.Substring(0, siteLink.Site.Length - 12);
                        if (language == "zh") 
                            continue;
                        var culture = CultureInfo.GetCultureInfo(language);
                        var label = Regex.Replace(siteLink.Title, @"(?!<=^)\s*\(.+", "");
                        var curLabel = item.Labels[language];
                        if (curLabel == null || label.ToLower(culture) != curLabel.ToLower(culture))
                        {
                            var convertToAlias = !string.IsNullOrWhiteSpace(curLabel) && !enLabels.Contains(curLabel);
                            Console.Write("{0}: {1}: {2} -> {3}", item, language, curLabel, label);
                            var changes = new List<EntityEditEntry>
                            {
                                new EntityEditEntry(nameof(item.Labels), new WbMonolingualText(language, label))
                            };
                            if (convertToAlias)
                            {
                                Console.Write(" (New alias)");
                                changes.Add(new EntityEditEntry(nameof(item.Aliases), new WbMonolingualText(language, curLabel)));
                            }
                            await item.EditAsync(changes, "Update label from " + siteLink.Site + ".", EntityEditOptions.Bot);
                            Console.WriteLine();
                        }
                    }
                }
                itemCounter += items.Count;
                Console.WriteLine("Processed {0} items.", itemCounter);
            }
        }

    }
}
