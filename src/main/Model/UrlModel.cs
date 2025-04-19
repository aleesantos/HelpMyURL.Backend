using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Globalization;

namespace Model
{
    [Table("UrlUser")]
    public class ModelUrl
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [MaxLength(2048)]
        public required string OriginalUrl { get; set; }

        [Required]
        public required string UserId { get; set; }

        [Required]
        [MaxLength(20)]
        public required string ShortUrl { get; set; }

        [Required]
        public required DateTime UrlCreat { get; set; } = DateTime.UtcNow;

        public ModelUrl()
        {

        }

        public ModelUrl(string originalUrl, string shortUrl, string userId)
        {
            OriginalUrl = originalUrl ?? throw new ArgumentException(nameof(originalUrl));
            ShortUrl = shortUrl ?? throw new ArgumentException(nameof(shortUrl));
            UserId = userId ?? throw new ArgumentException(nameof(userId)); 
            UrlCreat = DateTime.UtcNow;
        }
    }
}