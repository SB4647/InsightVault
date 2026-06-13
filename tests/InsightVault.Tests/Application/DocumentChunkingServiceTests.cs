using InsightVault.Application.Features.Documents.Processing;

namespace InsightVault.Tests.Application;

public class DocumentChunkingServiceTests
{
    [Fact]
    public void Chunk_WithShortText_ReturnsSingleChunk()
    {
        var service = new DocumentChunkingService();

        var chunks = service.Chunk("This is a short document.", chunkSize: 100, overlapSize: 20);

        var chunk = Assert.Single(chunks);
        Assert.Equal(0, chunk.ChunkIndex);
        Assert.Equal("This is a short document.", chunk.Text);
    }

    [Fact]
    public void Chunk_WithLongText_ReturnsOverlappingChunksInOrder()
    {
        var service = new DocumentChunkingService();
        var text = string.Join(' ', Enumerable.Range(1, 30).Select(number => $"word{number}"));

        var chunks = service.Chunk(text, chunkSize: 50, overlapSize: 10);

        Assert.True(chunks.Count > 1);
        Assert.Equal(Enumerable.Range(0, chunks.Count), chunks.Select(chunk => chunk.ChunkIndex));
        Assert.All(chunks, chunk => Assert.False(string.IsNullOrWhiteSpace(chunk.Text)));
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(100, 100)]
    [InlineData(100, 101)]
    public void Chunk_WithInvalidSettings_ThrowsArgumentException(int chunkSize, int overlapSize)
    {
        var service = new DocumentChunkingService();

        Assert.Throws<ArgumentException>(() => service.Chunk("content", chunkSize, overlapSize));
    }
}
