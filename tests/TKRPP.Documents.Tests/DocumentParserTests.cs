using System.Diagnostics;
using System.Text;

namespace TKRPP.Documents.Tests;

/// <summary>
/// Unit-тесты парсера документов по docs/testing.md (UT-01…UT-05) и проверки
/// заявленных ограничений модуля (строгий UTF-8, BOM, расширения, скорость).
/// </summary>
public sealed class DocumentParserTests
{
    private readonly DocumentParser _parser = new();

    [Fact]
    public async Task UT_01_ParsePdf_ValidPdf_ReturnsText()
    {
        var pdf = PdfFixtures.Create(
            "Lecture one: sorting algorithms",
            "Lecture two: data structures");

        var result = await ParsePdfAsync(pdf);

        Assert.True(result.IsSuccess);
        Assert.Equal(ParseErrorCode.None, result.Error);
        Assert.NotNull(result.Text);
        Assert.False(string.IsNullOrWhiteSpace(result.Text));

        Assert.Contains("sorting", result.Text, StringComparison.Ordinal);
        Assert.Contains("algorithms", result.Text, StringComparison.Ordinal);
        Assert.Contains("structures", result.Text, StringComparison.Ordinal);

        // Порядок текста сохраняется: сначала первая страница, затем вторая.
        Assert.True(
            result.Text.IndexOf("Lecture one", StringComparison.Ordinal) <
            result.Text.IndexOf("Lecture two", StringComparison.Ordinal));
    }

    [Fact]
    public async Task UT_02_ParseTxt_ValidUtf8_ReturnsFileContent()
    {
        var fileContent = "Первая строка\nВторая строка";
        var bytes = Encoding.UTF8.GetBytes("Первая строка\r\nВторая строка");

        var result = await ParseTxtAsync(bytes);

        Assert.True(result.IsSuccess);
        Assert.Equal(ParseErrorCode.None, result.Error);
        Assert.Equal(fileContent, result.Text);
    }

    [Fact]
    public async Task UT_03_ParsePdf_BinaryBytesWithPdfExtension_ReturnsBadFile()
    {
        var bytes = new byte[512];
        new Random(42).NextBytes(bytes);

        var result = await ParsePdfAsync(bytes);

        Assert.False(result.IsSuccess);
        Assert.Equal(ParseErrorCode.BadFile, result.Error);
        Assert.Null(result.Text);
    }

    [Fact]
    public async Task UT_04_ParseTxt_EmptyFile_ReturnsEmptyFile()
    {
        var result = await ParseTxtAsync([]);

        Assert.False(result.IsSuccess);
        Assert.Equal(ParseErrorCode.EmptyFile, result.Error);
        Assert.Null(result.Text);
    }

    [Fact]
    public async Task UT_05_ParsePdf_ScanWithoutTextLayer_ReturnsNoText()
    {
        var pdf = PdfFixtures.CreateWithoutTextLayer();

        var result = await ParsePdfAsync(pdf);

        Assert.False(result.IsSuccess);
        Assert.Equal(ParseErrorCode.NoText, result.Error);
        Assert.Null(result.Text);
    }

    [Fact]
    public async Task ParseTxt_InvalidUtf8_ReturnsBadFile()
    {
        byte[] bytes = [0x41, 0xFF, 0xFE, 0x42];

        var result = await ParseTxtAsync(bytes);

        Assert.False(result.IsSuccess);
        Assert.Equal(ParseErrorCode.BadFile, result.Error);
    }

    [Fact]
    public async Task ParseTxt_WithUtf8Bom_StripsBomAndKeepsText()
    {
        var bytes = new byte[] { 0xEF, 0xBB, 0xBF }
            .Concat(Encoding.UTF8.GetBytes("Конспект лекции"))
            .ToArray();

        var result = await ParseTxtAsync(bytes);

        Assert.True(result.IsSuccess);
        Assert.Equal("Конспект лекции", result.Text);
    }

    [Fact]
    public async Task ParseTxt_WhitespaceOnly_ReturnsNoText()
    {
        var bytes = Encoding.UTF8.GetBytes("  \r\n\t\r\n ");

        var result = await ParseTxtAsync(bytes);

        Assert.False(result.IsSuccess);
        Assert.Equal(ParseErrorCode.NoText, result.Error);
    }

    [Theory]
    [InlineData("лекция.docx")]
    [InlineData("конспект")]
    [InlineData("")]
    public async Task Parse_UnsupportedExtension_ReturnsUnsupportedFormat(string fileName)
    {
        using var content = new MemoryStream([1, 2, 3]);

        var result = await _parser.ParseAsync(content, fileName);

        Assert.False(result.IsSuccess);
        Assert.Equal(ParseErrorCode.UnsupportedFormat, result.Error);
    }

    [Theory]
    [InlineData("лекция.pdf")]
    [InlineData("ЛЕКЦИЯ.PDF")]
    public async Task ParsePdf_ExtensionCaseInsensitive_ReturnsText(string fileName)
    {
        using var content = new MemoryStream(PdfFixtures.Create("Case insensitive extension"));

        var result = await _parser.ParseAsync(content, fileName);

        Assert.True(result.IsSuccess);
        Assert.Contains("insensitive", result.Text, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ParsePdf_TwentyPages_ParsesFasterThanThreeSeconds()
    {
        var pages = Enumerable.Range(1, 20)
            .Select(number => $"Page {number}: lecture material for performance check")
            .ToArray();
        var pdf = PdfFixtures.Create(pages);

        var stopwatch = Stopwatch.StartNew();
        var result = await ParsePdfAsync(pdf);
        stopwatch.Stop();

        Assert.True(result.IsSuccess);
        Assert.Contains("Page 20", result.Text, StringComparison.Ordinal);
        Assert.True(
            stopwatch.Elapsed < TimeSpan.FromSeconds(3),
            $"Разбор 20 страниц занял {stopwatch.Elapsed}, ожидалось меньше 3 секунд.");
    }

    private Task<ParseResult> ParseTxtAsync(byte[] bytes) => ParseAsync(bytes, "конспект.txt");

    private Task<ParseResult> ParsePdfAsync(byte[] bytes) => ParseAsync(bytes, "лекция.pdf");

    private Task<ParseResult> ParseAsync(byte[] bytes, string fileName)
    {
        using var content = new MemoryStream(bytes);
        return _parser.ParseAsync(content, fileName);
    }
}
