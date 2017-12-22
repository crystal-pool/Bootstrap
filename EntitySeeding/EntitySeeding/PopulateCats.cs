using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using HtmlAgilityPack;
using Microsoft.Extensions.Logging;
using MwParserFromScratch;
using MwParserFromScratch.Nodes;
using VDS.RDF;
using WikiClientLibrary;
using WikiClientLibrary.Generators;
using WikiClientLibrary.Pages;
using WikiClientLibrary.Pages.Parsing;
using WikiClientLibrary.Sites;
using WikiClientLibrary.Wikibase;
using WikiClientLibrary.Wikibase.DataTypes;
using WikiLink = MwParserFromScratch.Nodes.WikiLink;

namespace EntitySeeding
{
    public class PopulateCats : RoutineBase
    {
        /// <inheritdoc />
        public PopulateCats(WikiSite site, ILoggerFactory loggerFactory) : base(site, loggerFactory)
        {
            site.ModificationThrottler.ThrottleTime = TimeSpan.FromSeconds(1);
            zhWarriorsSite = new WikiSite(site.WikiClient, "https://warriors.huijiwiki.com/api.php");
        }

        private readonly WikiSite zhWarriorsSite;

        public async Task RunAsync()
        {
            await zhWarriorsSite.Initialization;
            var gen = new CategoryMembersGenerator(zhWarriorsSite, "猫物")
            {
                PaginationSize = 50,
                MemberTypes = CategoryMemberTypes.Page,
            };
            var counter = 0;
            using (var ie = gen.EnumPagesAsync(PageQueryOptions.FetchContent).GetEnumerator())
            {
                while (await ie.MoveNext())
                {
                    counter++;
                    var page = ie.Current;
                    var query = CPRepository.CreateQuery(@"
                    SELECT ?link {
                        ?link   schema:isPartOf <https://warriors.huijiwiki.com/>;
                                schema:name @title.
                    }");
                    query.SetLiteral("title", page.Title, "zh");
                    if (CPRepository.ExecuteQuery(query).Any())
                    {
                        Logger.LogWarning("Exists {}", page);
                        continue;
                    }
                    Logger.LogInformation("[{}] Processing {}", counter, page);
                    RETRY:
                    try
                    {
                        await ExportEntityAsync(page);
                    }
                    catch (WikiClientException ex)
                    {
                        Console.WriteLine(ex);
                        Console.ReadKey();
                        await page.RefreshAsync(PageQueryOptions.FetchContent);
                        goto RETRY;
                    }
                }
            }
        }

        private static readonly WikitextParser parser = new WikitextParser();

        // genderMode: [0] xxx tom; [1] male xxx; [2] a tom from xxx
        private static readonly Dictionary<string, (string en, string tw, int score, int genderMode)> ProminentClans =
            new Dictionary<string, (string, string, int, int)>
            {
                {"雷族", ("ThunderClan", "雷族", 10, 0)},
                {"影族", ("ShadowClan", "影族", 10, 0)},
                {"风族", ("WindClan", "風族", 10, 0)},
                {"河族", ("RiverClan", "河族", 10, 0)},
                {"天族", ("SkyClan", "天族", 10, 0)},
                {"星族", ("StarClan", "星族", 1, 0)},
                {"黑森林", ("Dark Forest", "黑森林", 1, 2)},
                {"宠物猫", ("kittypet", "寵物貓", 5, 1)},
                {"独行猫", ("loner", "獨行貓", 4, 1)},
                {"泼皮猫", ("rogue", "潑皮貓", 3, 1)},
                {"山地猫", ("Ancient Tribe", "遠古部落", 10, 2)},
                {"远古部落", ("Ancient Tribe", "遠古部落", 10, 2)},
                {"急水部落", ("Tribe of Rushing Water", "急水部落", 10, 2)},
            };

        private static (string cn, string en, string tw) GenerateDescriptions(IEnumerable<string> clans, bool? isMale)
        {
            string formatGender(string m, string f, string uk)
            {
                if (isMale == true) return m;
                if (isMale == false) return f;
                return uk;
            }
            string matchingClan = null;
            var clanInfo = default((string en, string tw, int score, int genderMode));
            foreach (var c in clans)
            {
                if (!ProminentClans.TryGetValue(c, out var cl)) continue;
                if (cl.score > clanInfo.score)
                {
                    matchingClan = c;
                    clanInfo = cl;
                }
            }
            if (matchingClan == null) return (null, null, null);
            if (clanInfo.en == "Ancient Tribe") matchingClan = "远古部落";
            switch (clanInfo.genderMode)
            {
                case 0:
                    return (matchingClan + formatGender("公猫", "母猫", "，性别未知"),
                        clanInfo.en + formatGender(" tom", " she-cat", ", gender unknown"),
                        null);
                case 1:
                    return (formatGender("雄性", "雌性", "未知性别") + matchingClan,
                        formatGender("male ", "female ", "gender-unknown ") + clanInfo.en,
                        formatGender("公", "母", "未知性別") + clanInfo.tw);
                case 2:
                    return (formatGender("公猫，来自", "母猫，来自", "性别未知，来自") + matchingClan, 
                        formatGender("a tom from ", " a she-cat from ", "gender unknown, comes from ") + clanInfo.en,
                        null);
                default:
                    throw new InvalidOperationException();
            }
        }

        private async Task ExportEntityAsync(WikiPage page)
        {
            var root = parser.Parse(page.Content);
            var langLinks = root.EnumDescendants().OfType<WikiLink>().Select(l => l.Target.ToString().Trim())
                .Where(l => !l.StartsWith(":") && l.Contains(":")).Select(l => WikiClientLibrary.WikiLink.Parse(zhWarriorsSite, l))
                .Where(l => l.InterwikiPrefix?.Length == 2).Select(l => (lang: l.InterwikiPrefix, title: l.Title))
                .ToList();
            var infobox = root.EnumDescendants().TemplatesWithTitle("Infobox cat").FirstOrDefault();
            if (infobox == null)
            {
                Logger.LogError("No {{Infobox cat}} found.");
                return;
            }
            var nameNode = infobox.Arguments["name"].Value.EnumDescendants().TemplatesWithTitle("Locale")
                .ToDictionary(n => n.Arguments[1].Value.ToString().Trim().ToLowerInvariant(),
                    n => n.Arguments[2].Value.ToPlainText(NodePlainTextOptions.RemoveRefTags).Replace("-{", "").Replace("}-", "").Trim());
            var chLabels = new WbMonolingualTextCollection();
            var chAliases = new WbMonolingualTextsCollection();
            var chDescriptions = new WbMonolingualTextCollection();

            {
                if (nameNode.TryGetValue("cn", out var l)) chLabels["zh-cn"] = l;
            }
            if (nameNode.TryGetValue("tw", out var ltw)) chLabels["zh-tw"] = ltw;
            {
                string l;
                if (nameNode.TryGetValue("en", out l) || nameNode.TryGetValue("us", out l) || nameNode.TryGetValue("uk", out l))
                    chLabels["en"] = l;
            }
            foreach (var arg in infobox.Arguments)
            {
                if (!arg.Name.ToString().Trim().EndsWith("_name")) continue;
                foreach (var name in arg.Value.EnumDescendants().TemplatesWithTitle("Engname"))
                {
                    var cn = name.Arguments[1].Value.ToPlainText(NodePlainTextOptions.RemoveRefTags).Trim();
                    var en = name.Arguments[2].Value.ToPlainText(NodePlainTextOptions.RemoveRefTags).Trim();
                    string tw = null;
                    var lt = name.Arguments[1].Value.EnumDescendants().TemplatesWithTitle("LT").FirstOrDefault();
                    var diffVer = name.Arguments[1].Value.EnumDescendants().TemplatesWithTitle("DiffVer").FirstOrDefault();
                    if (lt != null)
                    {
                        var parsed = await Task.WhenAll(
                            zhWarriorsSite.ParseContentAsync(lt.ToString(), null, null, null, "zh-cn",
                                ParsingOptions.DisableLimitReport, CancellationToken.None),
                            zhWarriorsSite.ParseContentAsync(lt.ToString(), null, null, null, "zh-tw",
                                ParsingOptions.DisableLimitReport, CancellationToken.None));
                        var doc = new HtmlDocument();
                        doc.LoadHtml(parsed[0].Content);
                        cn = doc.DocumentNode.InnerText.Trim();
                        doc.LoadHtml(parsed[1].Content);
                        tw = doc.DocumentNode.InnerText.Trim();
                    } else if (diffVer != null)
                    {
                        cn = diffVer.Arguments["cn"].Value.ToPlainText(NodePlainTextOptions.RemoveRefTags).Trim();
                        tw = diffVer.Arguments["tw"].Value.ToPlainText(NodePlainTextOptions.RemoveRefTags).Trim();
                    }
                    else
                    {
                        if (ltw != null)
                        {
                            if (cn.EndsWith("爪")) tw = ltw.Substring(0, ltw.Length - 1) + "掌";
                        }
                    }
                    if (cn != null) chAliases.Add("zh-cn", cn);
                    if (en != null) chAliases.Add("en", en);
                    if (tw != null) chAliases.Add("zh-tw", tw);
                }
            }

            var curaff = infobox.Arguments["current_affiliation"].EnumDescendants().OfType<WikiLink>().Select(l => l.Target.ToPlainText().Trim());
            var pastaff = infobox.Arguments["past_affiliation"].EnumDescendants().OfType<WikiLink>().Select(l => l.Target.ToPlainText().Trim());
            bool? gender = null;
            var intro = root.ExtractIntro();
            if (intro != null)
            {
                if (intro.Contains("雄性") || intro.Contains("公")) gender = true;
                else if (intro.Contains("雌性") || intro.Contains("母")) gender = false;
            }
            var desc = GenerateDescriptions(curaff.Concat(pastaff), gender);
            chDescriptions["en"] = desc.en;
            chDescriptions["zh-cn"] = desc.cn;
            chDescriptions["zh-tw"] = desc.tw;
            if (desc.cn == null)
            {
                Logger.LogWarning("No description generated.");
            }
            foreach (var l in langLinks)
            {
                if (l.lang != "zh" && l.lang != "en")
                {
                    var title = l.title.Split('(', 2)[0].Trim();
                    chLabels[l.lang] = title;
                }
            }
            //foreach (var l in chLabels) Console.WriteLine(l);
            //foreach (var l in chAliases) Console.WriteLine(l);
            //foreach (var l in chDescriptions) Console.WriteLine(l);
            //Console.WriteLine("---------------");
            var claims = new List<Claim>
            {
                new Claim("P3", "Q622", BuiltInDataTypes.WikibaseItem),
            };
            if (gender != null)
            {
                claims.Add(new Claim("P78", gender.Value ? "Q678" : "Q679", BuiltInDataTypes.WikibaseItem));
            }
            var chEntity = new Entity(Site, EntityType.Item);
            var edits = new List<EntityEditEntry>();
            edits.AddRange(chLabels.Select(l => new EntityEditEntry(nameof(chEntity.Labels), l)));
            edits.AddRange(chAliases.Select(l => new EntityEditEntry(nameof(chEntity.Aliases), l)));
            edits.AddRange(chDescriptions.Select(l => new EntityEditEntry(nameof(chEntity.Descriptions), l)));
            edits.AddRange(claims.Select(c => new EntityEditEntry(nameof(chEntity.Claims), c)));
            edits.Add(new EntityEditEntry(nameof(chEntity.SiteLinks), new EntitySiteLink("zhwarriorswiki", page.Title)));
            edits.AddRange(langLinks.Select(l => new EntityEditEntry(nameof(chEntity.SiteLinks), new EntitySiteLink(l.lang + "warriorswiki", l.title))));
            await chEntity.EditAsync(edits, "Populate cat from zhwarriorswiki.", EntityEditOptions.Bulk | EntityEditOptions.Bot);
        }

    }
}
