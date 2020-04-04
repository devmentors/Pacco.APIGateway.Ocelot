using System.Collections.Generic;

namespace Pacco.APIGateway.Ocelot.Infrastructure
{
    internal sealed class AsyncRoutesOptions
    {
        public bool? Authenticate { get; set; }
        public IDictionary<string, AsyncRouteOptions> Routes { get; set; }
    }
}