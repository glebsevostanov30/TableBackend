using System.Data;
using Microsoft.AspNetCore.Mvc;
using TableBackend.service;

namespace TableBackend.Controller.Table;

[ApiController]
[Route("Api/Table/Cell")]
public class TableCellController(ITableStorage storage) : ControllerBase
{
    [HttpGet]
    public IActionResult Item(
        [FromQuery] int col,
        [FromQuery] int row)
    {
        var item = storage.Table.Rows[col][row];
        return Ok(
            new
            {
                kind = "Text",
                data = item,
                displayData = item,
                allowOverlay = true,
                @readonly = false
            });
    }
}