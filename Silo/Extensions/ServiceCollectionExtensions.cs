// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT License.

using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.Extensibility;

namespace Orleans.ShoppingCart.Silo.Extensions
{
    public class ApplicationMapNodeNameInitializer : ITelemetryInitializer
    {
        public ApplicationMapNodeNameInitializer(string name)
        {
            Name = name;
        }

        public string Name { get; set; }

        public void Initialize(ITelemetry telemetry)
        {
            telemetry.Context.Cloud.RoleName = Name;
        }
    }
}

namespace Microsoft.Extensions.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        public static void AddApplicationInsights(this IServiceCollection services, string applicationName)
        {
            services.AddApplicationInsightsTelemetry();
            services.AddSingleton<ITelemetryInitializer>((services) => new ApplicationMapNodeNameInitializer(applicationName));
        }
    }
}
