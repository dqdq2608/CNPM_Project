using System.Diagnostics;
using System.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel.Embeddings;
using Pgvector;

namespace eShop.Catalog.API.Services;

public sealed class CatalogAI : ICatalogAI
{
    private const int EmbeddingDimensions = 384;
    private readonly ITextEmbeddingGenerationService? _embeddingGenerator;

    private readonly IWebHostEnvironment _environment;
    private readonly ILogger _logger;

    public CatalogAI(IWebHostEnvironment environment, ILogger<CatalogAI> logger, ITextEmbeddingGenerationService? embeddingGenerator = null)
    {
        _embeddingGenerator = embeddingGenerator;
        _environment = environment;
        _logger = logger;
    }

    public bool IsEnabled => _embeddingGenerator is not null;

    public ValueTask<Vector?> GetEmbeddingAsync(CatalogItem item) =>
        IsEnabled
            ? GetEmbeddingAsync(CatalogItemToString(item))
            : ValueTask.FromResult<Vector?>(null);

    public async ValueTask<IReadOnlyList<Vector>?> GetEmbeddingsAsync(IEnumerable<CatalogItem> items)
    {
        if (!IsEnabled)
            return null;

        long timestamp = Stopwatch.GetTimestamp();

        // SK trả về ReadOnlyMemory<float>; Pgvector.Vector cần float[]
        var texts = items.Select(CatalogItemToString).ToList();
        var embeddings = await _embeddingGenerator!.GenerateEmbeddingsAsync(texts); // IList<ReadOnlyMemory<float>>
        var results = embeddings
            .Select(m => new Vector(m.Span[0..EmbeddingDimensions].ToArray()))
            .ToList();

        if (_logger.IsEnabled(LogLevel.Trace))
        {
            _logger.LogTrace("Generated {EmbeddingsCount} embeddings in {ElapsedSeconds}s",
                results.Count, Stopwatch.GetElapsedTime(timestamp).TotalSeconds);
        }

        return results;
    }

    public async ValueTask<Vector?> GetEmbeddingAsync(string text)
    {
        if (!IsEnabled)
            return null;

        long timestamp = Stopwatch.GetTimestamp();

        var embedding = await _embeddingGenerator!.GenerateEmbeddingAsync(text); // ReadOnlyMemory<float>
        var slice = embedding.Span[0..EmbeddingDimensions].ToArray();
        var vector = new Vector(slice);

        if (_logger.IsEnabled(LogLevel.Trace))
        {
            _logger.LogTrace("Generated embedding in {ElapsedSeconds}s: '{Text}'",
                Stopwatch.GetElapsedTime(timestamp).TotalSeconds, text);
        }

        return vector;
    }

    private static string CatalogItemToString(CatalogItem item) => $"{item.Name} {item.Description}";
}
