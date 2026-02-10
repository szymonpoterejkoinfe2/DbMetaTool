namespace DbMetaTool.Models
{
    public class ProcedureData
    {
        public string Name { get; init; }
        public string Source { get; init; }
        public List<ParameterData> Parameters { get; init; } = new();

        public ProcedureData(string Name, string Source)
        {
            this.Name = Name;
            this.Source = Source;
        }

    };
}
