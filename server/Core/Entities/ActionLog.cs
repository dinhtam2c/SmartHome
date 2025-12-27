using Core.Common;

namespace Core.Entities;

public class ActionLog
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Action { get; set; }
    public long Timestamp { get; set; }

    public User? User { get; set; }

    public ActionLog(Guid userId, string action)
    {
        Id = Guid.NewGuid();
        UserId = userId;
        Action = action;
        Timestamp = Time.UnixNow();
    }
}
