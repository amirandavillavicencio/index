using AppPortable.Core.Models;
using AppPortable.Infrastructure.Services;

namespace AppPortable.Tests;

public sealed class JsonPersistenceServiceTests
{
    [Fact]
    public async Task SaveAndLoadAsync_RoundTripsDocument()
    {
        var path = Path.Combine(Path.GetTempPath(), $"json_{Guid.NewGuid():N}.json");
        var service = new JsonPersistenceService();
        var input = new ProcessedDocument
        {
            DocumentId = "abc",
            SourceFile = "test.pdf",
            TotalPages = 1
        };

        await service.SaveAsync(path, input);
        var output = await service.LoadAsync<ProcessedDocument>(path);

        Assert.NotNull(output);
        Assert.Equal("abc", output!.DocumentId);
        Assert.Equal("test.pdf", output.SourceFile);
    }
}
