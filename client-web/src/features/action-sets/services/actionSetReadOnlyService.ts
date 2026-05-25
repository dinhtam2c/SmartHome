import {
  getCapabilityRegistryKey,
  type CapabilityRegistryMap,
} from "@/features/capabilities";
import type { BuilderCapabilityDto, BuilderDeviceDto } from "@/features/capability-builder";
import { isPlainObject, toSchemaFields } from "@/features/capability-builder";
import type { ActionDraft } from "./actionSetFormService";

function findCapabilityByAction(
  action: Pick<ActionDraft, "deviceId" | "endpointId" | "capabilityId">,
  devices: BuilderDeviceDto[]
) {
  const device = devices.find((deviceItem) => deviceItem.id === action.deviceId);
  const endpoint = device?.endpoints.find(
    (endpointItem) => endpointItem.endpointId === action.endpointId
  );

  return (
    endpoint?.capabilities.find(
      (capabilityItem) => capabilityItem.capabilityId === action.capabilityId
    ) ?? null
  );
}

function getRegistryEntry(
  capability: BuilderCapabilityDto | null,
  registryMap: CapabilityRegistryMap | undefined
) {
  if (!capability || !registryMap) {
    return null;
  }

  return (
    registryMap.get(
      getCapabilityRegistryKey(capability.capabilityId, capability.capabilityVersion)
    ) ?? null
  );
}

function resolveOperationSchema(
  operations: Record<string, Record<string, unknown>>,
  operation: string
) {
  const normalizedOperation = operation.trim().toLowerCase();
  if (!normalizedOperation) {
    return null;
  }

  const exact = operations[operation];
  if (exact && isPlainObject(exact)) {
    return exact;
  }

  const matchedKey = Object.keys(operations).find(
    (key) => key.trim().toLowerCase() === normalizedOperation
  );

  if (!matchedKey) {
    return null;
  }

  const matched = operations[matchedKey];
  return isPlainObject(matched) ? matched : null;
}

function getReadOnlyPathsFromSchema(schema: Record<string, unknown> | null | undefined) {
  if (!schema || !isPlainObject(schema)) {
    return [];
  }

  return toSchemaFields(schema)
    .filter((field) => field.readOnly)
    .map((field) => field.path.trim())
    .filter((path) => path !== "");
}

export function getActionReadOnlyPaths(
  action: ActionDraft,
  devices: BuilderDeviceDto[],
  registryMap: CapabilityRegistryMap | undefined
) {
  const capability = findCapabilityByAction(action, devices);
  const registryEntry = getRegistryEntry(capability, registryMap);

  if (!registryEntry) {
    return [];
  }

  if (action.type === "setState") {
    return getReadOnlyPathsFromSchema(registryEntry.stateSchema);
  }

  const operationSchema = resolveOperationSchema(
    registryEntry.operations,
    action.operation
  );

  return getReadOnlyPathsFromSchema(operationSchema);
}
