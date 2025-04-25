using Microsoft.AspNetCore.Mvc;
using Service;
using Response;

namespace Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UrlController : ControllerBase
    {
        private readonly UrlService _urlservice;
        private readonly ILogger<UrlController> _logger;

        public UrlController(UrlService urlservice, ILogger<UrlController> logger)
        {
            _urlservice = urlservice;
            _logger = logger;
        }

        [HttpPost]
        public async Task<IActionResult> CreatShotenUrl(
            [FromBody] UrlRequest request,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.UrlOriginal) ||
               !Uri.TryCreate(request.UrlOriginal, UriKind.Absolute, out var uri) ||
               (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttp))
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
                
                var url = await _urlservice.ShortenUrlAsync(request.UrlOriginal, userId);
                var baseurl = $"{Request.Scheme}://{Request.Host}/r/";
                string urlRedirect = $"{baseurl}{_urlservice.ShortenUrlAsync}";

                var response = new UrlResponse(
                    url.Id,
                    url.OriginalUrl,
                    urlRedirect,
                    url.UrlCreat
                    );

                return Ok(response);
            }
            catch
            {
                return StatusCode(500, "Error generating shortened URL");
            }
        }       

        private string GetOrCreateUserId()
        {
            if (Request.Cookies.TryGetValue("userId", out var userId))
                return userId;
            userId = Guid.NewGuid().ToString();

            userId = Guid.NewGuid().ToString();
            Response.Cookies.Append("userId", userId, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = DateTimeOffset.UtcNow.AddDays(30)
            });
        }

        

        

        [HttpGet("ShortUrl")]
        public IActionResult UrlRedirect(string shorturl)
        {
            try
            {
                var url = _urlservice.GetOriginal(shorturl);

                if (url == null)
                {
                    return NotFound("Url not found");
                }

                return Redirect(url.OriginalUrl);
            }
            catch
            {
                return StatusCode(500, "Internal error while trying to redirect.");
            }
        }
    }
    
    public class UrlRequest
    {
        [Required]
        public string? UrlOriginal { get; set; }
    }

    
}