using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json.Linq;
using WikiClientLibrary.Wikibase.DataTypes;

namespace EntitySeeding.Contracts
{
    public class CoverImg
    {
        public string File { get; set; }
        public string Title { get; set; }
        public string Area { get; set; }
    }

    public class EditionRef
    {
        public string Work { get; set; }
        public string Url { get; set; }
        public string Name { get; set; }
        public string Title { get; set; }
    }

    public class Edition
    {
        private JToken _Ref;

        public string MediaType { get; set; }

        public string Publisher { get; set; }

        public string Translator { get; set; }

        public string Illustrator { get; set; }

        public string Narrator { get; set; }

        public string Title { get; set; }

        public CoverImg CoverImg { get; set; }

        public string PubDate { get; set; }

        public JToken Ref
        {
            get { return _Ref; }
            set
            {
                _Ref = value;
                if (value == null)
                {
                    RefObj = null;
                }
                else if (value is JObject jobj)
                {
                    RefObj = jobj.ToObject<EditionRef>(Utility.jsonSerializer);
                }
                else if (value is JValue jv)
                {
                    RefObj = new EditionRef { Url = (string)jv };
                }
                else
                {
                    throw new ArgumentException();
                }
            }
        }

        public EditionRef RefObj { get; private set; }

        public IList<EditionRef> Refs { get; set; }

        public string Isbn { get; set; }

        public int EditionNumber { get; set; } = 1;

        public void TidyFields()
        {
            Title = Title?.Trim();
            if (MediaType != null)
            {
                if (MediaType.Contains("重")) EditionNumber = 2;
            }
            MediaType = GetMediaType()?.Trim() ?? "平装书";
            Isbn = Isbn?.Trim();
        }

        private string GetMediaType()
        {
            if (string.IsNullOrEmpty(MediaType)) return null;
            var t = MediaType.Split(new[] { ',', ';', '；' }, StringSplitOptions.None)[0];
            t = t.Replace("套书", "书");
            return t;
        }

        public WbTime? TryParsePubDate()
        {
            if (PubDate == null || PubDate.StartsWith('?')) return null;
            if (DateTime.TryParse(PubDate, out var v)) return WbTime.FromDateTime(v, WikibaseTimePrecision.Day);
            var year = Convert.ToInt16(PubDate);
            return WbTime.FromDateTime(new DateTime(year, 1, 1, 0, 0, 0, DateTimeKind.Utc), WikibaseTimePrecision.Year);
        }

        public IEnumerable<EditionRef> EnumRefs()
        {
            if (RefObj != null) yield return RefObj;
            if (Refs != null)
            {
                foreach (var r in Refs)
                {
                    yield return r;
                }
            }
        }

    }

}
