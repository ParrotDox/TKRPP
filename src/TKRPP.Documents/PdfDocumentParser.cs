using System.Text;
using UglyToad.PdfPig;
using UglyToad.PdfPig.DocumentLayoutAnalysis.TextExtractor;

namespace TKRPP.Documents;

/// <summary>
/// Разбор PDF-файлов через PdfPig: текст извлекается постранично в порядке документа,
/// страницы разделяются <c>\n</c>. OCR не выполняется, поэтому скан без текстового слоя
/// даёт <see cref="ParseErrorCode.NoText"/>.
/// </summary>
public sealed class PdfDocumentParser
{
    private static readonly byte[] Signature = "%PDF-"u8.ToArray();

    public async Task<ParseResult> ParseAsync(Stream content, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(content);

        var bytes = await TextNormalization.ReadAllBytesAsync(content, cancellationToken);
        if (bytes.Length == 0)
        {
            return ParseResult.Fail(ParseErrorCode.EmptyFile);
        }

        if (!bytes.AsSpan().StartsWith(Signature))
        {
            return ParseResult.Fail(ParseErrorCode.BadFile);
        }

        try
        {
            using var document = PdfDocument.Open(bytes);

            var text = new StringBuilder();
            foreach (var page in document.GetPages())
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (text.Length > 0)
                {
                    text.Append('\n');
                }

                text.Append(ContentOrderTextExtractor.GetText(page).Trim());
            }

            var normalized = TextNormalization.NormalizeLineEndings(text.ToString());
            if (string.IsNullOrWhiteSpace(normalized))
            {
                return ParseResult.Fail(ParseErrorCode.NoText);
            }

            return ParseResult.Ok(normalized);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Битый или зашифрованный PDF: PdfPig бросает разные типы исключений.
            return ParseResult.Fail(ParseErrorCode.BadFile);
        }
    }
}
