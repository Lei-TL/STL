namespace STL.SharedServices.File;

public enum FileFormat
{
    Excel,
    Csv
}

public sealed record FileImportOptions(
    bool HasHeader = true,
    string? WorksheetName = null);

public sealed record FileExportOptions(
    string? FileName = null,
    string? SheetName = null,
    bool IncludeHeader = true);

public sealed record FileExportResult(
    byte[] Content,
    string FileName,
    string ContentType,
    FileFormat Format);
