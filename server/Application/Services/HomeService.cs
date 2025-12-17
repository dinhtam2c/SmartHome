using Application.DTOs.HomeDto;
using Application.Exceptions;
using Application.Interfaces;
using Application.Interfaces.Repositories;
using Microsoft.Extensions.Logging;

namespace Application.Services;

public interface IHomeService
{
    Task<IEnumerable<HomeListElement>> GetHomeList();

    Task<HomeDetails> GetHomeDetails(Guid homeId);

    Task<HomeAddResponse> AddHome(HomeAddRequest request);

    Task UpdateHome(Guid homeId, HomeUpdateRequest request);

    Task DeleteHome(Guid homeId);

    Task AssignGatewayToHome(Guid homeId, GatewayAssignRequest request);
}

public class HomeService : IHomeService
{
    private readonly ILogger<HomeService> _logger;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IHomeRepository _homeRepository;
    private readonly IGatewayRepository _gatewayRepository;

    public HomeService(ILogger<HomeService> logger, IUnitOfWork unitOfWork, IHomeRepository homeRepository,
        IGatewayRepository gatewayRepository)
    {
        _logger = logger;
        _unitOfWork = unitOfWork;
        _homeRepository = homeRepository;
        _gatewayRepository = gatewayRepository;
    }

    public async Task<IEnumerable<HomeListElement>> GetHomeList()
    {
        var homes = await _homeRepository.GetAll();
        return homes.Select(HomeListElement.FromHome);
    }

    public async Task<HomeDetails> GetHomeDetails(Guid homeId)
    {
        var home = await _homeRepository.GetById(homeId) ?? throw new HomeNotFoundException(homeId);
        return HomeDetails.FromHome(home);
    }

    public async Task<HomeAddResponse> AddHome(HomeAddRequest request)
    {
        var home = request.ToHome();
        await _homeRepository.Add(home);
        await _unitOfWork.Commit();
        return HomeAddResponse.FromHome(home);
    }

    public async Task UpdateHome(Guid homeId, HomeUpdateRequest request)
    {
        var home = await _homeRepository.GetById(homeId) ?? throw new HomeNotFoundException(homeId);
        home.Name = request.Name ?? home.Name;
        home.Description = request.Description ?? home.Description;
        await _unitOfWork.Commit();
    }

    public async Task DeleteHome(Guid homeId)
    {
        var home = await _homeRepository.GetById(homeId) ?? throw new HomeNotFoundException(homeId);
        await _homeRepository.Delete(home);
        await _unitOfWork.Commit();
    }

    public async Task AssignGatewayToHome(Guid homeId, GatewayAssignRequest request)
    {
        var home = await _homeRepository.GetById(homeId) ?? throw new HomeNotFoundException(homeId);

        var gatewayId = request.GatewayId;
        var gateway = await _gatewayRepository.GetById(gatewayId) ?? throw new GatewayNotFoundException(gatewayId);

        gateway.HomeId = homeId;
        gateway.UpdatedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        await _unitOfWork.Commit();
    }
}
