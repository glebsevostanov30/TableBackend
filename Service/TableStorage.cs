using System.Data;
using TableBackend.service;

namespace TableBackend.Service;

public class TableStorage : ITableStorage
{
    public DataTable Table { get; }

    public TableStorage()
    {
        Table = new DataTable();
        Table.Columns.Add("Имя", typeof(string));
        Table.Columns.Add("Фамилия", typeof(string));
        // Заполним 1 000 000 строк тестовыми данными
        for (int i = 0; i < 1_000_000; i++)
        {
            Table.Rows.Add($"Имя {i+1}", $"Фамилия {i+1}");
        }
    }

    public Task UpdateCell(int row, string colName, object value)
    {
        if (row < Table.Rows.Count && Table.Columns.Contains(colName))
        {
            Table.Rows[row][colName] = value;
        }
        return Task.CompletedTask;
    }
}