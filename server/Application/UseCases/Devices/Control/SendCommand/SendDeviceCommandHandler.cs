using Application.Ports.Messages;
using System.Text.Json;
using Application.BusinessServices.Capabilities.Validation;
using Application.Common.Errors;
using Application.Ports.Persistence;
using Domain.Models.Devices.Commands;
using Domain.Models.Devices;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.UseCases.Devices.Control.SendCommand;

public sealed class SendDeviceCommandHandler : IRequestHandler<SendDeviceCommand, Guid>
{
    private readonly ILogger<SendDeviceCommandHandler> _logger;
    private readonly IDeviceRepository _deviceRepository;
    private readonly IDeviceCommandExecutionRepository _commandExecutionRepository;
    private readonly ICapabilityCommandValidator _capabilityCommandValidator;
    private readonly IDeviceCommandSender _deviceCommandSender;
    private readonly IUnitOfWork _unitOfWork;

    public SendDeviceCommandHandler(
        ILogger<SendDeviceCommandHandler> logger,
        IDeviceRepository deviceRepository,
        IDeviceCommandExecutionRepository commandExecutionRepository,
        ICapabilityCommandValidator capabilityCommandValidator,
        IDeviceCommandSender deviceCommandSender,
        IUnitOfWork unitOfWork
    )
    {
        _logger = logger;
        _deviceRepository = deviceRepository;
        _commandExecutionRepository = commandExecutionRepository;
        _capabilityCommandValidator = capabilityCommandValidator;
        _deviceCommandSender = deviceCommandSender;
        _unitOfWork = unitOfWork;
    }

    public async Task<Guid> Handle(SendDeviceCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.EndpointId))
            throw new DomainValidationException("endpointId is required.");

        var device = await _deviceRepository.GetById(request.DeviceId, cancellationToken)
            ?? throw new DeviceNotFoundException(request.DeviceId);

        var capability = device.FindCapability(request.CapabilityId, request.EndpointId);

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

        var validatedPayload = _capabilityCommandValidator.NormalizeAndValidate(
            capability,
            request.Operation,
            request.Value
        );

        var requestPayload = validatedPayload is null
            ? null
            : JsonSerializer.Serialize(validatedPayload);

        var execution = DeviceCommandExecution.Create(
            id: request.CommandExecutionId,
            deviceId: device.Id,
            capabilityId: capability.CapabilityId,
            endpointId: request.EndpointId,
            correlationId: request.CorrelationId,
            operation: request.Operation,
            requestPayload: requestPayload
        );

        await _commandExecutionRepository.Add(execution);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var command = new DeviceCommandRequest(
            device.Id,
            request.EndpointId,
            capability.CapabilityId,
            request.Operation,
            validatedPayload,
            request.CorrelationId
        );

        try
        {
            await _deviceCommandSender.Send(command, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            execution.MarkFailed(ex.Message);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            throw new DeviceCommandDispatchException(execution.Id, ex.Message, ex);
        }

        return request.CommandExecutionId;
    }
}
