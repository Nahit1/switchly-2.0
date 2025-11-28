namespace Switchly_2._0.WebApi.Entities;

public class Organization
{
    public Guid Id { get; set; }
    public string Name { get; set; } = default!;
    public string Slug { get; set; } = default!;
    public string PublicKey { get; set; } = default!;
    public string SecretKey { get; set; } = default!;

    public Guid OwnerUserId { get; set; }
    public User OwnerUser { get; set; } = default!;

    public DateTimeOffset CreatedAt { get; set; }
    public ICollection<OrganizationMember> Members { get; set; } = new List<OrganizationMember>();
    public ICollection<Project> Projects { get; set; } = new List<Project>();
    public ICollection<SegmentGroup> SegmentGroups { get; set; } = new List<SegmentGroup>();
}