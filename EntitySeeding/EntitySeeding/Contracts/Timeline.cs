
using System;
using System.Collections.Generic;

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

        public IDictionary<string, DetailEntry> Details { get; set; }

    }

}

