using System.Data;
using Microsoft.AspNetCore.Mvc;
using TableBackend.service;

namespace TableBackend.Controller.Table;

[ApiController]
[Route("Api/Table/Columns")]
public class ColumnsController(ITableStorage storage) : ControllerBase
{
    [HttpGet]
    public IActionResult Columns()
    {
        var cols = storage.Table.Columns.Cast<DataColumn>()
            .Select(c => new { title = c.ColumnName, id = c.ColumnName });
        return Ok(cols);
    }
}