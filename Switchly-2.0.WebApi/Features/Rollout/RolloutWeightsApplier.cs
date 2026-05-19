using System.Text.Json;
using Switchly_2._0.WebApi.Context;
using Switchly_2._0.WebApi.Entities;
using Switchly_2._0.WebApi.Models.Enums;

namespace Switchly_2._0.WebApi.Features.Rollout;

// Rollout adımı arasında weight uygulama, pre-schedule snapshot alma ve restore mantığı
// burada toplandı — 5 farklı handler ve promoter service tarafından paylaşılıyor.
internal static class RolloutWeightsApplier
{
    public static string TakeSnapshot(
        FeatureFlagEnvironment env,
        FeatureFlagType flagType,
        List<FeatureFlagEnvironmentVariantWeight> currentWeights)
    {
        if (flagType == FeatureFlagType.Boolean)
        {
            return JsonSerializer.Serialize(new SnapshotBoolean(
                Type: "boolean",
                IsEnabled: env.IsEnabled,
                DefaultRolloutKind: env.DefaultRolloutKind.ToString(),
                DefaultRolloutPercentage: env.DefaultRolloutPercentage
            ));
        }

        var weights = currentWeights
            .Select(w => new SnapshotVariantWeight(w.VariantId, w.Weight))
            .ToList();

        return JsonSerializer.Serialize(new SnapshotMultivariant(
            Type: "multivariant",
            Weights: weights
        ));
    }

    public static void ApplyStep(
        SwitchlyDbContext context,
        FeatureFlagEnvironment env,
        FeatureFlagType flagType,
        Guid? targetVariantId,
        int stepPercentage,
        List<FeatureFlagEnvironmentVariantWeight> currentWeights,
        List<Variant> flagVariants)
    {
        if (flagType == FeatureFlagType.Boolean)
        {
            env.IsEnabled = stepPercentage > 0;
            env.DefaultRolloutKind = stepPercentage switch
            {
                <= 0 => RolloutKind.Off,
                >= 100 => RolloutKind.AllUsers,
                _ => RolloutKind.Percentage
            };
            env.DefaultRolloutPercentage = stepPercentage;
            env.UpdatedAt = DateTimeOffset.UtcNow;
            return;
        }

        // Multivariant: target variant'a stepPercentage, kalanı diğerleri orantılı paylaşır.
        // Mevcut weight'leri silip yeniden ekliyoruz (SetEnvVariantWeights pattern'i).
        if (currentWeights.Count > 0)
            context.FeatureFlagEnvironmentVariantWeights.RemoveRange(currentWeights);

        var remaining = 100 - stepPercentage;
        var otherVariants = flagVariants.Where(v => v.Id != targetVariantId).ToList();

        // Diğer variant'ların eski weight toplamı (rebalance referansı). 0 ise eşit pay.
        var otherCurrentSum = currentWeights
            .Where(w => w.VariantId != targetVariantId)
            .Sum(w => w.Weight);

        var newWeights = new List<FeatureFlagEnvironmentVariantWeight>(flagVariants.Count);
        var distributed = 0;

        for (var i = 0; i < otherVariants.Count; i++)
        {
            var v = otherVariants[i];
            int weight;
            if (i == otherVariants.Count - 1)
            {
                // Son variant: rounding hatalarını kapatmak için kalan'ı al.
                weight = remaining - distributed;
            }
            else if (otherCurrentSum > 0)
            {
                var prev = currentWeights.FirstOrDefault(w => w.VariantId == v.Id)?.Weight ?? 0;
                weight = (int)Math.Round((double)prev / otherCurrentSum * remaining);
            }
            else
            {
                weight = remaining / otherVariants.Count;
            }
            distributed += weight;
            newWeights.Add(new FeatureFlagEnvironmentVariantWeight
            {
                Id = Guid.NewGuid(),
                FeatureFlagEnvironmentId = env.Id,
                VariantId = v.Id,
                Weight = weight
            });
        }

        if (targetVariantId.HasValue)
        {
            newWeights.Add(new FeatureFlagEnvironmentVariantWeight
            {
                Id = Guid.NewGuid(),
                FeatureFlagEnvironmentId = env.Id,
                VariantId = targetVariantId.Value,
                Weight = stepPercentage
            });
        }

        context.FeatureFlagEnvironmentVariantWeights.AddRange(newWeights);
        env.UpdatedAt = DateTimeOffset.UtcNow;
    }

    public static void RestoreSnapshot(
        SwitchlyDbContext context,
        FeatureFlagEnvironment env,
        FeatureFlagType flagType,
        string snapshotJson,
        List<FeatureFlagEnvironmentVariantWeight> currentWeights)
    {
        if (flagType == FeatureFlagType.Boolean)
        {
            var snap = JsonSerializer.Deserialize<SnapshotBoolean>(snapshotJson)
                       ?? throw new InvalidOperationException("Snapshot deserialize failed.");

            env.IsEnabled = snap.IsEnabled;
            env.DefaultRolloutKind = Enum.TryParse<RolloutKind>(snap.DefaultRolloutKind, out var rk) ? rk : RolloutKind.Off;
            env.DefaultRolloutPercentage = snap.DefaultRolloutPercentage;
            env.UpdatedAt = DateTimeOffset.UtcNow;
            return;
        }

        var msnap = JsonSerializer.Deserialize<SnapshotMultivariant>(snapshotJson)
                    ?? throw new InvalidOperationException("Snapshot deserialize failed.");

        if (currentWeights.Count > 0)
            context.FeatureFlagEnvironmentVariantWeights.RemoveRange(currentWeights);

        var restored = msnap.Weights.Select(w => new FeatureFlagEnvironmentVariantWeight
        {
            Id = Guid.NewGuid(),
            FeatureFlagEnvironmentId = env.Id,
            VariantId = w.VariantId,
            Weight = w.Weight
        }).ToList();

        context.FeatureFlagEnvironmentVariantWeights.AddRange(restored);
        env.UpdatedAt = DateTimeOffset.UtcNow;
    }

    private sealed record SnapshotBoolean(
        string Type,
        bool IsEnabled,
        string DefaultRolloutKind,
        int DefaultRolloutPercentage);

    private sealed record SnapshotMultivariant(
        string Type,
        List<SnapshotVariantWeight> Weights);

    private sealed record SnapshotVariantWeight(Guid VariantId, int Weight);
}
