using Application.DTOs.LocationDto;
using Application.Exceptions;
using Application.Interfaces;
using Application.Interfaces.Repositories;
using Microsoft.Extensions.Logging;

namespace Application.Services;

public interface ILocationService
{
    Task<IEnumerable<LocationListElement>> GetLocationList(Guid? homeId = null);

    Task<LocationDetails> GetLocationDetails(Guid locationId);

    Task<LocationAddResponse> AddLocation(LocationAddRequest request);

    Task UpdateLocation(Guid locationId, LocationUpdateRequest request);

    Task DeleteLocation(Guid locationId);
}

public class LocationService : ILocationService
{
    private readonly ILogger<LocationService> _logger;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILocationRepository _locationRepository;
    private readonly IHomeRepository _homeRepository;

    public LocationService(ILogger<LocationService> logger, IUnitOfWork unitOfWork,
        ILocationRepository locationRepository, IHomeRepository homeRepository)
    {
        _logger = logger;
        _unitOfWork = unitOfWork;
        _locationRepository = locationRepository;
        _homeRepository = homeRepository;
    }

    public async Task<IEnumerable<LocationListElement>> GetLocationList(Guid? homeId = null)
    {
        var locations = homeId.HasValue
            ? await _locationRepository.GetByHomeId(homeId.Value)
            : await _locationRepository.GetAll();
        return locations.Select(LocationListElement.FromLocation);
    }

    public async Task<LocationDetails> GetLocationDetails(Guid locationId)
    {
        var location = await _locationRepository.GetById(locationId) ?? throw new LocationNotFoundException(locationId);
        return LocationDetails.FromLocation(location);
    }

    public async Task<LocationAddResponse> AddLocation(LocationAddRequest request)
    {
        // Ensure home exists
        var home = await _homeRepository.GetById(request.HomeId) ?? throw new HomeNotFoundException(request.HomeId);

        var location = request.ToLocation();
        await _locationRepository.Add(location);
        await _unitOfWork.Commit();
        return LocationAddResponse.FromLocation(location);
    }

    public async Task UpdateLocation(Guid locationId, LocationUpdateRequest request)
    {
        var location = await _locationRepository.GetById(locationId) ?? throw new LocationNotFoundException(locationId);

        location.Name = request.Name ?? location.Name;
        location.Description = request.Description ?? location.Description;
        location.UpdatedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        await _unitOfWork.Commit();
    }

    public async Task DeleteLocation(Guid locationId)
    {
        var location = await _locationRepository.GetById(locationId) ?? throw new LocationNotFoundException(locationId);

        await _locationRepository.Delete(location);
        await _unitOfWork.Commit();
    }
}
