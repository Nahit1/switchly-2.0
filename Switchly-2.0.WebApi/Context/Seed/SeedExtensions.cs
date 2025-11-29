using Microsoft.EntityFrameworkCore;

namespace Switchly_2._0.WebApi.Context.Seed;

public static class SeedExtensions
{
    public static void ApplySeed(this ModelBuilder modelBuilder)
    {
        SeedDemoData.Apply(modelBuilder);
    }
}