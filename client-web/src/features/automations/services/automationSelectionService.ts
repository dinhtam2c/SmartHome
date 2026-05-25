import type {
  CapabilityRegistryMap,
  SchemaField,
} from "@/features/capabilities";
import type {
  BuilderCapabilityDto,
  BuilderDeviceDto,
} from "@/features/capability-builder";
import {
  getCapabilityRegistryEntry,
  toSchemaFields,
} from "@/features/capability-builder";

export function getConditionCapabilities(
  capabilities: BuilderCapabilityDto[],
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
  devices: BuilderDeviceDto[],
  deviceId: string
) {
  return devices.find((device) => device.id === deviceId)?.roomId ?? "";
}

export function findConditionField(
  devices: BuilderDeviceDto[],
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
