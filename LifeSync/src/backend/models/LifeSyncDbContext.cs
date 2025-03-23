using Microsoft.EntityFrameworkCore;

namespace backend.models
{
    public class LifeSyncDbContext : DbContext
    {
        public LifeSyncDbContext(DbContextOptions<LifeSyncDbContext> options) : base(options) { }

        public DbSet<TaskItem> Tasks { get; set; }
        public DbSet<Event> Events { get; set; }
        public DbSet<Note> Notes { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<TaskItem>(entity =>
            {
                entity.ToTable("Tasks", "public");
            });

            modelBuilder.Entity<Event>(entity =>
            {
                entity.ToTable("Events", "public");
            });

            modelBuilder.Entity<Note>(entity =>
            {
                entity.ToTable("Notes", "public");
            });
        }
    }

    public class TaskItem
    {
        public Guid Id { get; set; }
        public string Content { get; set; } = "";
        public DateTime? DueDate { get; set; }
        public bool Completed { get; set; } = false;
        public string Source { get; set; } = "";
        public Guid? UserId { get; set; }
    }

    public class Event
    {
        public Guid Id { get; set; }
        public string Summary { get; set; } = "";
        public DateTime StartDate { get; set; }
        public string Source { get; set; } = "";
        public Guid UserId { get; set; }
    }

    public class Note
    {
        public Guid Id { get; set; }
        public string Content { get; set; } = "";
        public DateTime CreatedAt { get; set; }
        public string Source { get; set; } = "";
        public Guid UserId { get; set; }
    }
}
