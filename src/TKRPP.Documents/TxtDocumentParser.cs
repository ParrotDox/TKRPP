using System.Text;

namespace TKRPP.Documents;

/// <summary>
/// Разбор текстовых файлов (<c>.txt</c>): строгий UTF-8, BOM допускается,
/// переводы строк нормализуются к <c>\n</c>, остальное содержимое не искажается.
/// </summary>
public sealed class TxtDocumentParser
{
    public async Task<ParseResult> ParseAsync(Stream content, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(content);

        var bytes = await TextNormalization.ReadAllBytesAsync(content, cancellationToken);
        if (bytes.Length == 0)
        {
            return ParseResult.Fail(ParseErrorCode.EmptyFile);
        }

        string text;
        try
        {
            text = TextNormalization.StrictUtf8.GetString(TextNormalization.WithoutUtf8Bom(bytes));
        }
        catch (DecoderFallbackException)
        {
            return ParseResult.Fail(ParseErrorCode.BadFile);
        }

        text = TextNormalization.NormalizeLineEndings(text);
        if (string.IsNullOrWhiteSpace(text))
        {
            return ParseResult.Fail(ParseErrorCode.NoText);
        }

        return ParseResult.Ok(text);
    }
}
