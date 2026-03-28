namespace DocIndexService.Core.Enums;

public enum IngestionJobType
{
    ScanIncremental = 0,
    ScanFullReconciliation = 1,
    ExtractText = 2,
    ChunkDocument = 3,
    GenerateEmbedding = 4,
    IndexDocument = 5,
    ReprocessDocument = 6
}
