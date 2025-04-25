using Model;

namespace InterfaceService
{
    public interface IUrlService
    {
        Task<ModelUrl> ShortenUrlAsync(
            string originalurl,
            string userId,
            CancellationToken ct);

        Task<string?> GetOriginalUrlAsync(
            string shortUrl,
            CancellationToken ct);
    }
}