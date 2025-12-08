using Application.Common.Message;

namespace Application.Interfaces;

public interface IMessagePublisher
{
    Task PublishMessage(string topic, object? message, MessagePolicy policy);
}
