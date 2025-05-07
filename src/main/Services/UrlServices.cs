using Repository;
using InterfaceService;
using NanoidDotNet;
using Model;
using InterfaceRepository;

namespace Service
{
    public class UrlService : IUrlService
    {
        private readonly IUrlRepository _iurlRepository;        

        public UrlService(UrlRepository urlRepository)
        {
            _iurlRepository = urlRepository;
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

            await _iurlRepository.AddAsync(modelUrl, ct);
            await _iurlRepository.SaveAsync(ct);

            return modelUrl;
        }

        public async Task<string?> GetOriginalUrlAsync(string shortUrl, CancellationToken ct)
        {
            var modelUrl = await _iurlRepository.FindOriginalAsync(shortUrl, ct);
            return modelUrl?.OriginalUrl;
        }
    }
}