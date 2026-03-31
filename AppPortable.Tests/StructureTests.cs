using Xunit;

namespace AppPortable.Tests;

public sealed class StructureTests
{
    [Fact]
    public void CoreRecord_CanBeCreated()
    {
        var record = new AppPortable.Core.DocumentRecord("1", "doc.txt");
        Assert.Equal("doc.txt", record.FileName);
    }
}
