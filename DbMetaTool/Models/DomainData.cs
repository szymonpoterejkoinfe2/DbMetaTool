using Microsoft.VisualBasic.FileIO;

namespace DbMetaTool.Models
{
    public record DomainData
    (
        string Name,
        int FieldType,
        int Length
    );
}
