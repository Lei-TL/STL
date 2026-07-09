using System.Data;
using System.Text;
using Sylvan.Data.Csv;

namespace STL.SharedServices.File;

public class CsvFileHandler : IFileHandler
{
    private const string ContentType = "text/csv";

    public FileFormat Format => FileFormat.Csv;

    public Task<IReadOnlyList<T>> ImportAsync<T>(
        Stream stream,
        FileImportOptions? options = null,
        CancellationToken ct = default)
    {
        options ??= new FileImportOptions();

        return ImportInternalAsync<T>(stream, options, ct);
    }

    public Task<FileExportResult> ExportAsync<T>(
        IEnumerable<T> rows,
        FileExportOptions? options = null,
        CancellationToken ct = default)
    {
        options ??= new FileExportOptions();

        return ExportInternalAsync(rows, options, ct);
    }

    private static async Task<IReadOnlyList<T>> ImportInternalAsync<T>(
        Stream stream,
        FileImportOptions options,
        CancellationToken ct)
    {
        using var reader = new StreamReader(
            stream,
            Encoding.UTF8,
            detectEncodingFromByteOrderMarks: true);
        await using var csv = await CsvDataReader.CreateAsync(
            reader,
            new CsvDataReaderOptions
            {
                HasHeaders = options.HasHeader,
                HeaderComparer = StringComparer.OrdinalIgnoreCase
            },
            ct);

        var properties = FileMappingHelper.GetWritableProperties<T>();
        var columnMap = options.HasHeader
            ? CreateHeaderColumnMap(csv, properties)
            : CreateDefaultColumnMap(properties);
        var result = new List<T>();

        while (await csv.ReadAsync(ct))
        {
            var item = FileMappingHelper.CreateItem<T>();

            foreach (var (ordinal, property) in columnMap)
            {
                if (csv.IsDBNull(ordinal))
                {
                    continue;
                }

                FileMappingHelper.SetPropertyValue(
                    item,
                    property,
                    csv.GetString(ordinal));
            }

            result.Add(item);
        }

        return result;
    }

    private static async Task<FileExportResult> ExportInternalAsync<T>(
        IEnumerable<T> rows,
        FileExportOptions options,
        CancellationToken ct)
    {
        var table = CreateDataTable(rows);
        using var stream = new MemoryStream();
        await using var writer = new StreamWriter(
            stream,
            new UTF8Encoding(encoderShouldEmitUTF8Identifier: true),
            leaveOpen: true);
        await using var csv = CsvDataWriter.Create(
            writer,
            new CsvDataWriterOptions
            {
                WriteHeaders = options.IncludeHeader
            });

        await csv.WriteAsync(table.CreateDataReader(), ct);
        await writer.FlushAsync(ct);

        var fileName = string.IsNullOrWhiteSpace(options.FileName)
            ? $"{typeof(T).Name}.csv"
            : EnsureExtension(options.FileName, ".csv");

        return new FileExportResult(
            stream.ToArray(),
            fileName,
            ContentType,
            FileFormat.Csv);
    }

    private static Dictionary<int, System.Reflection.PropertyInfo> CreateHeaderColumnMap(
        CsvDataReader csv,
        IEnumerable<System.Reflection.PropertyInfo> properties)
    {
        var map = new Dictionary<int, System.Reflection.PropertyInfo>();

        foreach (var property in properties)
        {
            var ordinal = TryGetOrdinal(csv, property.Name);

            if (ordinal >= 0)
            {
                map[ordinal] = property;
            }
        }

        return map;
    }

    private static Dictionary<int, System.Reflection.PropertyInfo> CreateDefaultColumnMap(
        IReadOnlyList<System.Reflection.PropertyInfo> properties)
    {
        return properties
            .Select((property, index) => new
            {
                Ordinal = index,
                Property = property
            })
            .ToDictionary(
                item => item.Ordinal,
                item => item.Property);
    }

    private static int TryGetOrdinal(CsvDataReader csv, string name)
    {
        try
        {
            return csv.GetOrdinal(name);
        }
        catch (IndexOutOfRangeException)
        {
            return -1;
        }
    }

    private static DataTable CreateDataTable<T>(IEnumerable<T> rows)
    {
        var properties = FileMappingHelper.GetReadableProperties<T>();
        var table = new DataTable(typeof(T).Name);

        foreach (var property in properties)
        {
            table.Columns.Add(property.Name, typeof(string));
        }

        foreach (var row in rows)
        {
            var values = properties
                .Select(property => FileMappingHelper.GetPropertyValue(row, property))
                .Cast<object>()
                .ToArray();

            table.Rows.Add(values);
        }

        return table;
    }

    private static string EnsureExtension(string fileName, string extension)
    {
        return Path.GetExtension(fileName).Equals(extension, StringComparison.OrdinalIgnoreCase)
            ? fileName
            : $"{fileName}{extension}";
    }
}
