using Application.Ports.Persistence;
using Application.Common.Errors;
using Domain.Models.Homes;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.UseCases.Homes.DeleteHome;

public class DeleteHomeCommandHandler : IRequestHandler<DeleteHomeCommand>
{
    private readonly IAppReadDbContext _context;
    private readonly IHomeRepository _homeRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteHomeCommandHandler(
        IAppReadDbContext context,
        IHomeRepository homeRepository,
        IUnitOfWork unitOfWork)
    {
        _context = context;
        _homeRepository = homeRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(DeleteHomeCommand request, CancellationToken cancellationToken)
    {
        var home = await _homeRepository.GetById(request.HomeId)
            ?? throw new HomeNotFoundException(request.HomeId);

        var hasDevices = await _context.Devices
            .AsNoTracking()
            .AnyAsync(device => device.HomeId == request.HomeId, cancellationToken);

        var hasScenes = await _context.Scenes
            .AsNoTracking()
            .AnyAsync(scene => scene.HomeId == request.HomeId, cancellationToken);

        var hasAutomationRules = await _context.AutomationRules
            .AsNoTracking()
            .AnyAsync(rule => rule.HomeId == request.HomeId, cancellationToken);

        if (hasDevices || hasScenes || hasAutomationRules)
            throw new DomainValidationException("Home has related devices, scenes, or automations and cannot be deleted.");

        _homeRepository.Remove(home);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
