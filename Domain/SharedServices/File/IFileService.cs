namespace STL.SharedServices.File;

public interface IFileService
{
    Task<IReadOnlyList<T>> ImportAsync<T>(
        Stream stream,
        string fileName,
        FileImportOptions? options = null,
        CancellationToken ct = default);

    Task<FileExportResult> ExportAsync<T>(
        IEnumerable<T> rows,
        FileFormat format,
        FileExportOptions? options = null,
        CancellationToken ct = default);
}
