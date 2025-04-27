using Microsoft.EntityFrameworkCore;

namespace backend.models
{
    public class LifeSyncDbContext : DbContext
    {
        public LifeSyncDbContext(DbContextOptions<LifeSyncDbContext> options) : base(options) { }

        public DbSet<TaskItem> Tasks { get; set; }
        public DbSet<Event> Events { get; set; }
        public DbSet<Note> Notes { get; set; }
        public DbSet<OAuthToken> OAuthTokens { get; set; }

        // 🔥 Yeni eklenenler:
        public DbSet<Habit> Habits { get; set; }
        public DbSet<AITaskSuggestion> AITaskSuggestions { get; set; }
        public DbSet<AILog> AILogs { get; set; }
        public DbSet<Reminder> Reminders { get; set; }
        public DbSet<UserSetting> UserSettings { get; set; }
        public DbSet<TaskTag> TaskTags { get; set; }
        public DbSet<TaskTagRelation> TaskTagRelations { get; set; }

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

            modelBuilder.Entity<OAuthToken>(entity =>
            {
                entity.ToTable("OAuthTokens", "public");
            });

            // 🔥 Yeni eklenen tablolar:
            modelBuilder.Entity<Habit>(entity =>
            {
                entity.ToTable("Habits", "public");
            });

            modelBuilder.Entity<AITaskSuggestion>(entity =>
            {
                entity.ToTable("AITaskSuggestions", "public");
            });

            modelBuilder.Entity<AILog>(entity =>
            {
                entity.ToTable("AI_Logs", "public");
            });

            modelBuilder.Entity<Reminder>(entity =>
            {
                entity.ToTable("Reminders", "public");
            });

            modelBuilder.Entity<UserSetting>(entity =>
            {
                entity.ToTable("UserSettings", "public");
            });

            modelBuilder.Entity<TaskTag>(entity =>
            {
                entity.ToTable("TaskTags", "public");
            });

            modelBuilder.Entity<TaskTagRelation>(entity =>
            {
                entity.ToTable("Task_Tag_Relation", "public");
                entity.HasKey(t => new { t.TaskId, t.TagId }); // Composite Key ekledim
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

    public class OAuthToken
    {
        public Guid Id { get; set; }
        public string? Source { get; set; }
        public string? AccessToken { get; set; }
        public string? RefreshToken { get; set; }
        public DateTime ExpiryDate { get; set; }
        public Guid UserId { get; set; }
    }

    // 🔥 Yeni eklenen model class'lar:

    public class Habit
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = "";
        public string Frequency { get; set; } = "";
        public int TargetCount { get; set; }
        public int CurrentCount { get; set; }
        public Guid? UserId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    public class AITaskSuggestion
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = "";
        public string Category { get; set; } = "";
        public string Description { get; set; } = "";
        public string Priority { get; set; } = "";
        public string EstimatedTime { get; set; } = "";
        public Guid? UserId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public bool Accepted { get; set; } = false;
    }

    public class AILog
    {
        public Guid Id { get; set; }
        public string Feature { get; set; } = "";
        public string InputData { get; set; } = "";
        public string OutputData { get; set; } = "";
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public Guid? UserId { get; set; }
    }

    public class Reminder
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = "";
        public DateTime ScheduledAt { get; set; }
        public Guid? UserId { get; set; }
    }

    public class UserSetting
    {
        public Guid Id { get; set; }
        public string? Theme { get; set; }
        public bool NotificationEnabled { get; set; } = true;
        public string Language { get; set; } = "en";
        public Guid? UserId { get; set; }
    }

    public class TaskTag
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = "";
        public Guid? UserId { get; set; }
    }

    public class TaskTagRelation
    {
        public Guid TaskId { get; set; }
        public Guid TagId { get; set; }
    }
}
