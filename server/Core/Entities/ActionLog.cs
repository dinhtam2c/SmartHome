namespace Core.Entities;

public class ActionLog
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Action { get; set; }
    public long Timestamp { get; set; }

    public User? User { get; set; }

    public ActionLog(Guid id, Guid userId, string action, long timestamp)
    {
        Id = id;
        UserId = userId;
        Action = action;
        Timestamp = timestamp;
    }
}
