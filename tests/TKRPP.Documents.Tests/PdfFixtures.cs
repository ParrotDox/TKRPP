using UglyToad.PdfPig.Content;
using UglyToad.PdfPig.Core;
using UglyToad.PdfPig.Fonts.Standard14Fonts;
using UglyToad.PdfPig.Writer;

namespace TKRPP.Documents.Tests;

/// <summary>
/// PDF-фикстуры генерируются в коде: бинарные файлы в репозиторий не попадают,
/// сеть в тестах не используется.
/// </summary>
internal static class PdfFixtures
{
    /// <summary>Валидный PDF с текстовым слоем: одна страница на каждый переданный текст.</summary>
    public static byte[] Create(params string[] pageTexts)
    {
        var builder = new PdfDocumentBuilder();
        var font = builder.AddStandard14Font(Standard14Font.Helvetica);

        foreach (var pageText in pageTexts)
        {
            var page = builder.AddPage(PageSize.A4);
            page.AddText(pageText, 12, new PdfPoint(50, 780), font);
        }

        return builder.Build();
    }

    /// <summary>PDF без текстового слоя — аналог скана, распознавать который модуль не умеет.</summary>
    public static byte[] CreateWithoutTextLayer()
    {
        var builder = new PdfDocumentBuilder();
        builder.AddPage(PageSize.A4);
        return builder.Build();
    }
}
