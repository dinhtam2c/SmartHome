namespace Application.Interfaces;

public interface IMessageHandler
{
    string TopicPattern { get; }

    Type MessageType { get; }

    Task HandleMessage(string[] topicTokens, object message);
}
