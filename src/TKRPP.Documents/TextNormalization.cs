using System.Text;

namespace TKRPP.Documents;

/// <summary>
/// Общие операции над извлечённым текстом: нормализация переводов строк и чтение потока.
/// </summary>
internal static class TextNormalization
{
    private static readonly byte[] Utf8Bom = [0xEF, 0xBB, 0xBF];

    /// <summary>
    /// Приводит переводы строк к <c>\n</c> (<c>\r\n</c> и одиночный <c>\r</c>).
    /// Остальное содержимое не меняется.
    /// </summary>
    public static string NormalizeLineEndings(string text) =>
        text.Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace('\r', '\n');

    /// <summary>Отбрасывает BOM UTF-8 в начале файла, если он есть.</summary>
    public static ReadOnlySpan<byte> WithoutUtf8Bom(byte[] bytes) =>
        bytes.AsSpan().StartsWith(Utf8Bom) ? bytes.AsSpan(Utf8Bom.Length) : bytes.AsSpan();

    /// <summary>Читает поток целиком: парсерам нужен доступ к содержимому несколько раз.</summary>
    public static async Task<byte[]> ReadAllBytesAsync(Stream content, CancellationToken cancellationToken)
    {
        if (content is MemoryStream memory && memory.Position == 0 && memory.TryGetBuffer(out var segment))
        {
            return segment.AsSpan(0, (int)memory.Length).ToArray();
        }

        using var buffer = new MemoryStream();
        await content.CopyToAsync(buffer, cancellationToken);
        return buffer.ToArray();
    }

    public static Encoding StrictUtf8 { get; } =
        new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);
}
