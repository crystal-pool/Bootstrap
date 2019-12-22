using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using WikiClientLibrary.Sites;

namespace EntitySeeding
{

    public class CleanupEntityLabelsRoutine : RoutineBase
    {

        /// <inheritdoc />
        public CleanupEntityLabelsRoutine(WikiSite site, ILoggerFactory loggerFactory) : base(site, loggerFactory)
        {
            site.ModificationThrottler.ThrottleTime = TimeSpan.FromSeconds(1);
        }

        public async Task RunAsync()
        {

        }


    }

}
