using Core.Common;
using Core.Primitives;

namespace Core.Domain.Floors;

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

    private readonly List<FloorRoom> _rooms = [];
    public IReadOnlyCollection<FloorRoom> Rooms => _rooms;

    private readonly List<PlacedFloorDevice> _placedFloorDevices = [];
    public IReadOnlyCollection<PlacedFloorDevice> PlacedFloorDevices => _placedFloorDevices;

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

        var now = Time.UnixNow();
        CreatedAt = now;
        UpdatedAt = now;

        RaiseChanged(FloorChangeReasons.Created);
    }

    public static Floor Create(Guid homeId, string name, int canvasWidth, int canvasHeight, int sortOrder)
    {
        return new Floor(homeId, name, canvasWidth, canvasHeight, sortOrder);
    }

    public FloorRoom AddRoom(
        Guid? linkedRoomId,
        string label,
        IReadOnlyCollection<FloorPoint> polygon,
        string? fillColor)
    {
        EnsurePolygonInBounds(polygon);

        var room = new FloorRoom(Id, linkedRoomId, label, polygon, fillColor);
        _rooms.Add(room);

        Touch(FloorChangeReasons.RoomAdded);
        return room;
    }

    public void UpdateRoom(
        Guid roomId,
        Guid? linkedRoomId,
        string label,
        IReadOnlyCollection<FloorPoint> polygon,
        string? fillColor)
    {
        var room = _rooms.FirstOrDefault(item => item.Id == roomId)
            ?? throw new InvalidOperationException("Floor room not found.");

        EnsurePolygonInBounds(polygon);
        room.Replace(linkedRoomId, label, polygon, fillColor);

        Touch(FloorChangeReasons.RoomUpdated);
    }

    public void RemoveRoom(Guid roomId)
    {
        var room = _rooms.FirstOrDefault(item => item.Id == roomId)
            ?? throw new InvalidOperationException("Floor room not found.");

        _rooms.Remove(room);

        foreach (var placedFloorDevice in _placedFloorDevices)
        {
            placedFloorDevice.ClearRoomAssignmentIfMatches(roomId);
        }

        Touch(FloorChangeReasons.RoomRemoved);
    }

    public PlacedFloorDevice PlaceDevice(Guid deviceId, float x, float y, Guid? floorRoomId)
    {
        if (deviceId == Guid.Empty)
            throw new InvalidOperationException("DeviceId is required.");

        if (_placedFloorDevices.Any(item => item.DeviceId == deviceId))
            throw new InvalidOperationException($"Device '{deviceId}' is already placed on this floor.");

        EnsureCoordinatesInBounds(x, y);
        EnsureRoomExists(floorRoomId);

        var placedFloorDevice = new PlacedFloorDevice(Id, deviceId, x, y, floorRoomId);
        _placedFloorDevices.Add(placedFloorDevice);

        Touch(FloorChangeReasons.DevicePlaced);
        return placedFloorDevice;
    }

    public void MoveDevice(Guid placedFloorDeviceId, float x, float y, Guid? floorRoomId)
    {
        var placedFloorDevice = _placedFloorDevices.FirstOrDefault(item => item.Id == placedFloorDeviceId)
            ?? throw new InvalidOperationException("Placed device not found.");

        EnsureCoordinatesInBounds(x, y);
        EnsureRoomExists(floorRoomId);

        placedFloorDevice.Move(x, y, floorRoomId);

        Touch(FloorChangeReasons.DeviceMoved);
    }

    public void RemovePlacedFloorDevice(Guid placedFloorDeviceId)
    {
        var placedFloorDevice = _placedFloorDevices.FirstOrDefault(item => item.Id == placedFloorDeviceId)
            ?? throw new InvalidOperationException("Placed device not found.");

        _placedFloorDevices.Remove(placedFloorDevice);

        Touch(FloorChangeReasons.DeviceRemoved);
    }

    public void UpdateInfo(string? name, int? canvasWidth, int? canvasHeight)
    {
        var nextName = name is null ? Name : NormalizeName(name);
        var nextCanvasWidth = canvasWidth ?? CanvasWidth;
        var nextCanvasHeight = canvasHeight ?? CanvasHeight;

        EnsurePositiveCanvasSize(nextCanvasWidth, nextCanvasHeight);

        if (nextCanvasWidth != CanvasWidth || nextCanvasHeight != CanvasHeight)
        {
            EnsureExistingContentFits(nextCanvasWidth, nextCanvasHeight);
        }

        var hasChanges =
            !string.Equals(Name, nextName, StringComparison.Ordinal)
            || CanvasWidth != nextCanvasWidth
            || CanvasHeight != nextCanvasHeight;

        if (!hasChanges)
        {
            return;
        }

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
        UpdatedAt = Time.UnixNow();
    }

    private void EnsureCoordinatesInBounds(float x, float y)
    {
        if (!IsCoordinateInBounds(x, y, CanvasWidth, CanvasHeight))
        {
            throw new InvalidOperationException(
                $"Coordinates ({x}, {y}) are outside canvas bounds {CanvasWidth}x{CanvasHeight}.");
        }
    }

    private void EnsurePolygonInBounds(IReadOnlyCollection<FloorPoint> polygon)
    {
        if (polygon == null)
            throw new InvalidOperationException("Polygon is required.");

        foreach (var point in polygon)
        {
            if (!IsCoordinateInBounds(point.X, point.Y, CanvasWidth, CanvasHeight))
            {
                throw new InvalidOperationException(
                    $"Room polygon points must be inside canvas bounds {CanvasWidth}x{CanvasHeight}.");
            }
        }
    }

    private void EnsureRoomExists(Guid? floorRoomId)
    {
        if (!floorRoomId.HasValue)
            return;

        if (floorRoomId.Value == Guid.Empty)
            throw new InvalidOperationException("FloorRoomId must not be empty.");

        if (_rooms.All(room => room.Id != floorRoomId.Value))
            throw new InvalidOperationException("Floor room not found.");
    }

    private void EnsureExistingContentFits(int canvasWidth, int canvasHeight)
    {
        foreach (var room in _rooms)
        {
            foreach (var point in room.GetPolygon())
            {
                if (!IsCoordinateInBounds(point.X, point.Y, canvasWidth, canvasHeight))
                {
                    throw new InvalidOperationException(
                        "Canvas size cannot be smaller than existing rooms or placed devices.");
                }
            }
        }

        foreach (var placedFloorDevice in _placedFloorDevices)
        {
            if (!IsCoordinateInBounds(placedFloorDevice.X, placedFloorDevice.Y, canvasWidth, canvasHeight))
            {
                throw new InvalidOperationException(
                    "Canvas size cannot be smaller than existing rooms or placed devices.");
            }
        }
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

    private static bool IsCoordinateInBounds(
        float x,
        float y,
        int canvasWidth,
        int canvasHeight)
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
        UpdatedAt = Time.UnixNow();
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
