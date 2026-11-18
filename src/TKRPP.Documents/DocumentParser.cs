namespace TKRPP.Documents;

/// <summary>
/// Точка входа модуля: выбирает парсер по расширению файла (без учёта регистра).
/// Поддерживаются <c>.txt</c> и <c>.pdf</c>, остальное — <see cref="ParseErrorCode.UnsupportedFormat"/>.
/// </summary>
public sealed class DocumentParser : IDocumentParser
{
    private readonly TxtDocumentParser _txtParser = new();
    private readonly PdfDocumentParser _pdfParser = new();

    public Task<ParseResult> ParseAsync(
        Stream content,
        string fileName,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(content);

        var extension = Path.GetExtension(fileName ?? string.Empty).ToLowerInvariant();

        return extension switch
        {
            ".txt" => _txtParser.ParseAsync(content, cancellationToken),
            ".pdf" => _pdfParser.ParseAsync(content, cancellationToken),
            _ => Task.FromResult(ParseResult.Fail(ParseErrorCode.UnsupportedFormat))
        };
    }
}
