using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using HtmlAgilityPack;
using Microsoft.Extensions.Logging;
using VDS.RDF;
using WikiClientLibrary.Pages.Parsing;
using WikiClientLibrary.Sites;
using WikiClientLibrary.Wikibase;
using WikiClientLibrary.Wikibase.DataTypes;

namespace EntitySeeding
{
    public class PopulateChapters : RoutineBase
    {
        /// <inheritdoc />
        public PopulateChapters(WikiSite site, ILoggerFactory loggerFactory) : base(site, loggerFactory)
        {
            site.ModificationThrottler.ThrottleTime = TimeSpan.FromSeconds(1);
        }

        private int? TryMatchChapterNumber(string expr)
        {
            var m = Regex.Match(expr, @"Chapter\s*(\d+)", RegexOptions.IgnoreCase);
            if (m.Success) return Convert.ToInt32(m.Groups[1].Value);
            return null;
        }

        private string GetAbbr(string expr)
        {
            var words = expr.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            return new string(words.Select(w => w[0]).ToArray());
        }

        public async Task RunAsync()
        {
            var enWw = new WikiSite(Site.WikiClient, "https://warriors.fandom.com/api.php");
            //            var books = CPRepository.ExecuteQuery(@"
            //SELECT ?book ?link {
            //    { ?book wdt:P3 wd:Q46. } UNION { ?book wdt:P3 wd:Q116. }
            //    ?link   schema:isPartOf <http://warriors.wikia.com/>;
            //            schema:about ?book.
            //}")
            //                .Select(r => (book: (UriNode)r["book"], link: (UriNode)r["link"]))
            //                .ToList();
            var books = CPRepository.ExecuteQuery(@"
SELECT ?book ?label {
{ ?book wdt:P3 wd:Q46. } UNION { ?book wdt:P3 wd:Q116. }
?book rdfs:label ?label. FILTER (lang(?label) = 'en')
}")
                .Select(r => (id: CPRepository.StripEntityUri(((UriNode)r["book"]).Uri), label: ((LiteralNode)r["label"]).Value));
            await enWw.Initialization;
            foreach (var book in books)
            {
                string lastChapterId = null;
                var bookItem = new Entity(Site, book.id);
                var tlabel = book.label;
            RETRY:
                var parsingTask = enWw.ParseContentAsync("{{Chapters/b|" + tlabel + "}}", null, null, ParsingOptions.None);
                await bookItem.RefreshAsync(EntityQueryOptions.FetchLabels | EntityQueryOptions.FetchAliases, new[] { "en", "zh-cn", "zh-tw" });
                var labelEn = bookItem.Labels["en"];
                var labelCn = bookItem.Labels["zh-cn"] ?? bookItem.Labels["zh-hans"] ?? labelEn;
                var labelTw = bookItem.Labels["zh-tw"] ?? bookItem.Labels["zh-hant"] ?? labelCn;
                Logger.LogInformation("{}, {}, {}", labelEn, labelCn, labelTw);
                var doc = new HtmlDocument();
                doc.LoadHtml((await parsingTask).Content);
                var nodes = doc.DocumentNode.SelectNodes("//a[@href|@data-uncrawlable-url]");
                if (nodes == null)
                {
                    if (!tlabel.Contains('('))
                    {
                        tlabel += " (Book)";
                        goto RETRY;
                    }
                    Logger.LogError("No chapter information found.");
                    continue;
                }
                foreach (var node in nodes)
                {
                    var chLabels = new WbMonolingualTextCollection();
                    var chAliases = new WbMonolingualTextsCollection();
                    var chDescriptions = new WbMonolingualTextCollection();
                    var text = node.InnerText.Trim();
                    var n = TryMatchChapterNumber(text);
                    var nId = n?.ToString();
                    if (n == null)
                    {
                        switch (text.ToLowerInvariant())
                        {
                            case "prologue":
                                chLabels["en"] = labelEn + ", Prologue";
                                chDescriptions["en"] = "prologue chapter of " + labelEn;
                                foreach (var a in bookItem.Aliases["en"])
                                {
                                    chAliases.Add("en", a + "-0");
                                }
                                chLabels["zh-cn"] = "《" + labelCn + "》引子";
                                chDescriptions["zh-cn"] = "《" + labelCn + "》的引子章节";
                                chLabels["zh-tw"] = "《" + labelTw + "》序章";
                                chDescriptions["zh-tw"] = "《" + labelTw + "》的序章";
                                chAliases.Add("zh-cn", labelCn + " 引子");
                                chAliases.Add("zh-tw", labelTw + " 序章");
                                chAliases.Add("zh-cn", labelCn + " 0");
                                chAliases.Add("zh-tw", labelTw + " 0");
                                nId = "0";
                                break;
                            case "epilogue":
                                chLabels["en"] = labelEn + ", Epilogue";
                                chDescriptions["en"] = "epilogue chapter of " + labelEn;
                                foreach (var a in bookItem.Aliases["en"])
                                {
                                    chAliases.Add("en", a + "-E");
                                }
                                chLabels["zh-cn"] = "《" + labelCn + "》尾声";
                                chDescriptions["zh-cn"] = "《" + labelCn + "》的尾声章节";
                                chLabels["zh-tw"] = "《" + labelTw + "》尾聲";
                                chDescriptions["zh-tw"] = "《" + labelTw + "》的尾聲章節";
                                chAliases.Add("zh-cn", labelCn + " 尾声");
                                chAliases.Add("zh-tw", labelTw + " 尾聲");
                                nId = "E";
                                break;
                            default:
                                chLabels["en"] = labelEn + ", " + text;
                                chDescriptions["en"] = "a chapter of " + labelEn;
                                var abbr = GetAbbr(text);
                                foreach (var a in bookItem.Aliases["en"])
                                {
                                    chAliases.Add("en", a + "-" + abbr);
                                }
                                chLabels["zh-cn"] = "《" + labelCn + "》" + text;
                                chDescriptions["zh-cn"] = "《" + labelCn + "》的一个章节";
                                chLabels["zh-tw"] = "《" + labelTw + "》" + text;
                                chDescriptions["zh-tw"] = "《" + labelTw + "》的一個章節";
                                chAliases.Add("zh-cn", labelCn + " " + abbr);
                                chAliases.Add("zh-tw", labelTw + " " + abbr);
                                break;
                        }
                    }
                    else
                    {
                        chLabels["en"] = labelEn + ", Chapter " + n;
                        chDescriptions["en"] = "Chapter " + n + " of " + labelEn;
                        foreach (var a in bookItem.Aliases["en"])
                        {
                            chAliases.Add("en", a + "-" + n);
                        }
                        var zhOrdinal = Utility.GetOrdinalZh(n.Value);
                        chLabels["zh-cn"] = "《" + labelCn + "》第" + zhOrdinal + "章";
                        chDescriptions["zh-cn"] = "《" + labelCn + "》的第" + zhOrdinal + "章";
                        chLabels["zh-tw"] = "《" + labelTw + "》第" + zhOrdinal + "章";
                        chDescriptions["zh-tw"] = "《" + labelTw + "》的第" + zhOrdinal + "章";
                        chAliases.Add("zh-cn", labelCn + " " + n);
                        chAliases.Add("zh-tw", labelTw + " " + n);
                    }
                    string cid = null;
                    if ((cid = CPRepository.EntityFromLabel(chLabels["en"])) != null)
                    {
                        // Bypass the whole book if any chapter has been populated.
                        Logger.LogWarning("Entity exists: {Name}", chLabels["en"]);
                        lastChapterId = cid;
                        if (labelEn.Contains("Hollyleaf's Story")) continue;
                        // continue;
                        break;
                    }
                    if (labelEn == labelCn)
                    {
                        chLabels.Remove("zh-cn");
                        chAliases.Remove("zh-cn");
                        chDescriptions.Remove("zh-cn");
                    }
                    if (labelEn == labelTw || labelCn == labelTw)
                    {
                        chLabels.Remove("zh-tw");
                        chAliases.Remove("zh-tw");
                        chDescriptions.Remove("zh-tw");
                    }
                    //foreach (var l in chLabels) Console.WriteLine(l);
                    //foreach (var l in chAliases) Console.WriteLine(l);
                    //foreach (var l in chDescriptions) Console.WriteLine(l);
                    var claims = new List<Claim>
                    {
                        new Claim("P3", "Q109", BuiltInDataTypes.WikibaseItem),
                    };
                    {
                        var c = new Claim("P50", book.id, BuiltInDataTypes.WikibaseItem);
                        if (nId != null)
                            c.Qualifiers.Add(new Snak("P53", nId, BuiltInDataTypes.String));
                        if (lastChapterId != null)
                            c.Qualifiers.Add(new Snak("P48", lastChapterId, BuiltInDataTypes.WikibaseItem));
                        claims.Add(c);
                    }
                    var chEntity = new Entity(Site, EntityType.Item);
                    var edits = new List<EntityEditEntry>();
                    edits.AddRange(chLabels.Select(l => new EntityEditEntry(nameof(chEntity.Labels), l)));
                    edits.AddRange(chAliases.Select(l => new EntityEditEntry(nameof(chEntity.Aliases), l)));
                    edits.AddRange(chDescriptions.Select(l => new EntityEditEntry(nameof(chEntity.Descriptions), l)));
                    edits.AddRange(claims.Select(c => new EntityEditEntry(nameof(chEntity.Claims), c)));
                    if (!node.HasClass("new"))
                    {
                        var title = WebUtility.UrlDecode(node.GetAttributeValue("href", "").Replace("/wiki/", ""));
                        edits.Add(new EntityEditEntry(nameof(chEntity.SiteLinks), new EntitySiteLink("enwarriorswiki", title)));
                    }
                    //try
                    //{
                    await chEntity.EditAsync(edits, "Populate chapter.", EntityEditOptions.Bulk | EntityEditOptions.Bot);
                    lastChapterId = chEntity.Id;
                    //}
                    //catch (OperationFailedException ex) when (ex.ErrorCode == "modification-failed")
                    //{
                    //    var id = Regex.Match(ex.ErrorMessage, @"\[\[Item:(Q\d+)\|Q").Groups[1].Value;
                    //    if (string.IsNullOrEmpty(id)) throw;
                    //    Logger.LogInformation("Entity exists: {Name} as {Entity}", chLabels["en"], id);
                    //    lastChapterId = id;
                    //}
                }
            }
        }

    }
}
