using System.Collections.Generic;

namespace Pacco.APIGateway.Ocelot.Infrastructure
{
    internal sealed class AnonymousRoutesOptions
    {
        public IEnumerable<string> Routes { get; set; }
    }
}