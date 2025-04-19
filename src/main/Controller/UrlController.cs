using Microsoft.AspNetCore.Mvc;
using Service;
using Response;

namespace Controllers
{
    [ApiController]
    [Route("ShortenUrl")]
    public class UrlController : ControllerBase
    {
        private UrlServ _urlservice;

        public UrlController(UrlServ urlservice)
        {
            _urlservice = urlservice;
        }

        [HttpPost]
        public IActionResult CreatShotenUrl([FromBody] UrlRequest request)
        {
            if(string.IsNullOrWhiteSpace(request.UrlOriginal) ||
               !Uri.TryCreate(request.UrlOriginal,UriKind.Absolute, out _))
                {
                return BadRequest("Url invalida");
                }

            string? userId;
            if(Request.Cookies.ContainsKey("userId"))
            {
                userId = Request.Cookies["userId"];
            }
            else
            {
                userId = Guid.NewGuid().ToString();

                Response.Cookies.Append("userId", userId, new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.Strict,
                    Expires = DateTimeOffset.UtcNow.AddDays(30)
                });
            }

            try
            {
                var url = _urlservice.ShortUrl(request.UrlOriginal, userId);
                var baseurl = $"{Request.Scheme}://{Request.Host}/r/";
                string urlRedirect = $"{baseurl}{_urlservice.ShortUrl}";

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

        public class UrlRequest
        {
            public string? UrlOriginal { get; set; }

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
}