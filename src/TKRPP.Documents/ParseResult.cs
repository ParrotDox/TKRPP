namespace TKRPP.Documents;

/// <summary>
/// Результат разбора файла: либо извлечённый текст, либо код ошибки.
/// </summary>
public sealed record ParseResult
{
    public bool IsSuccess { get; init; }

    public string? Text { get; init; }

    public ParseErrorCode Error { get; init; }

    public static ParseResult Ok(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        return new() { IsSuccess = true, Text = text };
    }

    public static ParseResult Fail(ParseErrorCode error) =>
        new() { IsSuccess = false, Error = error };
}
