// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT License.

using Azure.Data.Tables;

var builder = WebApplication.CreateBuilder(args);
builder.UseOrleans(siloBuilder =>
{
    siloBuilder.AddStartupTask<SeedProductStoreTask>();
    if (builder.Environment.IsDevelopment())
    {
        siloBuilder.UseLocalhostClustering()
            .AddMemoryGrainStorage("shopping-cart");
    }
    else
    {
        var endpointAddress = IPAddress.Parse(builder.Configuration["WEBSITE_PRIVATE_IP"]!);
        var strPorts = builder.Configuration["WEBSITE_PRIVATE_PORTS"]!.Split(',');
        if (strPorts.Length < 2)
        {
            throw new Exception("Insufficient private ports configured.");
        }

        var (siloPort, gatewayPort) = (int.Parse(strPorts[0]), int.Parse(strPorts[1]));

        siloBuilder
            .ConfigureEndpoints(endpointAddress, siloPort, gatewayPort, listenOnAnyHostAddress: true)
            .Configure<ClusterOptions>(
                options =>
                {
                    options.ClusterId = builder.Configuration["ORLEANS_CLUSTER_ID"];
                    options.ServiceId = nameof(ShoppingCartService);
                })
            .UseAzureStorageClustering(
                options =>
                {
                    options.TableServiceClient = new TableServiceClient(builder.Configuration["ORLEANS_AZURE_STORAGE_CONNECTION_STRING"]);
                    options.TableName = $"{builder.Configuration["ORLEANS_CLUSTER_ID"]}Clustering";
                })
            .AddAzureTableGrainStorage("shopping-cart",
                options =>
                {
                    options.TableServiceClient = new TableServiceClient(builder.Configuration["ORLEANS_AZURE_STORAGE_CONNECTION_STRING"]);
                    options.TableName = $"{builder.Configuration["ORLEANS_CLUSTER_ID"]}Persistence";
                });
    }
});

var services  = builder.Services; 
services.AddMudServices();
services.AddRazorPages();
services.AddServerSideBlazor();
services.AddHttpContextAccessor();
services.AddSingleton<ShoppingCartService>();
services.AddSingleton<InventoryService>();
services.AddSingleton<ProductService>();
services.AddScoped<ComponentStateChangedObserver>();
services.AddSingleton<ToastService>();
services.AddLocalStorageServices();
services.AddApplicationInsights("Silo");

var app = builder.Build();

if (builder.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.MapBlazorHub();
app.MapFallbackToPage("/_Host");
await app.RunAsync();
