using Repository;
using InterfaceService;
using NanoidDotNet;
using Model;
using InterfaceRepository;

namespace Service
{
    public class UrlService : IUrlService
    {
        private readonly IUrlRepository _urlRepository;        

        public UrlService(IUrlRepository urlRepository)
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
            await _urlRepository.SaveAsync(ct);

            return modelUrl;
        }

        public async Task<string?> GetOriginalUrlAsync(string shortUrl, CancellationToken ct)
        {
            var modelUrl = await _urlRepository.FindOriginalAsync(shortUrl, ct);
            return modelUrl?.OriginalUrl;
        }
    }
}