import {
  getCapabilityRegistryKey,
  type CapabilityRegistryEntry,
  type CapabilityRegistryMap,
} from "@/features/capabilities";
import type {
  BuilderCapabilityDto,
  BuilderDeviceDto,
  BuilderEndpointDto,
} from "../types/builderTypes";

type CapabilityFilter = (
  capabilities: BuilderCapabilityDto[],
  registryMap?: CapabilityRegistryMap
) => BuilderCapabilityDto[];

type CapabilityDeviceSelection = {
  selectedRoomId: string;
  roomDevices: BuilderDeviceDto[];
  selectedDevice: BuilderDeviceDto | undefined;
  selectedEndpoints: BuilderEndpointDto[];
  selectedEndpoint: BuilderEndpointDto | undefined;
  selectedCapabilities: BuilderCapabilityDto[];
  selectedCapability: BuilderCapabilityDto | undefined;
};

function filterDeviceEndpoints(
  device: BuilderDeviceDto,
  registryMap: CapabilityRegistryMap | undefined,
  filterCapabilities: CapabilityFilter
): BuilderEndpointDto[] {
  return device.endpoints.filter(
    (endpoint) => filterCapabilities(endpoint.capabilities, registryMap).length > 0
  );
}

function filterSelectableDevices(
  devices: BuilderDeviceDto[],
  registryMap: CapabilityRegistryMap | undefined,
  filterCapabilities: CapabilityFilter
): BuilderDeviceDto[] {
  return devices
    .map((device) => ({
      ...device,
      endpoints: filterDeviceEndpoints(device, registryMap, filterCapabilities),
    }))
    .filter((device) => device.endpoints.length > 0);
}

export function getCapabilityRegistryEntry(
  capability: BuilderCapabilityDto | null | undefined,
  registryMap: CapabilityRegistryMap | undefined
): CapabilityRegistryEntry | null {
  if (!capability || !registryMap) {
    return null;
  }

  return (
    registryMap.get(
      getCapabilityRegistryKey(
        capability.capabilityId,
        capability.capabilityVersion
      )
    ) ?? null
  );
}

export function getControlCapabilities(
  capabilities: BuilderCapabilityDto[],
  registryMap: CapabilityRegistryMap | undefined
): BuilderCapabilityDto[] {
  if (!registryMap) {
    return [];
  }

  return capabilities.filter((capability) => {
    const registryEntry = getCapabilityRegistryEntry(capability, registryMap);
    return registryEntry?.role === "Control" && !!registryEntry.applyStrategy;
  });
}

export function getOperationCapabilities(
  capabilities: BuilderCapabilityDto[],
  registryMap: CapabilityRegistryMap | undefined
): BuilderCapabilityDto[] {
  if (!registryMap) {
    return [];
  }

  return capabilities.filter((capability) => {
    const registryEntry = getCapabilityRegistryEntry(capability, registryMap);

    if (!registryEntry || registryEntry.role === "Sensor") {
      return false;
    }

    const registryOperations = Object.keys(registryEntry.operations ?? {});
    if (registryOperations.length === 0) {
      return false;
    }

    const supportedOperations = Array.isArray(capability.supportedOperations)
      ? capability.supportedOperations
      : [];

    return supportedOperations.length === 0
      ? false
      : registryOperations.some((operation) =>
        supportedOperations.some(
          (supportedOperation) =>
            supportedOperation.trim().toLowerCase() === operation.trim().toLowerCase()
        )
      );
  });
}

export function resolveCapabilityDeviceSelection({
  roomId,
  deviceId,
  endpointId,
  capabilityId,
  availableDevices,
  availableDevicesByRoom,
  registryMap,
  filterCapabilities,
}: {
  roomId: string;
  deviceId: string;
  endpointId: string;
  capabilityId: string;
  availableDevices: BuilderDeviceDto[];
  availableDevicesByRoom?: Record<string, BuilderDeviceDto[]>;
  registryMap?: CapabilityRegistryMap;
  filterCapabilities: CapabilityFilter;
}): CapabilityDeviceSelection {
  const selectedDeviceFromAll = availableDevices.find(
    (device) => device.id === deviceId
  );
  const normalizedRoomId = roomId.trim();
  const selectedRoomId = normalizedRoomId || selectedDeviceFromAll?.roomId || "";
  const roomDeviceCandidates = selectedRoomId
    ? (availableDevicesByRoom?.[selectedRoomId] ??
      availableDevices.filter((device) => device.roomId === selectedRoomId))
    : availableDevices;
  const roomDevices = filterSelectableDevices(
    roomDeviceCandidates,
    registryMap,
    filterCapabilities
  );
  const selectedDevice = roomDevices.find((device) => device.id === deviceId);
  const selectedEndpoints = selectedDevice?.endpoints ?? [];
  const selectedEndpoint = selectedEndpoints.find(
    (endpoint) => endpoint.endpointId === endpointId
  );
  const selectedCapabilities = filterCapabilities(
    selectedEndpoint?.capabilities ?? [],
    registryMap
  );
  const selectedCapability = selectedCapabilities.find(
    (capability) => capability.capabilityId === capabilityId
  );

  return {
    selectedRoomId,
    roomDevices,
    selectedDevice,
    selectedEndpoints,
    selectedEndpoint,
    selectedCapabilities,
    selectedCapability,
  };
}
