namespace STL.SharedServices.File;

public class FileService(IEnumerable<IFileHandler> handlers) : IFileService
{
    public Task<IReadOnlyList<T>> ImportAsync<T>(
        Stream stream,
        string fileName,
        FileImportOptions? options = null,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(stream);

        if (string.IsNullOrWhiteSpace(fileName))
        {
            throw new ArgumentException("File name is required.", nameof(fileName));
        }

        var format = ResolveFormat(fileName);
        var handler = GetHandler(format);

        return handler.ImportAsync<T>(stream, options, ct);
    }

    public Task<FileExportResult> ExportAsync<T>(
        IEnumerable<T> rows,
        FileFormat format,
        FileExportOptions? options = null,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(rows);

        var handler = GetHandler(format);

        return handler.ExportAsync(rows, options, ct);
    }

    private IFileHandler GetHandler(FileFormat format)
    {
        return handlers.FirstOrDefault(handler => handler.Format == format)
            ?? throw new NotSupportedException(
                $"File format '{format}' does not have a registered handler.");
    }

    private static FileFormat ResolveFormat(string fileName)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();

        return extension switch
        {
            ".xlsx" => FileFormat.Excel,
            ".csv" => FileFormat.Csv,
            _ => throw new NotSupportedException(
                $"File extension '{extension}' is not supported.")
        };
    }
}
