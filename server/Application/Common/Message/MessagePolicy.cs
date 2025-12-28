namespace Application.Common.Message;

public record MessagePolicy(
    int Qos,
    bool Retained
);
