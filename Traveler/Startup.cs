using System;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

[assembly: FunctionsStartup(typeof(Traveler.Startup))]
namespace Traveler
{
    public class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            builder.Services.AddSingleton<CosmosClient>(sp => new CosmosClient(Environment.GetEnvironmentVariable("COSMOSDB_CONNECTIONSTRING")));
            builder.Services.AddLogging();

        }
    }
}
