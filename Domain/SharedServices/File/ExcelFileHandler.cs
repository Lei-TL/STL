using ClosedXML.Excel;

namespace STL.SharedServices.File;

public class ExcelFileHandler : IFileHandler
{
    private const string ContentType =
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

    public FileFormat Format => FileFormat.Excel;

    public Task<IReadOnlyList<T>> ImportAsync<T>(
        Stream stream,
        FileImportOptions? options = null,
        CancellationToken ct = default)
    {
        options ??= new FileImportOptions();

        using var workbook = new XLWorkbook(stream);
        var worksheet = string.IsNullOrWhiteSpace(options.WorksheetName)
            ? workbook.Worksheets.First()
            : workbook.Worksheet(options.WorksheetName);
        var firstRow = worksheet.FirstRowUsed()
            ?? throw new InvalidOperationException("Excel file does not contain any data.");
        var properties = FileMappingHelper.GetWritableProperties<T>();
        var propertyMap = FileMappingHelper.CreatePropertyMap<T>(properties);
        var columnMap = options.HasHeader
            ? CreateHeaderColumnMap(firstRow, propertyMap)
            : CreateDefaultColumnMap(properties);
        var firstDataRowNumber = options.HasHeader
            ? firstRow.RowNumber() + 1
            : firstRow.RowNumber();
        var lastRow = worksheet.LastRowUsed()?.RowNumber() ?? firstDataRowNumber - 1;
        var result = new List<T>();

        for (var rowNumber = firstDataRowNumber; rowNumber <= lastRow; rowNumber++)
        {
            ct.ThrowIfCancellationRequested();

            var row = worksheet.Row(rowNumber);

            if (row.IsEmpty())
            {
                continue;
            }

            var item = FileMappingHelper.CreateItem<T>();

            foreach (var (columnNumber, property) in columnMap)
            {
                var value = row.Cell(columnNumber).GetString();
                FileMappingHelper.SetPropertyValue(item, property, value);
            }

            result.Add(item);
        }

        return Task.FromResult<IReadOnlyList<T>>(result);
    }

    public Task<FileExportResult> ExportAsync<T>(
        IEnumerable<T> rows,
        FileExportOptions? options = null,
        CancellationToken ct = default)
    {
        options ??= new FileExportOptions();

        var properties = FileMappingHelper.GetReadableProperties<T>();
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add(
            string.IsNullOrWhiteSpace(options.SheetName)
                ? "Sheet1"
                : options.SheetName);
        var rowNumber = 1;

        if (options.IncludeHeader)
        {
            for (var i = 0; i < properties.Count; i++)
            {
                worksheet.Cell(rowNumber, i + 1).Value = properties[i].Name;
            }

            rowNumber++;
        }

        foreach (var row in rows)
        {
            ct.ThrowIfCancellationRequested();

            for (var i = 0; i < properties.Count; i++)
            {
                worksheet.Cell(rowNumber, i + 1).Value =
                    FileMappingHelper.GetPropertyValue(row, properties[i]);
            }

            rowNumber++;
        }

        worksheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);

        var fileName = string.IsNullOrWhiteSpace(options.FileName)
            ? $"{typeof(T).Name}.xlsx"
            : EnsureExtension(options.FileName, ".xlsx");

        return Task.FromResult(new FileExportResult(
            stream.ToArray(),
            fileName,
            ContentType,
            Format));
    }

    private static Dictionary<int, System.Reflection.PropertyInfo> CreateHeaderColumnMap(
        IXLRow headerRow,
        IReadOnlyDictionary<string, System.Reflection.PropertyInfo> propertyMap)
    {
        return headerRow.CellsUsed()
            .Select(cell => new
            {
                ColumnNumber = cell.Address.ColumnNumber,
                Header = cell.GetString().Trim()
            })
            .Where(cell => propertyMap.ContainsKey(cell.Header))
            .ToDictionary(
                cell => cell.ColumnNumber,
                cell => propertyMap[cell.Header]);
    }

    private static Dictionary<int, System.Reflection.PropertyInfo> CreateDefaultColumnMap(
        IReadOnlyList<System.Reflection.PropertyInfo> properties)
    {
        return properties
            .Select((property, index) => new
            {
                ColumnNumber = index + 1,
                Property = property
            })
            .ToDictionary(
                item => item.ColumnNumber,
                item => item.Property);
    }

    private static string EnsureExtension(string fileName, string extension)
    {
        return Path.GetExtension(fileName).Equals(extension, StringComparison.OrdinalIgnoreCase)
            ? fileName
            : $"{fileName}{extension}";
    }
}
