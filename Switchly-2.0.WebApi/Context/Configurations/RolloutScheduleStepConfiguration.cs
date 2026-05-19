using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Switchly_2._0.WebApi.Entities;

namespace Switchly_2._0.WebApi.Context.Configurations;

public class RolloutScheduleStepConfiguration : IEntityTypeConfiguration<RolloutScheduleStep>
{
    public void Configure(EntityTypeBuilder<RolloutScheduleStep> builder)
    {
        builder.HasKey(x => x.Id);

        // Schedule içinde step'ler benzersiz indeksli olmalı.
        builder.HasIndex(x => new { x.RolloutScheduleId, x.StepIndex })
            .IsUnique();
    }
}
