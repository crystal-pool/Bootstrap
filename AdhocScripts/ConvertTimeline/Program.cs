using System;
using System.Threading.Tasks;
using WikiClientLibrary.Client;
using WikiClientLibrary.Sites;
using WikiClientLibrary.Scribunto;
using Newtonsoft.Json.Linq;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using ConvertTimeline.Contracts;

namespace ConvertTimeline
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var entityLookup = File.ReadLines("BookEntities.txt")
                                .Where(l => !string.IsNullOrWhiteSpace(l))
                                .Select(l => l.Split('\t'))
                                .ToDictionary(f => f[1], f => f[0]);
            using (var client = new WikiClient())
            {
                var site = new WikiSite(client, "https://warriors.huijiwiki.com/api.php");
                await site.Initialization;
                var rawData = await site.ScribuntoLoadDataAsync<IDictionary<string, BookEntry>>("Module:Timeline/bookData");
                var output = new List<string>();
                output.Add("{");
                foreach (var p in rawData) {
                    var entityName = entityLookup[p.Value.BookName];
                    output.Add($"{entityName} = \"{p.Key}\",");
                }
                output.Add("}");
                File.WriteAllLines("output.lua", output);
            }
        }
    }
}
