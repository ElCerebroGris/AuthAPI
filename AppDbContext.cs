using AuthAPI.Entities;
using Microsoft.EntityFrameworkCore;

namespace AuthAPI
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options) { }

        public DbSet<Todo> Todos => Set<Todo>();
    }
}
