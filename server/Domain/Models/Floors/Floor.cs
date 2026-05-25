using Domain.Common;

namespace Domain.Models.Floors;

public class Floor : Entity
{
    public Guid Id { get; private set; }
    public Guid HomeId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public int SortOrder { get; private set; }
    public int CanvasWidth { get; private set; }
    public int CanvasHeight { get; private set; }
    public long CreatedAt { get; private set; }
    public long UpdatedAt { get; private set; }

    private readonly List<FloorPlanRoom> _floorPlanRooms = [];
    public IReadOnlyCollection<FloorPlanRoom> FloorPlanRooms => _floorPlanRooms;

    private readonly List<FloorDevicePlacement> _devicePlacements = [];
    public IReadOnlyCollection<FloorDevicePlacement> DevicePlacements => _devicePlacements;

    private Floor()
    {
    }

    private Floor(Guid homeId, string name, int canvasWidth, int canvasHeight, int sortOrder)
    {
        if (homeId == Guid.Empty)
            throw new InvalidOperationException("HomeId is required.");

        EnsurePositiveCanvasSize(canvasWidth, canvasHeight);
        EnsurePositiveSortOrder(sortOrder);

        Id = Guid.NewGuid();
        HomeId = homeId;
        Name = NormalizeName(name);
        SortOrder = sortOrder;
        CanvasWidth = canvasWidth;
        CanvasHeight = canvasHeight;
        CreatedAt = UpdatedAt = UnixTime.Now();

        RaiseChanged(FloorChangeReasons.Created);
    }

    public static Floor Create(Guid homeId, string name, int canvasWidth, int canvasHeight, int sortOrder)
    {
        return new Floor(homeId, name, canvasWidth, canvasHeight, sortOrder);
    }

    public FloorPlanRoom AddRoom(
        Guid roomId,
        IReadOnlyCollection<FloorPoint> polygon,
        string? fillColor)
    {
        if (_floorPlanRooms.Any(room => room.RoomId == roomId))
            throw new InvalidOperationException("Room is already represented on this floor.");

        ValidatePolygon(polygon, null);
        var room = new FloorPlanRoom(Id, roomId, polygon, fillColor);
        _floorPlanRooms.Add(room);
        Touch(FloorChangeReasons.RoomAdded);
        return room;
    }

    public void UpdateRoom(
        Guid floorPlanRoomId,
        IReadOnlyCollection<FloorPoint> polygon,
        string? fillColor)
    {
        var room = GetRoom(floorPlanRoomId);
        ValidatePolygon(polygon, floorPlanRoomId);
        room.ReplaceVisuals(polygon, fillColor);
        Touch(FloorChangeReasons.RoomUpdated);
    }

    public void RemoveRoom(Guid floorPlanRoomId)
    {
        _floorPlanRooms.Remove(GetRoom(floorPlanRoomId));
        Touch(FloorChangeReasons.RoomRemoved);
    }

    public FloorDevicePlacement PlaceDevice(Guid deviceId, float x, float y)
    {
        if (deviceId == Guid.Empty)
            throw new InvalidOperationException("DeviceId is required.");

        if (_devicePlacements.Any(item => item.DeviceId == deviceId))
            throw new InvalidOperationException($"Device '{deviceId}' is already placed on this floor.");

        EnsureCoordinatesInBounds(x, y);
        var placement = new FloorDevicePlacement(Id, deviceId, x, y);
        _devicePlacements.Add(placement);
        Touch(FloorChangeReasons.DevicePlaced);
        return placement;
    }

    public void MoveDevice(Guid placementId, float x, float y)
    {
        var placement = GetPlacement(placementId);
        EnsureCoordinatesInBounds(x, y);
        placement.Move(x, y);
        Touch(FloorChangeReasons.DeviceMoved);
    }

    public void RemoveDevicePlacement(Guid placementId)
    {
        _devicePlacements.Remove(GetPlacement(placementId));
        Touch(FloorChangeReasons.DeviceRemoved);
    }

    public bool RemoveDevicePlacementByDeviceId(Guid deviceId)
    {
        var placement = _devicePlacements.FirstOrDefault(item => item.DeviceId == deviceId);
        if (placement is null)
            return false;

        _devicePlacements.Remove(placement);
        Touch(FloorChangeReasons.DeviceRemoved);
        return true;
    }

    public void RemoveDevicePlacements(IEnumerable<Guid> deviceIds)
    {
        var ids = deviceIds.ToHashSet();
        var removed = _devicePlacements.RemoveAll(placement => ids.Contains(placement.DeviceId));
        if (removed > 0)
            Touch(FloorChangeReasons.DeviceRemoved);
    }

    public Guid? ResolveRoomId(float x, float y)
    {
        EnsureCoordinatesInBounds(x, y);
        return FloorPlanGeometry.ResolveRoomId(_floorPlanRooms, new FloorPoint(x, y));
    }

    public bool PlacementMatchesRoom(FloorDevicePlacement placement, Guid? roomId)
    {
        return ResolveRoomId(placement.X, placement.Y) == roomId;
    }

    public void UpdateInfo(string? name, int? canvasWidth, int? canvasHeight)
    {
        var nextName = name is null ? Name : NormalizeName(name);
        var nextCanvasWidth = canvasWidth ?? CanvasWidth;
        var nextCanvasHeight = canvasHeight ?? CanvasHeight;
        EnsurePositiveCanvasSize(nextCanvasWidth, nextCanvasHeight);

        if (nextCanvasWidth != CanvasWidth || nextCanvasHeight != CanvasHeight)
            EnsureExistingContentFits(nextCanvasWidth, nextCanvasHeight);

        if (string.Equals(Name, nextName, StringComparison.Ordinal)
            && CanvasWidth == nextCanvasWidth
            && CanvasHeight == nextCanvasHeight)
            return;

        Name = nextName;
        CanvasWidth = nextCanvasWidth;
        CanvasHeight = nextCanvasHeight;
        Touch(FloorChangeReasons.InfoUpdated);
    }

    public void SetSortOrder(int sortOrder)
    {
        EnsurePositiveSortOrder(sortOrder);
        if (SortOrder == sortOrder)
            return;

        SortOrder = sortOrder;
        UpdatedAt = UnixTime.Now();
    }

    private FloorPlanRoom GetRoom(Guid floorPlanRoomId)
    {
        return _floorPlanRooms.FirstOrDefault(item => item.Id == floorPlanRoomId)
            ?? throw new InvalidOperationException("Floor plan room not found.");
    }

    private FloorDevicePlacement GetPlacement(Guid placementId)
    {
        return _devicePlacements.FirstOrDefault(item => item.Id == placementId)
            ?? throw new InvalidOperationException("Floor device placement not found.");
    }

    private void ValidatePolygon(
        IReadOnlyCollection<FloorPoint> polygon,
        Guid? excludedFloorPlanRoomId)
    {
        FloorPlanGeometry.ValidatePolygon(polygon);
        foreach (var point in polygon)
        {
            if (!IsCoordinateInBounds(point.X, point.Y, CanvasWidth, CanvasHeight))
                throw new InvalidOperationException(
                    $"Room polygon points must be inside canvas bounds {CanvasWidth}x{CanvasHeight}.");
        }

        if (_floorPlanRooms
            .Where(room => room.Id != excludedFloorPlanRoomId)
            .Any(room => FloorPlanGeometry.HasInteriorOverlap(room.Polygon, polygon.ToList())))
            throw new InvalidOperationException("Floor plan rooms cannot overlap.");
    }

    private void EnsureCoordinatesInBounds(float x, float y)
    {
        if (!IsCoordinateInBounds(x, y, CanvasWidth, CanvasHeight))
            throw new InvalidOperationException(
                $"Coordinates ({x}, {y}) are outside canvas bounds {CanvasWidth}x{CanvasHeight}.");
    }

    private void EnsureExistingContentFits(int canvasWidth, int canvasHeight)
    {
        var roomOutside = _floorPlanRooms
            .SelectMany(room => room.Polygon)
            .Any(point => !IsCoordinateInBounds(point.X, point.Y, canvasWidth, canvasHeight));
        var placementOutside = _devicePlacements
            .Any(item => !IsCoordinateInBounds(item.X, item.Y, canvasWidth, canvasHeight));

        if (roomOutside || placementOutside)
            throw new InvalidOperationException(
                "Canvas size cannot be smaller than existing rooms or placed devices.");
    }

    private static void EnsurePositiveCanvasSize(int canvasWidth, int canvasHeight)
    {
        if (canvasWidth <= 0 || canvasHeight <= 0)
            throw new InvalidOperationException("Canvas dimensions must be positive.");
    }

    private static void EnsurePositiveSortOrder(int sortOrder)
    {
        if (sortOrder <= 0)
            throw new InvalidOperationException("SortOrder must be positive.");
    }

    private static bool IsCoordinateInBounds(float x, float y, int canvasWidth, int canvasHeight)
    {
        return float.IsFinite(x)
            && float.IsFinite(y)
            && x >= 0
            && x <= canvasWidth
            && y >= 0
            && y <= canvasHeight;
    }

    private void Touch(string reason)
    {
        UpdatedAt = UnixTime.Now();
        RaiseChanged(reason);
    }

    private void RaiseChanged(string reason)
    {
        Raise(new FloorChangedDomainEvent(Guid.NewGuid(), Id, HomeId, reason));
    }

    private static string NormalizeName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new InvalidOperationException("Floor name is required.");

        return name.Trim();
    }
}
