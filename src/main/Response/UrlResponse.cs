using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace Response
{
        public record UrlResponse(
            int id,
            string OriginalUrl,
            string ShortUrl,
            DateTime UrlCreat);
}