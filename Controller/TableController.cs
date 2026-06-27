using System.Data;
using Microsoft.AspNetCore.Mvc;
using TableBackend.Dto;
using TableBackend.service;

namespace TableBackend.Controller;

[ApiController]
[Route("Api/[controller]/[action]")]
public class TableController(ITableStorage storage) : ControllerBase
{
    [HttpPut]
    public async Task<IActionResult> UpdateCell([FromBody] CellUpdateDto dto)
    {
        await storage.UpdateCell(dto.Row, dto.ColumnName, dto.Value);
        return Ok();
    }
}