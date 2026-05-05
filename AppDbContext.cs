using AuthAPI.Entities;
using Microsoft.EntityFrameworkCore;

namespace AuthAPI
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options) { }

        public DbSet<Customer> Customers => Set<Customer>();
        public DbSet<User> Users => Set<User>();
        public DbSet<PhoneOtp> PhoneOtps => Set<PhoneOtp>();

        public DbSet<Todo> Todos => Set<Todo>();
    }
}
