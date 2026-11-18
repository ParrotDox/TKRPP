namespace TKRPP.Documents;

/// <summary>
/// Код ошибки разбора файла. Соответствует кодам, которые сервер отдаёт в UI
/// (см. docs/architecture/module-interfaces.md).
/// </summary>
public enum ParseErrorCode
{
    None = 0,
    BadFile,
    EmptyFile,
    NoText,
    UnsupportedFormat
}
