using System.Data;
using OfficeOpenXml;
using System.Reflection;

namespace PWCEPortal.Services;

public class ExcelExportService
{
    public byte[] ExportToExcel<T>(List<T> data, Dictionary<string, string> columnMappings, string sheetName = "Export")
    {
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
        using var package = new ExcelPackage();
        var worksheet = package.Workbook.Worksheets.Add(sheetName);

        // Add headers
        int colIndex = 1;
        foreach (var mapping in columnMappings)
        {
            worksheet.Cells[1, colIndex].Value = mapping.Value;
            worksheet.Cells[1, colIndex].Style.Font.Bold = true;
            colIndex++;
        }

        // Add data
        for (int row = 0; row < data.Count; row++)
        {
            colIndex = 1;
            foreach (var mapping in columnMappings)
            {
                // Handle nested properties
                object value = data[row];
                string[] propertyParts = mapping.Key.Split('.');
            
                foreach (string part in propertyParts)
                {
                    if (value == null) break;
                
                    PropertyInfo prop = value.GetType().GetProperty(part);
                    if (prop != null)
                    {
                        value = prop.GetValue(value);
                    }
                    else
                    {
                        value = null;
                    }
                }

                var cell = worksheet.Cells[row + 2, colIndex];
                
                // Handle date formatting
                if (value is DateTime dateValue)
                {
                    cell.Value = dateValue;
                    cell.Style.Numberformat.Format = "dd-MMM-yyyy";
                }
                else if (value != null && Nullable.GetUnderlyingType(value.GetType()) == typeof(DateTime))
                {
                    DateTime? nullableDate = (DateTime?)value;
                    if (nullableDate.HasValue)
                    {
                        cell.Value = nullableDate.Value;
                        cell.Style.Numberformat.Format = "dd-MMM-yyyy";
                    }
                    else
                    {
                        cell.Value = DBNull.Value;
                    }
                }
                else
                {
                    cell.Value = value ?? DBNull.Value;
                }
                
                colIndex++;
            }
        }

        // Auto-fit columns
        worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

        return package.GetAsByteArray();
    }
    private DataTable ToDataTable<T>(List<T> items, string[] columnsToExport)
    {
        var dataTable = new DataTable(typeof(T).Name);
        var props = typeof(T).GetProperties();

        // Only add columns that are in our export list
        foreach (var prop in props)
        {
            if (columnsToExport.Contains(prop.Name))
            {
                // Skip navigation properties even if they're in the list
                if (!(prop.PropertyType.IsClass && prop.PropertyType != typeof(string)))
                {
                    dataTable.Columns.Add(prop.Name);
                }
            }
        }

        foreach (var item in items)
        {
            var values = new object[dataTable.Columns.Count];
            var valueIndex = 0;

            foreach (var prop in props)
            {
                if (columnsToExport.Contains(prop.Name))
                {
                    // Only add if it's not a navigation property
                    if (!(prop.PropertyType.IsClass && prop.PropertyType != typeof(string)))
                    {
                        values[valueIndex] = prop.GetValue(item, null) ?? DBNull.Value;
                        valueIndex++;
                    }
                }
            }

            dataTable.Rows.Add(values);
        }

        return dataTable;
    }
}
