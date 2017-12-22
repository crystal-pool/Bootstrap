using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.Logging;
using WikiClientLibrary.Sites;

namespace EntitySeeding
{
    public class RoutineBase
    {

        public WikiSite Site { get; }

        public ILogger Logger { get; }

        public RoutineBase(WikiSite site, ILoggerFactory loggerFactory)
        {
            Site = site ?? throw new ArgumentNullException(nameof(site));
            Logger = loggerFactory.CreateLogger(GetType());
        }

    }
}
