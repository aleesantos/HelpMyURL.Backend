using Repository;
using NanoidDotNet;
using Model;

namespace Service
{
    public class UrlServ
    {
        private UrlRepository _urlrepository;
        private static readonly Random _random = new Random();

        public UrlServ(UrlRepository urlRepository)
        {
            _urlrepository = urlRepository;
        }

        public static string GenerateRandomUrl()
        {
            int length = _random.Next(5, 11);
            return Nanoid.Generate(size: length);
        }

        public ModelUrl ShortUrl(string UrlOriginal)
        {
            ModelUrl modelurl = new ModelUrl
            {
                OriginalUrl = UrlOriginal,
                ShortUrl = GenerateRandomUrl(),
                UrlCreat = DateTime.Now
            };

            _urlrepository.Add(modelurl);
            _urlrepository.Save();

            return modelurl;
        }

        public ModelUrl GetOriginal(string shortUrl)
        {
            var url = _urlrepository.FindOriginal(shortUrl);

            if (url == null)
                throw new KeyNotFoundException("URL does not exist in our records.");

            return url;
        }
    }
}