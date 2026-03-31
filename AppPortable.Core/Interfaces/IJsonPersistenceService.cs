namespace AppPortable.Core.Interfaces;

public interface IJsonPersistenceService
{
    Task SaveAsync<T>(string path, T data, CancellationToken cancellationToken = default);
    Task<T?> LoadAsync<T>(string path, CancellationToken cancellationToken = default);
}
