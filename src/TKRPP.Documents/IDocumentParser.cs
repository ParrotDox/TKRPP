namespace TKRPP.Documents;

/// <summary>
/// Извлечение текста из загруженного пользователем файла.
/// Модуль отвечает только за превращение файла в текстовую строку.
/// </summary>
public interface IDocumentParser
{
    /// <summary>
    /// Разбирает содержимое файла.
    /// </summary>
    /// <param name="content">Поток с содержимым файла.</param>
    /// <param name="fileName">Исходное имя файла (нужно для выбора парсера по расширению).</param>
    /// <param name="cancellationToken">Токен отмены запроса.</param>
    Task<ParseResult> ParseAsync(
        Stream content,
        string fileName,
        CancellationToken cancellationToken = default);
}
