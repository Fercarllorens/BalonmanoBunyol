using BalonmanoBunol.Domain.Common;

namespace BalonmanoBunol.Domain.Teams;

public sealed class Team : Entity
{
    private Team() { }

    public Team(string name, string category, string season)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(category);
        ArgumentException.ThrowIfNullOrWhiteSpace(season);

        Name = name.Trim();
        Category = category.Trim();
        Season = season.Trim();
    }

    public string Name { get; private set; } = string.Empty;
    public string Category { get; private set; } = string.Empty;
    public string Season { get; private set; } = string.Empty;
    public bool IsActive { get; private set; } = true;
}
