using Application.Common.Data;
using Application.Common.Json;
using Application.Common.Message;
using Application.Common.Realtime;
using Application.Exceptions;
using Core.Domain.Devices;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Commands.Devices.SendDeviceCommand;

public sealed class SendDeviceCommandCommandHandler : IRequestHandler<SendDeviceCommandCommand>
{
    private readonly ILogger<SendDeviceCommandCommandHandler> _logger;
    private readonly IDeviceRepository _deviceRepository;
    private readonly IDeviceCommandExecutionRepository _commandExecutionRepository;
    private readonly ICapabilityCommandValidator _capabilityCommandValidator;
    private readonly IDeviceMessagePublisher _deviceMessagePublisher;
    private readonly IRealtimePublisher _realtimePublisher;
    private readonly IUnitOfWork _unitOfWork;

    public SendDeviceCommandCommandHandler(
        ILogger<SendDeviceCommandCommandHandler> logger,
        IDeviceRepository deviceRepository,
        IDeviceCommandExecutionRepository commandExecutionRepository,
        ICapabilityCommandValidator capabilityCommandValidator,
        IDeviceMessagePublisher deviceMessagePublisher,
        IRealtimePublisher realtimePublisher,
        IUnitOfWork unitOfWork
    )
    {
        _logger = logger;
        _deviceRepository = deviceRepository;
        _commandExecutionRepository = commandExecutionRepository;
        _capabilityCommandValidator = capabilityCommandValidator;
        _deviceMessagePublisher = deviceMessagePublisher;
        _realtimePublisher = realtimePublisher;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(SendDeviceCommandCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.EndpointId))
            throw new DomainValidationException("endpointId is required.");

        var device = await _deviceRepository.GetById(request.DeviceId, cancellationToken)
            ?? throw new DeviceNotFoundException(request.DeviceId);

        var capability = device.FindSingleCapability(
            request.CapabilityId,
            request.EndpointId,
            out _
        );

        if (capability is null)
        {
            throw new CapabilityNotFoundException(
                $"{request.CapabilityId}@{request.EndpointId}"
            );
        }

        if (!capability.SupportsOperation(request.Operation))
        {
            throw new CommandNotSupportedException(request.Operation, request.DeviceId);
        }

        _logger.LogInformation(
            "Sending command {Operation} to device {DeviceId} endpoint {EndpointId} capability {CapabilityId}",
            request.Operation,
            request.DeviceId,
            request.EndpointId,
            capability.CapabilityId
        );

        var validatedPayload = _capabilityCommandValidator.ValidateAndNormalize(
            capability,
            request.Operation,
            request.Value
        );

        var correlationId = request.CorrelationId ?? Guid.NewGuid().ToString("N");
        var requestPayload = JsonHelper.SerializePayload(validatedPayload);

        var execution = DeviceCommandExecution.Create(
            deviceId: device.Id,
            capabilityId: capability.CapabilityId,
            endpointId: request.EndpointId,
            correlationId: correlationId,
            operation: request.Operation,
            requestPayload: requestPayload
        );

        await _commandExecutionRepository.Add(execution);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _realtimePublisher.PublishToDevice(
            device.Id,
            RealtimeDeltaFactory.ForDeviceCommandExecution(execution),
            cancellationToken);

        var command = new DeviceCommandModel(
            device.Id,
            request.EndpointId,
            capability.CapabilityId,
            request.Operation,
            validatedPayload,
            correlationId
        );

        try
        {
            await _deviceMessagePublisher.SendCommand(command);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            execution.MarkFailed(ex.Message);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _realtimePublisher.PublishToDevice(
                device.Id,
                RealtimeDeltaFactory.ForDeviceCommandExecution(execution),
                cancellationToken);

            throw;
        }
    }
}
