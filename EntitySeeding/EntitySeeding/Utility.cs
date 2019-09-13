using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MwParserFromScratch;
using MwParserFromScratch.Nodes;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using VDS.RDF;
using VDS.RDF.Parsing;
using WikiClientLibrary.Client;
using WikiClientLibrary.Sites;
using WikiClientLibrary.Wikibase.DataTypes;

namespace EntitySeeding
{
    public static class Utility
    {

        public static readonly JsonSerializer jsonSerializer = new JsonSerializer
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver()
        };

        public static T ReadJsonFrom<T>(string path)
        {
            using (var reader = File.OpenText(path))
            using (var jreader = new JsonTextReader(reader))
            {
                return jsonSerializer.Deserialize<T>(jreader);
            }
        }

        public static void WriteJsonTo(string path, object value)
        {
            using (var writer = File.CreateText(path))
            using (var jwriter = new JsonTextWriter(writer))
            {
                jsonSerializer.Serialize(jwriter, value);
            }
        }

        public static void AddRange<T>(this ICollection<T> collection, IEnumerable<T> items)
        {
            foreach (var i in items) collection.Add(i);
        }

        private static readonly string[] OrdinalZh =
        {
            "", "一", "二", "三", "四", "五", "六", "七", "八", "九",
        };

        public static string GetOrdinalZh(int n)
        {
            if (n < 1) throw new ArgumentOutOfRangeException();
            if (n < 10) return OrdinalZh[n];
            if (n < 20) return "十" + OrdinalZh[n - 10];
            if (n < 30) return "二十" + OrdinalZh[n - 20];
            if (n < 40) return "三十" + OrdinalZh[n - 30];
            if (n < 50) return "四十" + OrdinalZh[n - 40];
            throw new ArgumentOutOfRangeException();
        }

        public static IEnumerable<Template> TemplatesWithTitle(this IEnumerable<Node> nodes, string name)
        {
            return nodes.OfType<Template>().Where(t => MwParserUtility.NormalizeTitle(t.Name) == name);
        }

        public static string ExtractIntro(this Wikitext text)
        {
            return text.Lines.TakeWhile(l => !(l is Heading))
                .Select(l => l.ToPlainText(NodePlainTextOptions.RemoveRefTags).Trim())
                .FirstOrDefault(l => !string.IsNullOrEmpty(l));
        }

        public static IEnumerable<ICollection<T>> Buffer<T>(this IEnumerable<T> source, int count)
        {
            var block = new List<T>();
            foreach (var i in source)
            {
                block.Add(i);
                if (block.Count >= count)
                {
                    yield return block;
                    block.Clear();
                }
            }
            if (block.Count >= 0) yield return block;
        }

        public static async Task<IList<string>> SearchItemsAsync(this WikiSite site, string keywords)
        {
            var request = new MediaWikiFormRequestMessage(
                new
                {
                    action = "wbsearchentities",
                    search = keywords,
                    language = "en",
                    type = "item",
                    limit = 5
                });
            var response = await site.InvokeMediaWikiApiAsync(request, CancellationToken.None);
            return response["search"].Select(item => (string)item["id"]).ToList();
        }

        public static async IAsyncEnumerable<IList<T>> BufferAsync<T>(this IAsyncEnumerable<T> sequence, int bufferSize)
        {
            if (sequence == null) throw new ArgumentNullException(nameof(sequence));
            if (bufferSize <= 0) throw new ArgumentOutOfRangeException(nameof(bufferSize));
            var partition = new List<T>();
            await foreach (var item in sequence.ConfigureAwait(false))
            {
                partition.Add(item);
                if (partition.Count == bufferSize)
                {
                    yield return partition;
                    partition = new List<T>();
                }
            }
            if (partition.Count > 0)
                yield return partition;
        }

    }
}
