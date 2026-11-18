using TKRPP.Documents;

namespace TKRPP.Server.Contracts;

public sealed record HealthResponse(string Status);

public sealed record ParseResponse(string Text);

/// <summary>
/// Тело любой ошибки API. <see cref="Message"/> — готовый текст для показа пользователю.
/// </summary>
public sealed record ApiError(string Error, string Message);

public static class ApiErrorCodes
{
    public const string NoFile = "ERR_NO_FILE";
    public const string UnsupportedFormat = "ERR_UNSUPPORTED_FORMAT";
    public const string BadFile = "ERR_BAD_FILE";
    public const string EmptyFile = "ERR_EMPTY_FILE";
    public const string NoText = "ERR_NO_TEXT";
    public const string TooLarge = "ERR_TOO_LARGE";
    public const string Internal = "ERR_INTERNAL";
}

/// <summary>
/// Готовые ответы с ошибками: код, HTTP-статус и сообщение для UI.
/// Коды зафиксированы в docs/architecture/module-interfaces.md.
/// </summary>
public static class ApiErrors
{
    public static ApiError NoFileError { get; } = new(
        ApiErrorCodes.NoFile, "Файл не передан. Выберите файл PDF или TXT.");

    public static ApiError UnsupportedFormatError { get; } = new(
        ApiErrorCodes.UnsupportedFormat, "Поддерживаются только файлы PDF и TXT.");

    public static ApiError BadFileError { get; } = new(
        ApiErrorCodes.BadFile, "Не удалось прочитать файл: он повреждён или не является PDF/TXT.");

    public static ApiError EmptyFileError { get; } = new(
        ApiErrorCodes.EmptyFile, "Файл пуст.");

    public static ApiError NoTextError { get; } = new(
        ApiErrorCodes.NoText, "Не удалось извлечь текст из файла.");

    public static ApiError TooLargeError { get; } = new(
        ApiErrorCodes.TooLarge, "Файл больше 25 МБ.");

    public static ApiError InternalError { get; } = new(
        ApiErrorCodes.Internal, "Внутренняя ошибка сервера.");

    public static IResult NoFile() => Fail(StatusCodes.Status400BadRequest, NoFileError);

    public static IResult UnsupportedFormat() => Fail(StatusCodes.Status400BadRequest, UnsupportedFormatError);

    public static IResult TooLarge() => Fail(StatusCodes.Status413PayloadTooLarge, TooLargeError);

    public static IResult FromParseError(ParseErrorCode error) => error switch
    {
        ParseErrorCode.BadFile => Fail(StatusCodes.Status400BadRequest, BadFileError),
        ParseErrorCode.EmptyFile => Fail(StatusCodes.Status400BadRequest, EmptyFileError),
        ParseErrorCode.NoText => Fail(StatusCodes.Status400BadRequest, NoTextError),
        ParseErrorCode.UnsupportedFormat => Fail(StatusCodes.Status400BadRequest, UnsupportedFormatError),
        _ => Fail(StatusCodes.Status500InternalServerError, InternalError)
    };

    private static IResult Fail(int statusCode, ApiError error) =>
        Results.Json(error, statusCode: statusCode);
}
