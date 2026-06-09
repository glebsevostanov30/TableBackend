using System.Data;

namespace TableBackend.service;

public interface ITableStorage
{
    DataTable Table { get; }
    Task UpdateCell(int row, string colName, object value);
}

