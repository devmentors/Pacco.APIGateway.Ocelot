using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;

namespace Pacco.APIGateway.Ocelot.Infrastructure
{
    internal sealed class AnonymousRouteValidator : IAnonymousRouteValidator
    {
        private readonly HashSet<string> _routes;

        public AnonymousRouteValidator(IOptions<AnonymousRoutesOptions> options)
        {
            _routes = new HashSet<string>(options.Value.Routes ?? Enumerable.Empty<string>());
        }

        public bool HasAccess(string path) => _routes.Contains(path);
    }
}