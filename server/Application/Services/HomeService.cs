using Application.DTOs.Api.Homes;
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
}

public class HomeService : IHomeService
{
    private readonly ILogger<HomeService> _logger;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IHomeRepository _homeRepository;

    public HomeService(ILogger<HomeService> logger, IUnitOfWork unitOfWork, IHomeRepository homeRepository)
    {
        _logger = logger;
        _unitOfWork = unitOfWork;
        _homeRepository = homeRepository;
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
        home.Update(request.Name, request.Description);

        await _unitOfWork.Commit();
    }

    public async Task DeleteHome(Guid homeId)
    {
        var home = await _homeRepository.GetById(homeId) ?? throw new HomeNotFoundException(homeId);
        await _homeRepository.Delete(home);
        await _unitOfWork.Commit();
    }
}
