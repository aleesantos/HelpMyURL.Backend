using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace Response
{
    public class UrlResponse
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [MaxLength(2048)]
        public string OriginalUrl { get; set; }

        [MaxLength(20)]
        public string ShortUrl { get; set; }

        public DateTime UrlCreat { get; set; } = DateTime.UtcNow;

        public UrlResponse(int id, string originalUrl, string shortUrl, DateTime urlCreat)
        {
            Id = id;
            OriginalUrl = originalUrl ?? throw new ArgumentException(nameof(originalUrl));
            ShortUrl = shortUrl ?? throw new ArgumentException(nameof(shortUrl));
            UrlCreat = DateTime.UtcNow;
        }
    }
}