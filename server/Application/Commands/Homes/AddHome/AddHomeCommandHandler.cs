using Application.Common.Data;
using Core.Domain.Homes;
using MediatR;

namespace Application.Commands.Homes.AddHome;

public class AddHomeCommandHandler : IRequestHandler<AddHomeCommand, Guid>
{
    private readonly IHomeRepository _homeRepository;
    private readonly IUnitOfWork _unitOfWork;

    public AddHomeCommandHandler(IHomeRepository homeRepository, IUnitOfWork unitOfWork)
    {
        _homeRepository = homeRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Guid> Handle(AddHomeCommand request, CancellationToken cancellationToken)
    {
        var home = Home.Create(request.Name, request.Description);

        await _homeRepository.Add(home);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return home.Id;
    }
}
