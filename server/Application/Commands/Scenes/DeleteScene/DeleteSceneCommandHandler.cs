using Application.Common.Data;
using Application.Common.Realtime;
using Application.Exceptions;
using Core.Domain.Scenes;
using MediatR;

namespace Application.Commands.Scenes.DeleteScene;

public sealed class DeleteSceneCommandHandler : IRequestHandler<DeleteSceneCommand>
{
    private readonly ISceneRepository _sceneRepository;
    private readonly IRealtimePublisher _realtimePublisher;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteSceneCommandHandler(
        ISceneRepository sceneRepository,
        IRealtimePublisher realtimePublisher,
        IUnitOfWork unitOfWork)
    {
        _sceneRepository = sceneRepository;
        _realtimePublisher = realtimePublisher;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(DeleteSceneCommand request, CancellationToken cancellationToken)
    {
        var scene = await _sceneRepository.GetById(request.SceneId, cancellationToken)
            ?? throw new SceneNotFoundException(request.SceneId);

        if (scene.HomeId != request.HomeId)
            throw new SceneNotFoundException(request.SceneId);

        _sceneRepository.Remove(scene);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _realtimePublisher.PublishToHome(
            request.HomeId,
            RealtimeDeltaFactory.ForSceneDeleted(request.HomeId, request.SceneId),
            cancellationToken);
    }
}
