import type {
  CapabilityRegistryMap,
  SchemaField,
} from "@/features/capabilities";
import type {
  SelectableCapabilityDto,
  SelectableDeviceDto,
} from "@/features/capabilities";
import {
  getCapabilityRegistryEntry,
  toSchemaFields,
} from "@/features/capabilities";

export function getConditionCapabilities(
  capabilities: SelectableCapabilityDto[],
  registryMap: CapabilityRegistryMap | undefined
) {
  if (!registryMap) {
    return [];
  }

  return capabilities.filter((capability) => {
    const registryEntry = getCapabilityRegistryEntry(capability, registryMap);
    return registryEntry?.role === "Sensor" || registryEntry?.role === "Control";
  });
}

export function resolveDeviceRoomId(
  devices: SelectableDeviceDto[],
  deviceId: string
) {
  return devices.find((device) => device.id === deviceId)?.roomId ?? "";
}

export function findConditionField(
  devices: SelectableDeviceDto[],
  registryMap: CapabilityRegistryMap | undefined,
  condition: {
    deviceId: string;
    endpointId: string;
    capabilityId: string;
    fieldPath: string;
  }
): SchemaField | undefined {
  const capability = devices
    .find((device) => device.id === condition.deviceId)
    ?.endpoints.find((endpoint) => endpoint.endpointId === condition.endpointId)
    ?.capabilities.find((candidate) => candidate.capabilityId === condition.capabilityId);
  const registryEntry = getCapabilityRegistryEntry(capability, registryMap);

  return registryEntry
    ? toSchemaFields(registryEntry.stateSchema).find(
      (field) => field.path === condition.fieldPath
    )
    : undefined;
}
