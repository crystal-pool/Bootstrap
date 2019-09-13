
using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace ConvertTimeline.Contracts
{
    public class DetailEntry
    {

        public int Year { get; set; }

        public int Month { get; set; }

    }

    public class BookEntry
    {

        public IList<string> Interval { get; set; }

        public string BookName { get; set; }

        // IDictionary<string, DetailEntry>
        public JToken Details { get; set; }

    }

}

