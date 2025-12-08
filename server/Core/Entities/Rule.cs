namespace Core.Entities;

public class Rule
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string? Description { get; set; }
    public string[] Conditions { get; set; }
    public string Logic { get; set; }
    public string[] Actions { get; set; }
    public bool IsEnabled { get; set; }
    public long CreatedAt { get; set; }
    public long UpdatedAt { get; set; }

    public Rule(Guid id, string name, string? description, string[] conditions, string logic,
        string[] actions, long createdAt)
    {
        Id = id;
        Name = name;
        Description = description;
        Conditions = conditions;
        Logic = logic;
        Actions = actions;
        IsEnabled = true;
        CreatedAt = createdAt;
        UpdatedAt = createdAt;
    }
}
