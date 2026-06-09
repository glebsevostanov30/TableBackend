using System.Data;
using Microsoft.AspNetCore.Mvc;
using TableBackend.Dto;
using TableBackend.service;

namespace TableBackend.Controller;

[ApiController]
[Route("Api/[controller]/[action]")]
public class TableController(ITableStorage storage) : ControllerBase
{
    [HttpGet]
    public IActionResult Info()
    {
        var cols = storage.Table.Columns.Cast<DataColumn>()
            .Select(c => new { c.ColumnName, DataType = c.DataType.Name });
        return Ok(new { columns = cols, totalRows = storage.Table.Rows.Count });
    }

    [HttpGet]
    public IActionResult Columns(string name)
    {
        var cols = storage.Table.Columns.Cast<DataColumn>()
            .Select(c => new { title = c.ColumnName, id = c.ColumnName });
        return Ok(cols);
    }
    
    [HttpGet("Count")]
    public IActionResult Columns()
    {
        var cols = storage.Table.Columns.Count;
        return Ok(cols);
    }

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

    [HttpPut]
    public async Task<IActionResult> UpdateCell([FromBody] CellUpdateDto dto)
    {
        await storage.UpdateCell(dto.Row, dto.ColumnName, dto.Value);
        return Ok();
    }
}