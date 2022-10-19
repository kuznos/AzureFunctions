using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Configurations;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Enums;
using Microsoft.OpenApi.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Traveler.OpenApiConfiguration
{
    internal class OpenApiConfigurationOptions : DefaultOpenApiConfigurationOptions
    {
        public override OpenApiInfo Info { get; set; } = new OpenApiInfo()
        {
            Version = "1.0.0",
            Title = "INTERLIFE Demo Azure Functions",
            Description = "HTTP APIs that run on Azure Functions using OpenAPI specification.",
            TermsOfService = new Uri("https://github.com/Azure/azure-functions-openapi-extension"),
            Contact = new OpenApiContact()
            {
                Name = "INTERLIFE Insurance Company SA",
                Email = "azfunc-openapi@interlife.gr",
                Url = new Uri("https://www.interlife.gr/it-support"),
            },
            License = new OpenApiLicense()
            {
                Name = "MIT",
                Url = new Uri("http://opensource.org/licenses/MIT"),
            }
        };

        public override OpenApiVersionType OpenApiVersion { get; set; } = OpenApiVersionType.V3;

    }
}
