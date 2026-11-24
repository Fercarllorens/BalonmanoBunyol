using BalonmanoBunol.Domain.Common;

namespace BalonmanoBunol.Domain.Teams;

public sealed class Team : Entity
{
    private Team() { }

    public Team(string name, string category)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(category);

        Name = name.Trim();
        Category = category.Trim();
    }

    public string Name { get; private set; } = string.Empty;
    public string Category { get; private set; } = string.Empty;
    public bool IsActive { get; private set; } = true;

    public void Update(string name, string category)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(category);
        Name = name.Trim();
        Category = category.Trim();
    }

    public void SetActive(bool isActive) => IsActive = isActive;
}
