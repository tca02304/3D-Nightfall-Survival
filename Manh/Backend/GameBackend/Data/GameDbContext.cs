using GameBackend.Models;
using Microsoft.EntityFrameworkCore;

namespace GameBackend.Data
{
    public class GameDbContext : DbContext
    {
        public GameDbContext(DbContextOptions<GameDbContext> options)
            : base(options)
        {
        }

        public DbSet<Item> Items { get; set; }
    }
}
