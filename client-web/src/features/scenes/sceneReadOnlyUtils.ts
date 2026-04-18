import {
  getCapabilityRegistryKey,
  type CapabilityRegistryMap,
} from "@/features/capabilities";
import type {
  HomeSceneBuilderCapabilityDto,
  HomeSceneBuilderDeviceDto,
} from "@/features/homes/homes.types";
import { isPlainObject, toSchemaFields } from "./components/schemaFieldUtils";
import type { SceneSideEffectDraft, SceneTargetDraft } from "./sceneFormUtils";

function findCapabilityByTarget(
  target: Pick<SceneTargetDraft, "deviceId" | "endpointId" | "capabilityId">,
  devices: HomeSceneBuilderDeviceDto[]
) {
  const device = devices.find((item) => item.id === target.deviceId);
  const endpoint = device?.endpoints.find(
    (item) => item.endpointId === target.endpointId
  );

  return (
    endpoint?.capabilities.find(
      (item) => item.capabilityId === target.capabilityId
    ) ?? null
  );
}

function findCapabilityBySideEffect(
  sideEffect: Pick<
    SceneSideEffectDraft,
    "deviceId" | "endpointId" | "capabilityId"
  >,
  devices: HomeSceneBuilderDeviceDto[]
) {
  const device = devices.find((item) => item.id === sideEffect.deviceId);
  const endpoint = device?.endpoints.find(
    (item) => item.endpointId === sideEffect.endpointId
  );

  return (
    endpoint?.capabilities.find(
      (item) => item.capabilityId === sideEffect.capabilityId
    ) ?? null
  );
}

function getRegistryEntry(
  capability: HomeSceneBuilderCapabilityDto | null,
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

export function getTargetReadOnlyPaths(
  target: SceneTargetDraft,
  devices: HomeSceneBuilderDeviceDto[],
  registryMap: CapabilityRegistryMap | undefined
) {
  const capability = findCapabilityByTarget(target, devices);
  const registryEntry = getRegistryEntry(capability, registryMap);

  return getReadOnlyPathsFromSchema(registryEntry?.stateSchema ?? null);
}

export function getSideEffectReadOnlyPaths(
  sideEffect: SceneSideEffectDraft,
  devices: HomeSceneBuilderDeviceDto[],
  registryMap: CapabilityRegistryMap | undefined
) {
  const capability = findCapabilityBySideEffect(sideEffect, devices);
  const registryEntry = getRegistryEntry(capability, registryMap);

  if (!registryEntry) {
    return [];
  }

  const operationSchema = resolveOperationSchema(
    registryEntry.operations,
    sideEffect.operation
  );

  if (!operationSchema) {
    return [];
  }

  return getReadOnlyPathsFromSchema(operationSchema);
}
