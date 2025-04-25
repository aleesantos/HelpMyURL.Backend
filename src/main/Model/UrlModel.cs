using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Globalization;

namespace Model
{
    [Table("UrlUser")]
    public class ModelUrl
    {
        [Required]
        public required string UserId { get; set; }

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [MaxLength(2048)]
        [Column(TypeName = "varchar(2048)")]
        public required string OriginalUrl { get; set; }

        [Required]
        [MaxLength(10)]
        [Column(TypeName = "varchar(10)")]
        public required string ShortUrl { get; set; }

        [Required]
        public required DateTime UrlCreat { get; set; } = DateTime.UtcNow;

    }
}