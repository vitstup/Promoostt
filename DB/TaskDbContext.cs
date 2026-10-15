using Microsoft.EntityFrameworkCore;
using PromoosTT.Models;

namespace PromoosTT.DB;

public class TaskDbContext : DbContext
{
    public TaskDbContext(DbContextOptions options) : base(options)
    {

    }

    public DbSet<TaskItem> tasks { get; set; }

    protected override void OnModelCreating(ModelBuilder mb)
    {
        mb.Entity<TaskItem>()
          .Property(t => t.Version)
          .IsRowVersion();
    }
}