using Context;
using InterfaceRepository;
using Microsoft.EntityFrameworkCore;
using Model;

namespace Repository
{
    public class UrlRepository : IUrlRepository
    {

        private readonly AppDbContext _context;

        public UrlRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(ModelUrl modelUrl, CancellationToken ct)
        {
            await _context.UrlTable.AddAsync(modelUrl, ct);
        }

        public async Task SaveChangesAsync(CancellationToken ct)
        {
            await _context.SaveChangesAsync(ct);
        }

        public async Task<ModelUrl?> FindOriginalAsync(string shortUrl, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(shortUrl))
                return null;

            return await _context.UrlTable.
                AsNoTracking().
                FirstOrDefaultAsync(url => url.ShortUrl == shortUrl, ct);
        }
    }
}