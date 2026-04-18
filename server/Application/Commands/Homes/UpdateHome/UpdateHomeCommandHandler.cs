using Application.Common.Data;
using Application.Exceptions;
using Core.Domain.Homes;
using MediatR;

namespace Application.Commands.Homes.UpdateHome;

public class UpdateHomeCommandHandler : IRequestHandler<UpdateHomeCommand>
{
    private readonly IHomeRepository _homeRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateHomeCommandHandler(IHomeRepository homeRepository, IUnitOfWork unitOfWork)
    {
        _homeRepository = homeRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(UpdateHomeCommand request, CancellationToken cancellationToken)
    {
        var home = await _homeRepository.GetById(request.HomeId)
            ?? throw new HomeNotFoundException(request.HomeId);

        home.Update(request.Name, request.Description);

        await _unitOfWork.SaveChangesAsync();
    }
}
