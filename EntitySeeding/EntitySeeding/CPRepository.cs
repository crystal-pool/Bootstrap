using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using VDS.RDF;
using VDS.RDF.Parsing;
using VDS.RDF.Query;
using VDS.RDF.Query.Datasets;

namespace EntitySeeding
{
    public static class CPRepository
    {

        private static readonly Lazy<IGraph> _cp = new Lazy<IGraph>(() => LoadGraph());
        private static readonly Lazy<ISparqlDataset> _cpds = new Lazy<ISparqlDataset>(() => new InMemoryDataset(_cp.Value));

        public static IGraph Graph => _cp.Value;

        public static ISparqlDataset Dataset => _cpds.Value;

        public static IGraph LoadGraph(string fileName = "wbdump.ttl")
        {
            var graph = new Graph();
            FileLoader.Load(graph, fileName);
            return graph;
        }

        public static SparqlResultSet ExecuteQuery(SparqlParameterizedString expr)
        {
            return ExecuteQuery(expr.ToString());
        }

        public static SparqlResultSet ExecuteQuery(string expr)
        {
            var parser = new SparqlQueryParser();
            var query = parser.ParseFromString(CreateQuery(expr));
            var proc = new LeviathanQueryProcessor(Dataset);
            return (SparqlResultSet)proc.ProcessQuery(query);
        }

        public static string StripEntityUri(Uri uri)
        {
            return CPNamespaces.Wd.MakeRelativeUri(uri).ToString();
        }

        public static string EntityFromIsbn(string expr)
        {
            if (expr == null) throw new ArgumentNullException(nameof(expr));
            var q = CreateQuery("SELECT ?i WHERE { ?i wdt:P68 ?ri. FILTER (@isbn = replace(?ri, '-', '')). } LIMIT 1");
            q.SetLiteral("isbn", expr.Replace("-", ""));
            var results = ExecuteQuery(q);
            var result = (IUriNode)results.Results.FirstOrDefault()?.Value("i");
            if (result == null) return null;
            return StripEntityUri(result.Uri);
        }

        public static string EntityFromLabel(string expr, string instanceOf = null)
        {
            if (instanceOf != null)
            {
                instanceOf = "?i wdt:P3 wd:" + instanceOf + ".";
            }
            var q = CreateQuery("SELECT DISTINCT ?i WHERE { ?i rdfs:label ?l. " + instanceOf + " FILTER (ucase(str(?l)) = ucase(@label)) } LIMIT 2");
            q.SetLiteral("label", expr);
            var results = ExecuteQuery(q);
            if (results.Count > 1)
                throw new AmbiguousMatchException("Ambiguous match: " +
                                                  string.Join(", ", results.Select(r => r.Value("i"))));
            var result = (IUriNode)results.Results.FirstOrDefault()?.Value("i");
            if (result == null) return null;
            return StripEntityUri(result.Uri);
        }

        public static string RequireEntityFromLabel(string expr, params string[] instancesOf)
        {
            string v;
            if (instancesOf.Length == 0) v = EntityFromLabel(expr);
            else v = instancesOf.Select(i => EntityFromLabel(expr, i)).FirstOrDefault(i => i != null);
            return v ?? throw new KeyNotFoundException("Missing entity: " + expr);
        }

        public static string LabelFromEntity(string id, string lang)
        {
            lang = lang.ToLowerInvariant();
            FB:
            var q = CreateQuery("SELECT ?l WHERE { @id rdfs:label ?l. FILTER (lang(?l) = @lang) } LIMIT 1");
            q.SetUri("id", Graph.ResolveQName("wd:" + id));
            q.SetLiteral("lang", lang);
            var results = ExecuteQuery(q);
            var result = (ILiteralNode)results.Results.FirstOrDefault()?.Value("l");
            if (result == null)
            {
                if (lang == "zh-cn")
                {
                    lang = "zh-hans";
                    goto FB;
                }
                if (lang.Contains('-'))
                {
                    lang = lang.Split('-', 2)[0];
                    goto FB;
                }
                if (lang != "en")
                {
                    lang = "en";
                    goto FB;
                }
                return null;
            }
            return result.Value;
        }

        public static SparqlParameterizedString CreateQuery(string expr = null)
        {
            var q = new SparqlParameterizedString(expr);
            // Use the namespace prefixes as defined in the RDF dump.
            q.Namespaces = Graph.NamespaceMap;
            return q;
        }

    }

    public static class CPNamespaces
    {

        public static readonly Uri Wd = UriFactory.Create("https://crystalpool.cxuesong.com/entity/"),
            Wdt = UriFactory.Create("https://crystalpool.cxuesong.com/prop/direct/");

    }

}
