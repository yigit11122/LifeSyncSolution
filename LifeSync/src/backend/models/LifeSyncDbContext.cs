using Microsoft.EntityFrameworkCore;

namespace backend.models
{
    public class LifeSyncDbContext : DbContext
    {
        public LifeSyncDbContext(DbContextOptions<LifeSyncDbContext> options) : base(options) { }

        public DbSet<TaskItem> Tasks { get; set; }
        public DbSet<Event> Events { get; set; }
        public DbSet<Note> Notes { get; set; }
        // Düzeltme: OAuthTokens DbSet'i eklendi
        public DbSet<OAuthToken> OAuthTokens { get; set; }

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

            // Düzeltme: OAuthTokens tablosu için yapılandırma eklendi
            modelBuilder.Entity<OAuthToken>(entity =>
            {
                entity.ToTable("OAuthTokens", "public");
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

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
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

    // Düzeltme: OAuthToken modeli eklendi
    public class OAuthToken
    {
        public Guid Id { get; set; }
        public string? Source { get; set; } // Düzeltme: null olabilir
        public string? AccessToken { get; set; } // Düzeltme: null olabilir
        public string? RefreshToken { get; set; } // Düzeltme: null olabilir
        public DateTime ExpiryDate { get; set; }
        public Guid UserId { get; set; }
    }
}