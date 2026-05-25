using Application.Ports.Persistence;
using Application.Common.Errors;
using Domain.Models.Homes;
using MediatR;

namespace Application.UseCases.Homes.CreateHome;

public class CreateHomeCommandHandler : IRequestHandler<CreateHomeCommand, Guid>
{
    private readonly IHomeRepository _homeRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateHomeCommandHandler(IHomeRepository homeRepository, IUnitOfWork unitOfWork)
    {
        _homeRepository = homeRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Guid> Handle(CreateHomeCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new DomainValidationException("Home name is required.");

        var home = Home.Create(request.Name, request.Description);

        await _homeRepository.Add(home);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return home.Id;
    }
}
