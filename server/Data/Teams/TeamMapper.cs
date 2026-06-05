using Core.Teams;

namespace Data.Teams;

internal static class TeamMapper
{
    public static Team ToDomain(this TeamDocument doc) => new()
    {
        Id = doc.Id,
        Name = doc.Name,
        Description = doc.Description,
        CreatedBy = doc.CreatedBy,
        Members = doc.Members.Select(m => m.ToDomain()).ToList(),
        CreatedAt = DateTimeOffset.FromUnixTimeMilliseconds(doc.CreatedAt).UtcDateTime,
        UpdatedAt = DateTimeOffset.FromUnixTimeMilliseconds(doc.UpdatedAt).UtcDateTime,
    };

    public static TeamMember ToDomain(this TeamMemberDocument doc) => new()
    {
        UserId = doc.UserId,
        Role = doc.Role,
        JoinedAt = DateTimeOffset.FromUnixTimeMilliseconds(doc.JoinedAt).UtcDateTime,
    };

    public static TeamImage ToDomain(this TeamImageDocument doc) => new()
    {
        Id = doc.Id,
        TeamId = doc.TeamId,
        UploadedBy = doc.UploadedBy,
        ImageUrl = doc.ImageUrl,
        Notes = doc.Notes,
        UploadedAt = DateTimeOffset.FromUnixTimeMilliseconds(doc.UploadedAt).UtcDateTime,
    };

    public static TeamDocument ToDocument(string name, string? description, string createdBy)
    {
        var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        return new TeamDocument
        {
            Id = $"team_{Guid.NewGuid():N}"[..20],
            Name = name,
            Description = description,
            CreatedBy = createdBy,
            Members =
            [
                new TeamMemberDocument { UserId = createdBy, Role = TeamRole.Owner, JoinedAt = now }
            ],
            CreatedAt = now,
            UpdatedAt = now,
        };
    }

    public static TeamMemberDocument ToDocument(this TeamMember member) => new()
    {
        UserId = member.UserId,
        Role = member.Role,
        JoinedAt = new DateTimeOffset(member.JoinedAt, TimeSpan.Zero).ToUnixTimeMilliseconds(),
    };

    public static TeamImageDocument ToDocument(string teamId, string uploadedBy, string imageUrl, string? notes)
    {
        var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        return new TeamImageDocument
        {
            Id = $"img_{Guid.NewGuid():N}"[..20],
            TeamId = teamId,
            UploadedBy = uploadedBy,
            ImageUrl = imageUrl,
            Notes = notes,
            UploadedAt = now,
        };
    }
}
