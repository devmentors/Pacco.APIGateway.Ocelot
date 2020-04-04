namespace Pacco.APIGateway.Ocelot.Infrastructure
{
    internal sealed class AsyncRouteOptions
    {
        public bool? Authenticate { get; set; }
        public string Exchange { get; set; }
        public string RoutingKey { get; set; }
    }
}