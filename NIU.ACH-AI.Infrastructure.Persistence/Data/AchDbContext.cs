using Microsoft.EntityFrameworkCore;
using NIU.ACH_AI.Infrastructure.Persistence.Entities;

namespace NIU.ACH_AI.Infrastructure.Persistence.Data
{
    /// <summary>
    /// Entity Framework DbContext for ACH-AI database
    /// </summary>
    public class AchDbContext : DbContext
    {
        public AchDbContext(DbContextOptions<AchDbContext> options)
            : base(options)
        {
        }

        public DbSet<HypothesisEntity> Hypotheses { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<HypothesisEntity>(entity =>
            {
                entity.ToTable("HYPOTHESES");
                entity.HasKey(e => e.HypothesisId);
                entity.Property(e => e.HypothesisId).HasColumnName("hypothesis_id");
                entity.Property(e => e.StepExecutionId).HasColumnName("step_execution_id");
                entity.Property(e => e.ShortTitle).HasColumnName("short_title").HasMaxLength(200);
                entity.Property(e => e.HypothesisText).HasColumnName("hypothesis_text");
                entity.Property(e => e.IsRefined).HasColumnName("is_refined");
                entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            });
        }
    }
}
