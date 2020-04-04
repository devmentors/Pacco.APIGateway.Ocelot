using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Pacco.APIGateway.Ocelot.Infrastructure
{
    internal interface IPayloadBuilder
    {
        Task<T> BuildFromJsonAsync<T>(HttpRequest request) where T : class, new();
    }
}