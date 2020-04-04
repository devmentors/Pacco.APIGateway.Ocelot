namespace Pacco.APIGateway.Ocelot.Infrastructure
{
    internal interface IAnonymousRouteValidator
    {
        bool HasAccess(string path);
    }
}