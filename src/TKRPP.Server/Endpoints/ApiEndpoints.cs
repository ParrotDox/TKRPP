using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http.Metadata;
using TKRPP.Documents;
using TKRPP.Server.Contracts;

namespace TKRPP.Server.Endpoints;

public static class ApiEndpoints
{
    /// <summary>Предельный размер загружаемого файла — 25 МБ.</summary>
    public const long MaxUploadBytes = 25L * 1024 * 1024;

    /// <summary>Имя поля multipart-формы, в котором приходит файл.</summary>
    public const string FileFormField = "file";

    private static readonly HashSet<string> AllowedExtensions =
        new(StringComparer.OrdinalIgnoreCase) { ".pdf", ".txt" };

    public static void MapApi(this IEndpointRouteBuilder app)
    {
        var api = app.MapGroup("/api");

        api.MapGet("/health", () => TypedResults.Ok(new HealthResponse("ok")));

        api.MapPost("/parse", ParseAsync)
            .WithMetadata(new RequestSizeLimitMetadata(MaxUploadBytes));
    }

    private static async Task<IResult> ParseAsync(
        HttpContext context,
        IDocumentParser parser,
        ILoggerFactory loggerFactory,
        CancellationToken cancellationToken)
    {
        if (!context.Request.HasFormContentType)
        {
            return ApiErrors.NoFile();
        }

        if (context.Request.ContentLength > MaxUploadBytes)
        {
            return ApiErrors.TooLarge();
        }

        IFormCollection form;
        try
        {
            form = await context.Request.ReadFormAsync(cancellationToken);
        }
        catch (BadHttpRequestException ex) when (ex.StatusCode == StatusCodes.Status413PayloadTooLarge)
        {
            loggerFactory.CreateLogger("Api.Parse")
                .LogWarning(ex, "Upload rejected: request body exceeds {Limit} bytes", MaxUploadBytes);
            return ApiErrors.TooLarge();
        }
        catch (Exception ex) when (ex is InvalidDataException or BadHttpRequestException)
        {
            loggerFactory.CreateLogger("Api.Parse")
                .LogWarning(ex, "Malformed multipart request");
            return ApiErrors.NoFile();
        }

        var file = form.Files.GetFile(FileFormField) ?? form.Files.FirstOrDefault();
        if (file is null)
        {
            return ApiErrors.NoFile();
        }

        var fileName = Path.GetFileName(file.FileName);
        if (!AllowedExtensions.Contains(Path.GetExtension(fileName)))
        {
            return ApiErrors.UnsupportedFormat();
        }

        await using var content = file.OpenReadStream();
        var result = await parser.ParseAsync(content, fileName, cancellationToken);

        if (result.IsSuccess)
        {
            return Results.Ok(new ParseResponse(result.Text ?? string.Empty));
        }

        loggerFactory.CreateLogger("Api.Parse")
            .LogInformation("Parse failed for {FileName}: {Error}", fileName, result.Error);

        return ApiErrors.FromParseError(result.Error);
    }

    /// <summary>
    /// Единый обработчик необработанных исключений: клиент всегда получает JSON с кодом ошибки.
    /// </summary>
    public static async Task HandleUnhandledExceptionAsync(HttpContext context)
    {
        var exception = context.Features.Get<IExceptionHandlerFeature>()?.Error;

        var (statusCode, error) = exception switch
        {
            BadHttpRequestException bad when bad.StatusCode == StatusCodes.Status413PayloadTooLarge
                => (StatusCodes.Status413PayloadTooLarge, ApiErrors.TooLargeError),
            _ => (StatusCodes.Status500InternalServerError, ApiErrors.InternalError)
        };

        var logger = context.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger("Api");
        if (statusCode >= StatusCodes.Status500InternalServerError)
        {
            logger.LogError(exception, "Unhandled exception for {Path}", context.Request.Path);
        }
        else
        {
            logger.LogWarning(exception, "Request rejected for {Path}", context.Request.Path);
        }

        if (context.Response.HasStarted)
        {
            return;
        }

        context.Response.Clear();
        context.Response.StatusCode = statusCode;
        await context.Response.WriteAsJsonAsync(error);
    }

    /// <summary>Ограничение размера тела запроса для минимального API.</summary>
    private sealed class RequestSizeLimitMetadata(long maxRequestBodySize) : IRequestSizeLimitMetadata
    {
        public long? MaxRequestBodySize { get; } = maxRequestBodySize;
    }
}
