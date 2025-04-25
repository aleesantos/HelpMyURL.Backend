using Model;

namespace InterfaceRepository
{
    public interface IUrlRepository
    {
        Task AddAsync(ModelUrl modelUrl, CancellationToken ct);
        Task SaveAsync(CancellationToken ct);
        Task<ModelUrl?> FindOriginalAsync(string shortUrl, CancellationToken ct);
    }
}