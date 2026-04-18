using Application.Exceptions;
using Application.Queries.Devices.GetDeviceDetails;
using Application.Queries.Rooms.GetRoomDetails;
using MediatR;

namespace Application.Common.Realtime;

public sealed class RealtimeDetailsNotifier : IRealtimeDetailsNotifier
{
    private readonly IRealtimePublisher _realtimePublisher;
    private readonly ISender _sender;

    public RealtimeDetailsNotifier(IRealtimePublisher realtimePublisher, ISender sender)
    {
        _realtimePublisher = realtimePublisher;
        _sender = sender;
    }

    public async Task PublishDeviceDetailsChanged(Guid deviceId, CancellationToken cancellationToken = default)
    {
        DeviceDetailsDto details;
        try
        {
            details = await _sender.Send(new GetDeviceDetailsQuery(deviceId), cancellationToken);
        }
        catch (DeviceNotFoundException)
        {
            return;
        }

        var payload = new
        {
            DeviceId = details.Id,
            details.HomeId,
            details.RoomId,
            Device = details
        };

        await _realtimePublisher.PublishToDevice(
            details.Id,
            RealtimeEventNames.DeviceDetailsChanged,
            payload,
            cancellationToken);

        if (details.RoomId.HasValue)
        {
            await _realtimePublisher.PublishToRoom(
                details.RoomId.Value,
                RealtimeEventNames.DeviceDetailsChanged,
                payload,
                cancellationToken);
        }

        if (details.HomeId.HasValue)
        {
            await _realtimePublisher.PublishToHome(
                details.HomeId.Value,
                RealtimeEventNames.DeviceDetailsChanged,
                payload,
                cancellationToken);

            if (details.RoomId.HasValue)
            {
                await PublishRoomDetailsChangedInternal(
                    details.HomeId.Value,
                    details.RoomId.Value,
                    cancellationToken);
            }
        }
    }

    public async Task PublishDeviceDeleted(
        Guid deviceId,
        Guid? homeId,
        Guid? roomId,
        CancellationToken cancellationToken = default)
    {
        var payload = new
        {
            DeviceId = deviceId,
            HomeId = homeId,
            RoomId = roomId
        };

        await _realtimePublisher.PublishToDevice(
            deviceId,
            RealtimeEventNames.DeviceDeleted,
            payload,
            cancellationToken);

        if (roomId.HasValue)
        {
            await _realtimePublisher.PublishToRoom(
                roomId.Value,
                RealtimeEventNames.DeviceDeleted,
                payload,
                cancellationToken);
        }

        if (homeId.HasValue)
        {
            await _realtimePublisher.PublishToHome(
                homeId.Value,
                RealtimeEventNames.DeviceDeleted,
                payload,
                cancellationToken);
        }

        if (homeId.HasValue && roomId.HasValue)
        {
            await PublishRoomDetailsChangedInternal(homeId.Value, roomId.Value, cancellationToken);
        }
    }

    public Task PublishRoomDetailsChanged(
        Guid homeId,
        Guid roomId,
        CancellationToken cancellationToken = default)
    {
        return PublishRoomDetailsChangedInternal(homeId, roomId, cancellationToken);
    }

    public async Task PublishRoomDeleted(
        Guid homeId,
        Guid roomId,
        CancellationToken cancellationToken = default)
    {
        var payload = new
        {
            HomeId = homeId,
            RoomId = roomId
        };

        await _realtimePublisher.PublishToRoom(
            roomId,
            RealtimeEventNames.RoomDeleted,
            payload,
            cancellationToken);

        await _realtimePublisher.PublishToHome(
            homeId,
            RealtimeEventNames.RoomDeleted,
            payload,
            cancellationToken);
    }

    private async Task PublishRoomDetailsChangedInternal(
        Guid homeId,
        Guid roomId,
        CancellationToken cancellationToken)
    {
        RoomDetailsDto details;
        try
        {
            details = await _sender.Send(new GetRoomDetailsQuery(homeId, roomId), cancellationToken);
        }
        catch (RoomNotFoundException)
        {
            return;
        }

        var payload = new
        {
            HomeId = homeId,
            RoomId = details.Id,
            Room = details
        };

        await _realtimePublisher.PublishToRoom(
            details.Id,
            RealtimeEventNames.RoomDetailsChanged,
            payload,
            cancellationToken);

        await _realtimePublisher.PublishToHome(
            homeId,
            RealtimeEventNames.RoomDetailsChanged,
            payload,
            cancellationToken);
    }
}
