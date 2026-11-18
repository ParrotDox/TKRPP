using Microsoft.AspNetCore.Http.Features;
using TKRPP.Documents;
using TKRPP.Server.Endpoints;
using TKRPP.Server.Parsing;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = ApiEndpoints.MaxUploadBytes;
});

// TODO(T-05): заменить заглушку на DocumentParser из TKRPP.Documents после мержа парсера.
builder.Services.AddSingleton<IDocumentParser, StubDocumentParser>();

var app = builder.Build();

app.UseExceptionHandler(handler => handler.Run(ApiEndpoints.HandleUnhandledExceptionAsync));

app.UseDefaultFiles();
app.UseStaticFiles();

app.MapApi();

app.Run();

/// <summary>Точка входа для интеграционных тестов (WebApplicationFactory).</summary>
public partial class Program;
