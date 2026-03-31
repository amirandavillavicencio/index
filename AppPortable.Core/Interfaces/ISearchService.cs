using AppPortable.Core.Models;

namespace AppPortable.Core.Interfaces;

public interface ISearchService
{
    Task<IReadOnlyList<SearchResult>> SearchAsync(string query, int limit = 50, CancellationToken cancellationToken = default);
}
