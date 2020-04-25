using System.Threading.Tasks;
using App.Metrics.AspNetCore;
using Convey;
using Convey.Auth;
using Convey.Logging;
using Convey.MessageBrokers.RabbitMQ;
using Convey.Secrets.Vault;
using Convey.Security;
using Convey.Tracing.Jaeger;
using Convey.Types;
using Convey.WebApi;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using Ocelot.Provider.Polly;
using Pacco.APIGateway.Ocelot.Infrastructure;

namespace Pacco.APIGateway.Ocelot
{
    public class Program
    {
        public static Task Main(string[] args) => CreateHostBuilder(args).Build().RunAsync();

        public static IHostBuilder CreateHostBuilder(string[] args)
            => Host.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration((hostingContext, config) =>
                {
                    config
                        .SetBasePath(hostingContext.HostingEnvironment.ContentRootPath)
                        .AddJsonFile("appsettings.json", false)
                        .AddJsonFile($"appsettings.{hostingContext.HostingEnvironment.EnvironmentName}.json", true, true)
                        .AddJsonFile("ocelot.json")
                        .AddEnvironmentVariables();
                })
                .ConfigureWebHostDefaults(builder => builder
                    .ConfigureServices(services =>
                    {
                        services.AddMetrics();
                        services.AddHttpClient();
                        services.AddSingleton<IPayloadBuilder, PayloadBuilder>();
                        services.AddSingleton<ICorrelationContextBuilder, CorrelationContextBuilder>();
                        services.AddSingleton<IAnonymousRouteValidator, AnonymousRouteValidator>();
                        services.AddTransient<AsyncRoutesMiddleware>();
                        services.AddTransient<ResourceIdGeneratorMiddleware>();
                        services.AddOcelot()
                            .AddPolly()
                            .AddDelegatingHandler<CorrelationContextHandler>(true);

                        services
                            .AddConvey()
                            .AddErrorHandler<ExceptionToResponseMapper>()
                            .AddJaeger()
                            .AddJwt()
                            .AddRabbitMq()
                            .AddSecurity()
                            .AddWebApi()
                            .Build();

                        using var provider = services.BuildServiceProvider();
                        var configuration = provider.GetService<IConfiguration>();
                        services.Configure<AsyncRoutesOptions>(configuration.GetSection("AsyncRoutes"));
                        services.Configure<AnonymousRoutesOptions>(configuration.GetSection("AnonymousRoutes"));
                    })
                    .Configure(app =>
                    {
                        app.UseConvey();
                        app.UseErrorHandler();
                        app.UseAccessTokenValidator();
                        app.UseAuthentication();
                        app.UseRabbitMq();
                        app.MapWhen(ctx => ctx.Request.Path == "/", a =>
                        {
                            a.Use((ctx, next) =>
                            {
                                var appOptions = ctx.RequestServices.GetRequiredService<AppOptions>();
                                return ctx.Response.WriteAsync(appOptions.Name);
                            });
                        });
                        app.UseMiddleware<AsyncRoutesMiddleware>();
                        app.UseMiddleware<ResourceIdGeneratorMiddleware>();
                        app.UseOcelot(GetOcelotConfiguration()).GetAwaiter().GetResult();
                    })
                    .UseLogging()
                    .UseVault()
                    .UseMetrics());

        private static OcelotPipelineConfiguration GetOcelotConfiguration()
            => new OcelotPipelineConfiguration
            {
                AuthenticationMiddleware = async (context, next) =>
                {
                    if (!context.DownstreamReRoute.IsAuthenticated)
                    {
                        await next.Invoke();
                        return;
                    }

                    if (context.HttpContext.RequestServices.GetRequiredService<IAnonymousRouteValidator>()
                        .HasAccess(context.HttpContext.Request.Path))
                    {
                        await next.Invoke();
                        return;
                    }

                    var authenticateResult = await context.HttpContext.AuthenticateAsync();
                    if (authenticateResult.Succeeded)
                    {
                        context.HttpContext.User = authenticateResult.Principal;
                        await next.Invoke();
                        return;
                    }

                    context.Errors.Add(new UnauthenticatedError("Unauthenticated"));
                }
            };
    }
}
