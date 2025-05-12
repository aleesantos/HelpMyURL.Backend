using Microsoft.AspNetCore.Mvc;
using Service;
using Response;
using System.ComponentModel.DataAnnotations;

namespace Controllers
{
    [ApiController]
    [Route("r")]
    public class UrlController : ControllerBase
    {
        private readonly UrlService _urlService;
        private readonly ILogger<UrlController> _logger;

        public UrlController(UrlService urlService, ILogger<UrlController> logger)
        {
            _urlService = urlService;
            _logger = logger;
        }

        [HttpPost("creat")]
        public async Task<IActionResult> CreateShortenUrl(
            [FromBody] UrlRequest request,
            CancellationToken ct)
        {
            if (!Uri.TryCreate(request.UrlOriginal, UriKind.Absolute, out var uri) ||
               !(uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
            {
                _logger.LogWarning("Invalid URL received: {Url}", request.UrlOriginal);
                return BadRequest(new ProblemDetails
                {
                    Title = "Invalid URL",
                    Detail = "Use an absolute URL with http:// or https://"
                });
            }

            var userId = GetOrCreateUserId();
            try
            {
                
                var shortenedUrl = await _urlService.ShortenUrlAsync(request.UrlOriginal, userId, ct);

                var urlRedirect = $"{Request.Scheme}://{Request.Host}/r/{shortenedUrl.ShortUrl}";

                var response = new UrlResponse(
                    shortenedUrl.Id,
                    shortenedUrl.OriginalUrl,
                    urlRedirect,
                    shortenedUrl.UrlCreat
                    );

                return Ok(response);
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, "Error to short LINK");
                return StatusCode(500, new ProblemDetails
                {
                    Title = "Internal error",
                    Detail = "An error occurred while processing your request"
                });
            }
        }

        [HttpGet("{shortUrl}")]
        public async Task<IActionResult> RedirectToOriginal(
            [FromRoute] string shortUrl,
            CancellationToken ct)
        {
            try
            {
                var originalUrl = await _urlService.GetOriginalUrlAsync(shortUrl, ct);
                if(string.IsNullOrEmpty(originalUrl))
                {
                    return NotFound(new ProblemDetails { Title = "Url not found" });
                }

                return Redirect(originalUrl);
            }
            catch (Exception ex) 
            {
                _logger.LogError(ex, "Error to redirect");
                return StatusCode(500, new ProblemDetails
                {
                    Title = "Internal error",
                    Detail = "Error to redirect"
                });
            }
        }

        private string GetOrCreateUserId()
        {
            if (Request.Cookies.TryGetValue("userId", out var userId))
                return userId;            

            userId = Guid.NewGuid().ToString();
            Response.Cookies.Append("userId", userId, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = DateTimeOffset.UtcNow.AddDays(30)
            });

            return userId;
        }              
    }
    
    public class UrlRequest
    {
        [Required]
        public string? UrlOriginal { get; set; }
    }    
}