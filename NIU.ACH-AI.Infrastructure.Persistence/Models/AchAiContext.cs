using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace NIU.ACH_AI.Infrastructure.Persistence.Models;

public partial class AchAiContext : DbContext
{
    public AchAiContext()
    {
    }

    public AchAiContext(DbContextOptions<AchAiContext> options)
        : base(options)
    {
    }

    public virtual DbSet<AchStep> AchSteps { get; set; }

    public virtual DbSet<AgentConfiguration> AgentConfigurations { get; set; }

    public virtual DbSet<AgentResponse> AgentResponses { get; set; }

    public virtual DbSet<EvaluationScore> EvaluationScores { get; set; }

    public virtual DbSet<Evidence> Evidences { get; set; }

    public virtual DbSet<EvidenceHypothesisEvaluation> EvidenceHypothesisEvaluations { get; set; }

    public virtual DbSet<EvidenceType> EvidenceTypes { get; set; }

    public virtual DbSet<Experiment> Experiments { get; set; }

    public virtual DbSet<Hypothesis> Hypotheses { get; set; }

    public virtual DbSet<Model> Models { get; set; }

    public virtual DbSet<OrchestrationType> OrchestrationTypes { get; set; }

    public virtual DbSet<Provider> Providers { get; set; }

    public virtual DbSet<Scenario> Scenarios { get; set; }

    public virtual DbSet<StepExecution> StepExecutions { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseSqlServer("Server=TIM-DESKTOP-W11;Database=ach-ai;Trusted_Connection=True;TrustServerCertificate=True");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AchStep>(entity =>
        {
            entity.HasKey(e => e.AchStepId).HasName("PK__ACH_STEP__E2060DE228905C9F");

            entity.ToTable("ACH_STEPS");

            entity.HasIndex(e => e.PrimaryAchStep, "IX_ACH_STEPS_primary_ach_step");

            entity.HasIndex(e => e.StepOrder, "IX_ACH_STEPS_step_order");

            entity.HasIndex(e => e.StepName, "UQ_ACH_STEPS_step_name").IsUnique();

            entity.Property(e => e.AchStepId)
                .ValueGeneratedNever()
                .HasColumnName("ach_step_id");
            entity.Property(e => e.Description)
                .HasMaxLength(500)
                .HasColumnName("description");
            entity.Property(e => e.PrimaryAchStep).HasColumnName("primary_ach_step");
            entity.Property(e => e.StepName)
                .HasMaxLength(100)
                .HasColumnName("step_name");
            entity.Property(e => e.StepOrder).HasColumnName("step_order");
        });

        modelBuilder.Entity<AgentConfiguration>(entity =>
        {
            entity.HasKey(e => e.AgentConfigurationId).HasName("PK__AGENT_CO__71AE6C2E8463A9AA");

            entity.ToTable("AGENT_CONFIGURATIONS");

            entity.HasIndex(e => e.ModelId, "IX_AGENT_CONFIGURATIONS_model_id");

            entity.HasIndex(e => e.ProviderId, "IX_AGENT_CONFIGURATIONS_provider_id");

            entity.HasIndex(e => e.StepExecutionId, "IX_AGENT_CONFIGURATIONS_step_execution_id");

            entity.Property(e => e.AgentConfigurationId)
                .ValueGeneratedNever()
                .HasColumnName("agent_configuration_id");
            entity.Property(e => e.AgentName)
                .HasMaxLength(50)
                .HasColumnName("agent_name");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(sysutcdatetime())")
                .HasColumnName("created_at");
            entity.Property(e => e.Description)
                .HasMaxLength(500)
                .HasColumnName("description");
            entity.Property(e => e.Instructions).HasColumnName("instructions");
            entity.Property(e => e.ModelId).HasColumnName("model_id");
            entity.Property(e => e.ProviderId).HasColumnName("provider_id");
            entity.Property(e => e.StepExecutionId).HasColumnName("step_execution_id");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");

            entity.HasOne(d => d.Model).WithMany(p => p.AgentConfigurations)
                .HasForeignKey(d => d.ModelId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_AGENT_CONFIGURATIONS_MODELS");

            entity.HasOne(d => d.Provider).WithMany(p => p.AgentConfigurations)
                .HasForeignKey(d => d.ProviderId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_AGENT_CONFIGURATIONS_PROVIDERS");

            entity.HasOne(d => d.StepExecution).WithMany(p => p.AgentConfigurations)
                .HasForeignKey(d => d.StepExecutionId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_AGENT_CONFIGURATIONS_STEP_EXECUTIONS");
        });

        modelBuilder.Entity<AgentResponse>(entity =>
        {
            entity.HasKey(e => e.AgentResponseId).HasName("PK__AGENT_RE__918C63C63929D267");

            entity.ToTable("AGENT_RESPONSES");

            entity.HasIndex(e => e.AgentConfigurationId, "IX_AGENT_RESPONSES_agent_configuration_id");

            entity.HasIndex(e => e.CreatedAt, "IX_AGENT_RESPONSES_created_at");

            entity.HasIndex(e => e.StepExecutionId, "IX_AGENT_RESPONSES_step_execution_id");

            entity.Property(e => e.AgentResponseId)
                .ValueGeneratedNever()
                .HasColumnName("agent_response_id");
            entity.Property(e => e.AcceptedPredictionTokenCount).HasColumnName("accepted_prediction_token_count");
            entity.Property(e => e.AgentConfigurationId).HasColumnName("agent_configuration_id");
            entity.Property(e => e.AgentName)
                .HasMaxLength(50)
                .HasColumnName("agent_name");
            entity.Property(e => e.CachedInputTokenCount).HasColumnName("cached_input_token_count");
            entity.Property(e => e.CompletionId)
                .HasMaxLength(100)
                .HasColumnName("completion_id");
            entity.Property(e => e.Content).HasColumnName("content");
            entity.Property(e => e.ContentLength).HasColumnName("content_length");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(sysutcdatetime())")
                .HasColumnName("created_at");
            entity.Property(e => e.FinishedAt)
                .HasDefaultValueSql("(getutcdate())")
                .HasColumnName("finished_at");
            entity.Property(e => e.InputAudioTokenCount).HasColumnName("input_audio_token_count");
            entity.Property(e => e.InputTokenCount).HasColumnName("input_token_count");
            entity.Property(e => e.OutputAudioTokenCount).HasColumnName("output_audio_token_count");
            entity.Property(e => e.OutputTokenCount).HasColumnName("output_token_count");
            entity.Property(e => e.ReasoningTokenCount).HasColumnName("reasoning_token_count");
            entity.Property(e => e.RejectedPredictionTokenCount).HasColumnName("rejected_prediction_token_count");
            entity.Property(e => e.ResponseDuration).HasColumnName("response_duration");
            entity.Property(e => e.StepExecutionId).HasColumnName("step_execution_id");
            entity.Property(e => e.TurnNumber).HasColumnName("turn_number");

            entity.HasOne(d => d.AgentConfiguration).WithMany(p => p.AgentResponses)
                .HasForeignKey(d => d.AgentConfigurationId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_AGENT_RESPONSES_AGENT_CONFIGURATIONS");

            entity.HasOne(d => d.StepExecution).WithMany(p => p.AgentResponses)
                .HasForeignKey(d => d.StepExecutionId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_AGENT_RESPONSES_STEP_EXECUTIONS");
        });

        modelBuilder.Entity<EvaluationScore>(entity =>
        {
            entity.HasKey(e => e.EvaluationScoreId).HasName("PK__EVALUATI__A3546EFFED786DC4");

            entity.ToTable("EVALUATION_SCORES");

            entity.HasIndex(e => e.ScoreName, "IX_EVALUATION_SCORES_score_name");

            entity.Property(e => e.EvaluationScoreId).HasColumnName("evaluation_score_id");
            entity.Property(e => e.Description)
                .HasMaxLength(255)
                .HasColumnName("description");
            entity.Property(e => e.ScoreName)
                .HasMaxLength(50)
                .HasColumnName("score_name");
            entity.Property(e => e.ScoreValue).HasColumnName("score_value");
        });

        modelBuilder.Entity<Evidence>(entity =>
        {
            entity.HasKey(e => e.EvidenceId).HasName("PK__EVIDENCE__C59A788ED99DE0F9");

            entity.ToTable("EVIDENCE");

            entity.HasIndex(e => e.EvidenceTypeId, "IX_EVIDENCE_evidence_type_id");

            entity.HasIndex(e => e.StepExecutionId, "IX_EVIDENCE_step_execution_id");

            entity.Property(e => e.EvidenceId)
                .ValueGeneratedNever()
                .HasColumnName("evidence_id");
            entity.Property(e => e.Claim).HasColumnName("claim");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(sysutcdatetime())")
                .HasColumnName("created_at");
            entity.Property(e => e.EvidenceTypeId).HasColumnName("evidence_type_id");
            entity.Property(e => e.Notes).HasColumnName("notes");
            entity.Property(e => e.ReferenceSnippet).HasColumnName("reference_snippet");
            entity.Property(e => e.StepExecutionId).HasColumnName("step_execution_id");

            entity.HasOne(d => d.EvidenceType).WithMany(p => p.Evidences)
                .HasForeignKey(d => d.EvidenceTypeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_EVIDENCE_EVIDENCE_TYPES");

            entity.HasOne(d => d.StepExecution).WithMany(p => p.Evidences)
                .HasForeignKey(d => d.StepExecutionId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_EVIDENCE_STEP_EXECUTIONS");
        });

        modelBuilder.Entity<EvidenceHypothesisEvaluation>(entity =>
        {
            entity.HasKey(e => e.EvidenceHypothesisEvaluationId).HasName("PK__EVIDENCE__18393A922E86FD90");

            entity.ToTable("EVIDENCE_HYPOTHESIS_EVALUATIONS");

            entity.HasIndex(e => e.EvaluationScoreId, "IX_EHE_evaluation_score_id");

            entity.HasIndex(e => e.EvidenceId, "IX_EHE_evidence_id");

            entity.HasIndex(e => e.HypothesisId, "IX_EHE_hypothesis_id");

            entity.HasIndex(e => e.StepExecutionId, "IX_EHE_step_execution_id");

            entity.Property(e => e.EvidenceHypothesisEvaluationId)
                .ValueGeneratedNever()
                .HasColumnName("evidence_hypothesis_evaluation_id");
            entity.Property(e => e.ConfidenceRationale).HasColumnName("confidence_rationale");
            entity.Property(e => e.ConfidenceScore)
                .HasColumnType("decimal(5, 4)")
                .HasColumnName("confidence_score");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(sysutcdatetime())")
                .HasColumnName("created_at");
            entity.Property(e => e.EvaluationScoreId).HasColumnName("evaluation_score_id");
            entity.Property(e => e.EvidenceId).HasColumnName("evidence_id");
            entity.Property(e => e.HypothesisId).HasColumnName("hypothesis_id");
            entity.Property(e => e.Rationale).HasColumnName("rationale");
            entity.Property(e => e.StepExecutionId).HasColumnName("step_execution_id");

            entity.HasOne(d => d.EvaluationScore).WithMany(p => p.EvidenceHypothesisEvaluations)
                .HasForeignKey(d => d.EvaluationScoreId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_EHE_EVALUATION_SCORE");

            entity.HasOne(d => d.Evidence).WithMany(p => p.EvidenceHypothesisEvaluations)
                .HasForeignKey(d => d.EvidenceId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_EHE_EVIDENCE");

            entity.HasOne(d => d.Hypothesis).WithMany(p => p.EvidenceHypothesisEvaluations)
                .HasForeignKey(d => d.HypothesisId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_EHE_HYPOTHESIS");

            entity.HasOne(d => d.StepExecution).WithMany(p => p.EvidenceHypothesisEvaluations)
                .HasForeignKey(d => d.StepExecutionId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_EHE_STEP_EXECUTION");
        });

        modelBuilder.Entity<EvidenceType>(entity =>
        {
            entity.HasKey(e => e.EvidenceTypeId).HasName("PK__EVIDENCE__07203B4B5B5DD7EE");

            entity.ToTable("EVIDENCE_TYPES");

            entity.HasIndex(e => e.EvidenceTypeName, "UQ_EVIDENCE_TYPES_evidence_type_name").IsUnique();

            entity.Property(e => e.EvidenceTypeId).HasColumnName("evidence_type_id");
            entity.Property(e => e.Description)
                .HasMaxLength(255)
                .HasColumnName("description");
            entity.Property(e => e.EvidenceTypeName)
                .HasMaxLength(50)
                .HasColumnName("evidence_type_name");
        });

        modelBuilder.Entity<Experiment>(entity =>
        {
            entity.HasKey(e => e.ExperimentId).HasName("PK__EXPERIME__38C6E36F13B523CD");

            entity.ToTable("EXPERIMENTS");

            entity.HasIndex(e => e.CreatedAt, "IX_EXPERIMENTS_created_at");

            entity.HasIndex(e => e.ScenarioId, "IX_EXPERIMENTS_scenario_id");

            entity.Property(e => e.ExperimentId)
                .ValueGeneratedNever()
                .HasColumnName("experiment_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(sysutcdatetime())")
                .HasColumnName("created_at");
            entity.Property(e => e.Description)
                .HasMaxLength(500)
                .HasColumnName("description");
            entity.Property(e => e.ExperimentName)
                .HasMaxLength(50)
                .HasColumnName("experiment_name");
            entity.Property(e => e.Kiq)
                .HasMaxLength(255)
                .HasColumnName("kiq");
            entity.Property(e => e.ScenarioId).HasColumnName("scenario_id");

            entity.HasOne(d => d.Scenario).WithMany(p => p.Experiments)
                .HasForeignKey(d => d.ScenarioId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_EXPERIMENTS_SCENARIOS");
        });

        modelBuilder.Entity<Hypothesis>(entity =>
        {
            entity.HasKey(e => e.HypothesisId).HasName("PK__HYPOTHES__6261C85A5EF87E0E");

            entity.ToTable("HYPOTHESES");

            entity.HasIndex(e => e.IsRefined, "IX_HYPOTHESES_is_refined");

            entity.HasIndex(e => e.StepExecutionId, "IX_HYPOTHESES_step_execution_id");

            entity.Property(e => e.HypothesisId)
                .ValueGeneratedNever()
                .HasColumnName("hypothesis_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(sysutcdatetime())")
                .HasColumnName("created_at");
            entity.Property(e => e.HypothesisText).HasColumnName("hypothesis_text");
            entity.Property(e => e.IsRefined).HasColumnName("is_refined");
            entity.Property(e => e.ShortTitle)
                .HasMaxLength(200)
                .HasColumnName("short_title");
            entity.Property(e => e.StepExecutionId).HasColumnName("step_execution_id");

            entity.HasOne(d => d.StepExecution).WithMany(p => p.Hypotheses)
                .HasForeignKey(d => d.StepExecutionId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_HYPOTHESES_STEP_EXECUTIONS");
        });

        modelBuilder.Entity<Model>(entity =>
        {
            entity.HasKey(e => e.ModelId).HasName("PK__MODELS__DC39CAF4D60AD608");

            entity.ToTable("MODELS");

            entity.HasIndex(e => e.ModelName, "IX_MODELS_model_name");

            entity.HasIndex(e => e.ProviderId, "IX_MODELS_provider_id");

            entity.HasIndex(e => new { e.ProviderId, e.ModelName }, "UQ_MODELS_provider_model").IsUnique();

            entity.Property(e => e.ModelId)
                .ValueGeneratedNever()
                .HasColumnName("model_id");
            entity.Property(e => e.CachedInputTokenCost)
                .HasColumnType("decimal(12, 8)")
                .HasColumnName("cached_input_token_cost");
            entity.Property(e => e.InputTokenCost)
                .HasColumnType("decimal(12, 8)")
                .HasColumnName("input_token_cost");
            entity.Property(e => e.ModelName)
                .HasMaxLength(50)
                .HasColumnName("model_name");
            entity.Property(e => e.OutputTokenCost)
                .HasColumnType("decimal(12, 8)")
                .HasColumnName("output_token_cost");
            entity.Property(e => e.ProviderId).HasColumnName("provider_id");

            entity.HasOne(d => d.Provider).WithMany(p => p.Models)
                .HasForeignKey(d => d.ProviderId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_MODELS_PROVIDERS");
        });

        modelBuilder.Entity<OrchestrationType>(entity =>
        {
            entity.HasKey(e => e.OrchestrationTypeId).HasName("PK__ORCHESTR__32FE74E839E0D24C");

            entity.ToTable("ORCHESTRATION_TYPES");

            entity.Property(e => e.OrchestrationTypeId)
                .ValueGeneratedNever()
                .HasColumnName("orchestration_type_id");
            entity.Property(e => e.Description)
                .HasMaxLength(50)
                .HasColumnName("description");
        });

        modelBuilder.Entity<Provider>(entity =>
        {
            entity.HasKey(e => e.ProviderId).HasName("PK__PROVIDER__00E21310026518FA");

            entity.ToTable("PROVIDERS");

            entity.HasIndex(e => e.IsActive, "IX_PROVIDERS_is_active");

            entity.HasIndex(e => e.ProviderName, "IX_PROVIDERS_provider_name");

            entity.Property(e => e.ProviderId)
                .ValueGeneratedNever()
                .HasColumnName("provider_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(sysutcdatetime())")
                .HasColumnName("created_at");
            entity.Property(e => e.Description)
                .HasMaxLength(255)
                .HasColumnName("description");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("is_active");
            entity.Property(e => e.ProviderName)
                .HasMaxLength(50)
                .HasColumnName("provider_name");
        });

        modelBuilder.Entity<Scenario>(entity =>
        {
            entity.HasKey(e => e.ScenarioId).HasName("PK__SCENARIO__D56D0C7DC78BF424");

            entity.ToTable("SCENARIOS");

            entity.Property(e => e.ScenarioId)
                .ValueGeneratedNever()
                .HasColumnName("scenario_id");
            entity.Property(e => e.Context).HasColumnName("context");
        });

        modelBuilder.Entity<StepExecution>(entity =>
        {
            entity.HasKey(e => e.StepExecutionId).HasName("PK__STEP_EXE__A2477101FD753A3A");

            entity.ToTable("STEP_EXECUTIONS");

            entity.HasIndex(e => e.AchStepId, "IX_STEP_EXECUTIONS_ach_step_id");

            entity.HasIndex(e => e.DatetimeStart, "IX_STEP_EXECUTIONS_datetime_start");

            entity.HasIndex(e => e.ExecutionStatus, "IX_STEP_EXECUTIONS_execution_status");

            entity.HasIndex(e => e.ExperimentId, "IX_STEP_EXECUTIONS_experiment_id");

            entity.Property(e => e.StepExecutionId)
                .ValueGeneratedNever()
                .HasColumnName("step_execution_id");
            entity.Property(e => e.AchStepId).HasColumnName("ach_step_id");
            entity.Property(e => e.AchStepName)
                .HasMaxLength(100)
                .HasColumnName("ach_step_name");
            entity.Property(e => e.DatetimeEnd).HasColumnName("datetime_end");
            entity.Property(e => e.DatetimeStart).HasColumnName("datetime_start");
            entity.Property(e => e.Description)
                .HasMaxLength(500)
                .HasColumnName("description");
            entity.Property(e => e.ErrorMessage).HasColumnName("error_message");
            entity.Property(e => e.ErrorType)
                .HasMaxLength(100)
                .HasColumnName("error_type");
            entity.Property(e => e.ExecutionStatus)
                .HasMaxLength(50)
                .HasColumnName("execution_status");
            entity.Property(e => e.ExperimentId).HasColumnName("experiment_id");
            entity.Property(e => e.OrchestrationTypeId).HasColumnName("orchestration_type_id");
            entity.Property(e => e.RetryCount)
                .HasDefaultValue(0)
                .HasColumnName("retry_count");
            entity.Property(e => e.TaskInstructions).HasColumnName("task_instructions");

            entity.HasOne(d => d.AchStep).WithMany(p => p.StepExecutions)
                .HasForeignKey(d => d.AchStepId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_STEP_EXECUTIONS_ACH_STEPS");

            entity.HasOne(d => d.Experiment).WithMany(p => p.StepExecutions)
                .HasForeignKey(d => d.ExperimentId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_STEP_EXECUTIONS_EXPERIMENTS");

            entity.HasOne(d => d.OrchestrationType).WithMany(p => p.StepExecutions)
                .HasForeignKey(d => d.OrchestrationTypeId)
                .HasConstraintName("FK_STEP_EXECUTIONS_ORCHESTRATION_TYPES");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
