using Microsoft.AspNetCore.Http.Features;
using TKRPP.Documents;
using TKRPP.Server.Endpoints;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = ApiEndpoints.MaxUploadBytes;
});

builder.Services.AddSingleton<IDocumentParser, DocumentParser>();

var app = builder.Build();

app.UseExceptionHandler(handler => handler.Run(ApiEndpoints.HandleUnhandledExceptionAsync));

app.UseDefaultFiles();
app.UseStaticFiles();

app.MapApi();

app.Run();

/// <summary>Точка входа для интеграционных тестов (WebApplicationFactory).</summary>
public partial class Program;
