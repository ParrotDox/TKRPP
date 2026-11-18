using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using TKRPP.Documents;
using TKRPP.Server.Contracts;
using TKRPP.Server.Endpoints;

namespace TKRPP.Server.Tests;

public sealed class ApiEndpointsTests
{
    [Fact]
    public async Task Health_ReturnsOkStatus()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<HealthResponse>();
        Assert.Equal("ok", payload?.Status);
    }

    [Fact]
    public async Task Root_ServesIndexPageFromWwwroot()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/html", response.Content.Headers.ContentType?.MediaType);
        Assert.Contains("ТКРПП", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Parse_WithoutFile_ReturnsNoFileError()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();
        using var form = new MultipartFormDataContent();

        var response = await client.PostAsync("/api/parse", form);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ApiError>();
        Assert.Equal(ApiErrorCodes.NoFile, error?.Error);
        Assert.False(string.IsNullOrWhiteSpace(error?.Message));
    }

    [Fact]
    public async Task Parse_UnsupportedExtension_ReturnsUnsupportedFormatAndSkipsParser()
    {
        var parser = new FakeDocumentParser(ParseResult.Ok("не должно использоваться"));
        using var factory = CreateFactory(parser);
        using var client = factory.CreateClient();
        using var form = CreateFileForm("лекция.docx");

        var response = await client.PostAsync("/api/parse", form);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ApiError>();
        Assert.Equal(ApiErrorCodes.UnsupportedFormat, error?.Error);
        Assert.Equal(0, parser.Calls);
    }

    [Theory]
    [InlineData(ParseErrorCode.BadFile, ApiErrorCodes.BadFile)]
    [InlineData(ParseErrorCode.EmptyFile, ApiErrorCodes.EmptyFile)]
    [InlineData(ParseErrorCode.NoText, ApiErrorCodes.NoText)]
    [InlineData(ParseErrorCode.UnsupportedFormat, ApiErrorCodes.UnsupportedFormat)]
    public async Task Parse_ParserError_IsMappedToStructuredError(
        ParseErrorCode parserError,
        string expectedCode)
    {
        var parser = new FakeDocumentParser(ParseResult.Fail(parserError));
        using var factory = CreateFactory(parser);
        using var client = factory.CreateClient();
        using var form = CreateFileForm("лекция.txt");

        var response = await client.PostAsync("/api/parse", form);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ApiError>();
        Assert.Equal(expectedCode, error?.Error);
        Assert.False(string.IsNullOrWhiteSpace(error?.Message));
    }

    [Fact]
    public async Task Parse_Success_ReturnsExtractedText()
    {
        var parser = new FakeDocumentParser(ParseResult.Ok("Первая строка\nВторая строка"));
        using var factory = CreateFactory(parser);
        using var client = factory.CreateClient();
        using var form = CreateFileForm("лекция.txt", [0x41, 0x42]);

        var response = await client.PostAsync("/api/parse", form);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<ParseResponse>();
        Assert.Equal("Первая строка\nВторая строка", payload?.Text);
        Assert.Equal(1, parser.Calls);
        Assert.Equal("лекция.txt", parser.LastFileName);
    }

    [Fact]
    public async Task Parse_RealStubOnEmptyFile_ReportsEmptyFile()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();
        using var form = CreateFileForm("пустой.txt", []);

        var response = await client.PostAsync("/api/parse", form);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ApiError>();
        Assert.Equal(ApiErrorCodes.EmptyFile, error?.Error);
    }

    private static MultipartFormDataContent CreateFileForm(string fileName, byte[]? bytes = null)
    {
        var form = new MultipartFormDataContent();
        var file = new ByteArrayContent(bytes ?? [1, 2, 3]);
        file.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
        form.Add(file, ApiEndpoints.FileFormField, fileName);
        return form;
    }

    private static WebApplicationFactory<Program> CreateFactory(IDocumentParser? parser = null)
    {
        var factory = new WebApplicationFactory<Program>();
        if (parser is null)
        {
            return factory;
        }

        return factory.WithWebHostBuilder(builder =>
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IDocumentParser>();
                services.AddSingleton(parser);
            }));
    }

    private sealed class FakeDocumentParser(ParseResult result) : IDocumentParser
    {
        public int Calls { get; private set; }

        public string? LastFileName { get; private set; }

        public Task<ParseResult> ParseAsync(
            Stream content,
            string fileName,
            CancellationToken cancellationToken = default)
        {
            Calls++;
            LastFileName = fileName;
            return Task.FromResult(result);
        }
    }
}
