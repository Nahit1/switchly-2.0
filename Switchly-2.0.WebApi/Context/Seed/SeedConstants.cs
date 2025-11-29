namespace Switchly_2._0.WebApi.Context.Seed;

public static class SeedConstants
{
    public static readonly Guid UserId              = Guid.Parse("00000000-0000-0000-0000-000000000001");
    public static readonly Guid OrganizationId      = Guid.Parse("00000000-0000-0000-0000-000000000010");
    public static readonly Guid OrganizationMemberId= Guid.Parse("00000000-0000-0000-0000-000000000011");

    public static readonly Guid ProjectId           = Guid.Parse("00000000-0000-0000-0000-000000000020");
    public static readonly Guid EnvDevId            = Guid.Parse("00000000-0000-0000-0000-000000000030");
    public static readonly Guid EnvStgId            = Guid.Parse("00000000-0000-0000-0000-000000000031");
    public static readonly Guid EnvProdId           = Guid.Parse("00000000-0000-0000-0000-000000000032");

    public static readonly Guid FeatureFlagId       = Guid.Parse("00000000-0000-0000-0000-000000000040");
    public static readonly Guid FFEnvDevId          = Guid.Parse("00000000-0000-0000-0000-000000000050");
    public static readonly Guid FFEnvStgId          = Guid.Parse("00000000-0000-0000-0000-000000000051");
    public static readonly Guid FFEnvProdId         = Guid.Parse("00000000-0000-0000-0000-000000000052");
}