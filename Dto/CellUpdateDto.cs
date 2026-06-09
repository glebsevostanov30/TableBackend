namespace TableBackend.Dto;

public class CellUpdateDto
{
    public int Row { get; set; }
    public string ColumnName { get; set; } = "";
    public object Value { get; set; } = "";
}