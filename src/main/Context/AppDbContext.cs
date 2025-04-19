using Microsoft.EntityFrameworkCore;
using Model;

namespace Context
{
    public class AppDbContext : DbContext
    {
        public DbSet<ModelUrl> UrlTable { get; set; }

        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
    }
}