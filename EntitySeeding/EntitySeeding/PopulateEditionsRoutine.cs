using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using EntitySeeding.Contracts;
using Microsoft.Extensions.Logging;
using VDS.RDF;
using WikiClientLibrary.Generators;
using WikiClientLibrary.Sites;
using WikiClientLibrary.Wikibase;
using WikiClientLibrary.Wikibase.DataTypes;

namespace EntitySeeding
{
    public class PopulateEditionsRoutine : RoutineBase
    {

        private static readonly IDictionary<string, (string lncn, string lnen)> langDict
            = new Dictionary<string, (string lncn, string lnen)>
            {
                {"en", ("英语", "English-language")},
                {"zh-cn", ("简体中文", "Simplified Chinese")},
                {"zh-tw", ("繁体中文", "Traditional Chinese")},
            };

        public string FileName { get; set; }

        public string LanguageNameCn { get; set; }

        public string LanguageNameEn { get; set; }

        public string LanguageCode { get; set; }

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

        /// <inheritdoc />
        public PopulateEditionsRoutine(WikiSite site, ILoggerFactory loggerFactory, string lang) : base(site, loggerFactory)
        {
            var info = langDict[lang];
            FileName = string.Format("Editions.{0}.json", lang);
            LanguageCode = lang;
            LanguageNameCn = info.lncn;
            LanguageNameEn = info.lnen;
        }

        private static IEnumerable<string> ParseList(string expr)
        {
            if (string.IsNullOrEmpty(expr)) return Enumerable.Empty<string>();
            return expr.Split(',', '，').Select(x => x.Trim());
        }

        public void PrintEntities()
        {
            var editionsRoot = Utility.ReadJsonFrom<IDictionary<string, ICollection<Edition>>>(FileName);
            foreach (var e in editionsRoot.Values.SelectMany(es => es)) e.TidyFields();
            var labels = new HashSet<string>();
            foreach (var edition in editionsRoot.Values.SelectMany(e => e))
            {
                foreach (var i in ParseList(edition.Illustrator)) labels.Add(i);
                labels.Add(edition.Publisher ?? "");
                labels.AddRange(ParseList(edition.Translator));
                labels.Add(edition.Narrator ?? "");
                labels.Add(edition.MediaType ?? "");
            }
            foreach (var l in editionsRoot.Keys) labels.Add(l);
            labels.Remove("");
            foreach (var l in labels.ToArray())
            {
                try
                {
                    if (CPRepository.EntityFromLabel(l) != null) labels.Remove(l);
                }
                catch (AmbiguousMatchException)
                {
                    labels.Remove(l);
                }
            }
            foreach (var l in labels)
            {
                Console.WriteLine(l);
            }
        }

        private static readonly string[] ArcItems = {"Q50", "Q52", "Q53", "Q54", "Q55", "Q56"};

        private string DescribeEditionCn(Edition edition, string bookId)
        {
            var mt = CPRepository.LabelFromEntity(CPRepository.EntityFromLabel(edition.MediaType), "zh-cn");
            if (ArcItems.Contains(bookId)) mt += "套装";
            var book = CPRepository.LabelFromEntity(bookId, "zh-cn");
            string prep = null;
            if (edition.CoverImg?.Area?.Trim() == "英国") prep = "英国发行的";
            var date = edition.TryParsePubDate();
            if (date == null)
                return $"{prep}{LanguageNameCn}版本{mt}《{book}》第{OrdinalZh[edition.EditionNumber - 1]}版";
            return $"{date.Value.Year}年出版的{prep}{LanguageNameCn}版本{mt}《{book}》";
        }

        private string DescribeEditionEn(Edition edition, string bookId)
        {
            var mt = CPRepository.LabelFromEntity(CPRepository.EntityFromLabel(edition.MediaType), "en");
            if (ArcItems.Contains(bookId)) mt += " box set";
            else mt += " edition";
            var book = CPRepository.LabelFromEntity(bookId, "en");
            string prep = null;
            if (edition.CoverImg?.Area?.Trim() == "英国") prep = " released in U.K.";
            var date = edition.TryParsePubDate();
            if (date == null)
                return $"{OrdinalEn[edition.EditionNumber - 1]} {LanguageNameEn} {mt} of {book}{prep}";
            return $"{LanguageNameEn} {mt} of {book}{prep} released in {date.Value.Year}";
        }

        private static readonly Regex TidyExpr = new Regex(@"\s+");

        private static string TidyTitle(string expr)
        {
            return TidyExpr.Replace(expr, " ").Trim();
        }

        public async Task RunAsync()
        {
            var editionsRoot = Utility.ReadJsonFrom<IDictionary<string, ICollection<Edition>>>(FileName);
            var insertedIsbn = new HashSet<string>();
            foreach (var bookEditions in editionsRoot)
            {
                var bookId = CPRepository.RequireEntityFromLabel(bookEditions.Key, "Q46", "Q116", "Q48");
                Logger.LogInformation("Book {} ({}).", bookEditions.Key, bookId);
                foreach (var edition in bookEditions.Value)
                {
                    edition.TidyFields();
                    if (edition.Isbn == null)
                    {
                        Logger.LogInformation("Skipped edition without ISBN: {}.", edition.Title);
                        continue;
                    }
                    if (insertedIsbn.Contains(edition.Isbn))
                    {
                        Logger.LogInformation("Skipped appended edition {}.", edition.Isbn);
                        continue;
                    }
                    if (CPRepository.EntityFromIsbn(edition.Isbn) != null)
                    {
                        Logger.LogInformation("Skipped existing edition {}.", edition.Isbn);
                        continue;
                    }
                    Logger.LogInformation("ISBN: {}", edition.Isbn);
                    var entity = new Entity(Site, EntityType.Item);
                    var claims = new List<Claim>
                    {
                        new Claim("P3", "Q47", BuiltInDataTypes.WikibaseItem),
                        new Claim("P52", bookId, BuiltInDataTypes.WikibaseItem),
                        new Claim("P44", new WbMonolingualText(LanguageCode, edition.Title), BuiltInDataTypes.MonolingualText),
                        new Claim("P57", edition.EditionNumber.ToString(), BuiltInDataTypes.String),
                        new Claim("P59", CPRepository.RequireEntityFromLabel(edition.Publisher), BuiltInDataTypes.WikibaseItem),
                        new Claim("P22", "Q38", BuiltInDataTypes.WikibaseItem)
                    };
                    foreach (var tr in ParseList(edition.Translator))
                    {
                        claims.Add(new Claim("P56", CPRepository.RequireEntityFromLabel(tr), BuiltInDataTypes.WikibaseItem));
                    }
                    foreach (var ill in ParseList(edition.Illustrator))
                    {
                        claims.Add(new Claim("P72", CPRepository.RequireEntityFromLabel(ill), BuiltInDataTypes.WikibaseItem));
                    }
                    var pubDate = edition.TryParsePubDate();
                    if (pubDate != null)
                    {
                        claims.Add(new Claim("P60", pubDate.Value, BuiltInDataTypes.Time));
                    }
                    claims.Add(new Claim(new Snak("P65", SnakType.SomeValue)));
                    claims.Add(new Claim(new Snak("P66", SnakType.SomeValue)));
                    var claim = new Claim("P68", edition.Isbn, BuiltInDataTypes.ExternalId);
                    claim.References.AddRange(edition.EnumRefs().Select(r => new ClaimReference(
                        new Snak("P33", r.Url, BuiltInDataTypes.Url),
                        new Snak("P44", new WbMonolingualText(LanguageCode, TidyTitle(r.Title)), BuiltInDataTypes.MonolingualText),
                        new Snak("P32", WbTime.FromDateTime(DateTime.UtcNow.Date, WikibaseTimePrecision.Day), BuiltInDataTypes.Time)
                    )));
                    claims.Add(claim);
                    claims.Add(new Claim("P69", CPRepository.RequireEntityFromLabel(edition.MediaType), BuiltInDataTypes.WikibaseItem));
                    if (ArcItems.Contains(bookId))
                        claims.Add(new Claim("P69", "Q465", BuiltInDataTypes.WikibaseItem));
                    claims.Add(new Claim("P46", CPRepository.RequireEntityFromLabel(LanguageNameCn), BuiltInDataTypes.WikibaseItem));
                    var changes = new List<EntityEditEntry>
                    {
                        new EntityEditEntry(nameof(entity.Labels), new WbMonolingualText(LanguageCode, edition.Title)),
                        new EntityEditEntry(nameof(entity.Descriptions), new WbMonolingualText("zh-cn", DescribeEditionCn(edition, bookId))),
                        new EntityEditEntry(nameof(entity.Descriptions), new WbMonolingualText("en", DescribeEditionEn(edition, bookId))),
                    };
                    changes.AddRange(claims.Select(c => new EntityEditEntry(nameof(entity.Claims), c)));
                    await entity.EditAsync(changes, $"Populate entity for {LanguageCode} edition of {bookEditions.Key}.", EntityEditOptions.Bot);
                    insertedIsbn.Add(edition.Isbn);
                }
            }
        }

    }
}
