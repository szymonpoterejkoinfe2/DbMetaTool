namespace DbMetaTool.Models
{
    public record TableData
    (
        string Name,
        List<FieldData> Fields
    );
}
