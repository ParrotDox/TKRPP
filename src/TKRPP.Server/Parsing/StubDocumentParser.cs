using TKRPP.Documents;

namespace TKRPP.Server.Parsing;

/// <summary>
/// Временная заглушка парсера, чтобы UI мог разрабатываться до мержа модуля T-05.
/// TODO(T-05): заменить регистрацию на TKRPP.Documents.DocumentParser, когда парсер будет влит.
/// </summary>
internal sealed class StubDocumentParser : IDocumentParser
{
    public async Task<ParseResult> ParseAsync(
        Stream content,
        string fileName,
        CancellationToken cancellationToken = default)
    {
        using var buffer = new MemoryStream();
        await content.CopyToAsync(buffer, cancellationToken);

        if (buffer.Length == 0)
        {
            return ParseResult.Fail(ParseErrorCode.EmptyFile);
        }

        return ParseResult.Ok(
            $"[заглушка парсера] Файл «{fileName}», {buffer.Length} байт. " +
            "Настоящий текст документа появится после задачи T-05.");
    }
}
