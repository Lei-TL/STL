namespace STL.SharedServices.File;

public interface IFileHandler
{
    FileFormat Format { get; }

    Task<IReadOnlyList<T>> ImportAsync<T>(
        Stream stream,
        FileImportOptions? options = null,
        CancellationToken ct = default);

    Task<FileExportResult> ExportAsync<T>(
        IEnumerable<T> rows,
        FileExportOptions? options = null,
        CancellationToken ct = default);
}
