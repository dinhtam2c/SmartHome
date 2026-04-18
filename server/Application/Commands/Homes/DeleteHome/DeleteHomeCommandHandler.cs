using Application.Common.Data;
using Application.Exceptions;
using Core.Domain.Homes;
using MediatR;

namespace Application.Commands.Homes.DeleteHome;

public class DeleteHomeCommandHandler : IRequestHandler<DeleteHomeCommand>
{
    private readonly IHomeRepository _homeRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteHomeCommandHandler(IHomeRepository homeRepository, IUnitOfWork unitOfWork)
    {
        _homeRepository = homeRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(DeleteHomeCommand request, CancellationToken cancellationToken)
    {
        var home = await _homeRepository.GetById(request.HomeId)
            ?? throw new HomeNotFoundException(request.HomeId);

        _homeRepository.Remove(home);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
