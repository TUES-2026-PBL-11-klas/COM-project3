namespace Data.CleanMap;

public sealed class CleanMapMongoOptions
{
    public const string SectionName = "CleanMapDatabase";

    public string ConnectionString { get; set; } = "mongodb://localhost:27017";
    public string DatabaseName { get; set; } = "nexora";
    public string ReportsCollectionName { get; set; } = "reports";
}
