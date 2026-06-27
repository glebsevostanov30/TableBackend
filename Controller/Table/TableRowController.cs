using System.Data;
using Microsoft.AspNetCore.Mvc;
using TableBackend.service;

namespace TableBackend.Controller.Table;

[ApiController]
[Route("Api/Table/Rows")]
public class RowController(ITableStorage storage) : ControllerBase
{

    [HttpGet]
    public IActionResult Rows([FromQuery] int skip = 0, [FromQuery] int take = 100)
    {
        var rows = new List<Dictionary<string, object>>();
        for (var i = skip; i < Math.Min(skip + take, storage.Table.Rows.Count); i++)
        {
            var rowDict = new Dictionary<string, object>();
            foreach (DataColumn col in storage.Table.Columns)
            {
                rowDict[col.ColumnName] = storage.Table.Rows[i][col];
            }

            rows.Add(rowDict);
        }

        return Ok(rows);
    }
    
    [HttpGet("Count")]
    public IActionResult RowsCount()
    {
        var cols = storage.Table.Rows.Count;
        return Ok(new { count = cols });
    }
}