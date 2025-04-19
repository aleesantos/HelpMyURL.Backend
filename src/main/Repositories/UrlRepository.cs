using Context;
using IUrlRepository;
using Microsoft.EntityFrameworkCore;
using Model;

namespace Repository
{
    public class UrlRepository : IUrlRepo
    {

        private readonly AppDbContext _context;

        public UrlRepository(AppDbContext context)
        {
            _context = context;
        }

        public void Add(ModelUrl modelurl)
        {
            _context.UrlTable.Add(modelurl);
        }

        public void Save()
        {
            _context.SaveChanges();    
        }

        public ModelUrl? FindOriginal(string shortUrl)
        {
            if (string.IsNullOrWhiteSpace(shortUrl))
                return null;

            return _context.UrlTable.AsNoTracking().FirstOrDefault(url => url.ShortUrl == shortUrl);
        }
    }
}