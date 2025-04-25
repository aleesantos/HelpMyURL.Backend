using Repository;
using InterfaceService;
using NanoidDotNet;
using Model;

namespace Service
{
    public class UrlService : IUrlService
    {
        private readonly UrlRepository _urlRepository;        

        public UrlService(UrlRepository urlRepository)
        {
            _urlRepository = urlRepository;
        }

        public async Task<ModelUrl> ShortenUrlAsync(string originalUrl, string userId, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(originalUrl))
                throw new ArgumentNullException("Original URL cannot be empty");

            var shortUrl = Nanoid.Generate(size: 8);

            var modelUrl = new ModelUrl
            {
                OriginalUrl = originalUrl,
                ShortUrl = shortUrl,
                UserId = userId,
                UrlCreat = DateTime.UtcNow,
            };

            await _urlRepository.AddAsync(modelUrl, ct);
            await _urlRepository.SaveChangesAsync(ct);

            return modelUrl;
        }

        public async Task<string?> GetOriginalUrlAsync(string shortUrl, CancellationToken ct)
        {
            var modelUrl = await _urlRepository.FindOriginalAsync(shortUrl, ct);
            return modelUrl?.OriginalUrl;
        }
    }
}