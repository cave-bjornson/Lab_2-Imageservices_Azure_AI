using Azure;
using Azure.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Azure;
using Microsoft.Azure.CognitiveServices.Vision.CustomVision;
using Microsoft.Azure.CognitiveServices.Vision.CustomVision.Prediction;
using Microsoft.Extensions.Options;
using WebApp;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();

builder.Services.Configure<CustomVisionOptions>(builder.Configuration.GetSection("CustomVision"));

builder.Services.AddAzureClients(clientBuilder =>
{
    clientBuilder.AddClient<CustomVisionPredictionClient, CustomVisionPredictionClientOptions>(
        (_, provider) =>
        {
            var visionOptions = provider.GetService<IOptions<CustomVisionOptions>>()?.Value;

            return new CustomVisionPredictionClient(
                new ApiKeyServiceClientCredentials(visionOptions?.PredictionKey)
            )
            {
                Endpoint = visionOptions?.PredictionEndpoint
            };
        }
    );
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapRazorPages();

app.Run();
