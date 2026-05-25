import { resolveSupportedOperation } from "../../services/deviceCapabilityService";
import {
  getOperationFields,
  resolveOperationFieldRenderPlan,
} from "../../services/deviceOperationFieldService";
import type { DeviceCapabilityDto } from "../../types/deviceTypes";
import type { DeviceCapabilityControlsProps } from "./deviceCapabilityControlTypes";

type OperationRenderPlan = ReturnType<typeof resolveOperationFieldRenderPlan>;

export function isOperationRenderPlanUsable(plan: OperationRenderPlan | null) {
  return Boolean(plan && plan.kind !== "unsupported" && plan.skippedFields.length === 0);
}

export function getDisplayedToggleValue(
  optimisticToggleValues: Record<string, boolean | undefined>,
  capabilityKey: string,
  fallbackValue: boolean | null
) {
  const optimisticValue = optimisticToggleValues[capabilityKey];
  return typeof optimisticValue === "boolean" ? optimisticValue : fallbackValue;
}

export function getBooleanValue(value: unknown) {
  return typeof value === "boolean" ? value : null;
}

export function getNumericValue(value: unknown) {
  if (typeof value === "number") return String(value);
  if (typeof value !== "string" || value.trim() === "") return null;

  return Number.isFinite(Number(value)) ? value : null;
}

export function getSupportedOperation(
  capability: DeviceCapabilityDto,
  operation: string | null | undefined
) {
  return resolveSupportedOperation(capability.supportedOperations, operation);
}

export function canSendFromCapability(capability: DeviceCapabilityDto) {
  return capability.hasRegistryMetadata && capability.supportedOperations.length > 0;
}

export function getOperationFieldsForCapability(
  capability: DeviceCapabilityDto,
  operation: string
) {
  return getOperationFields(
    capability.operations as Record<string, Record<string, unknown>> | null,
    operation
  );
}

export function getOperationContextKey(capability: DeviceCapabilityDto, operation: string) {
  return `${capability.id}|${operation}`;
}

export function createFieldValueUpdater(
  { setAdvancedFieldValuesByContext }: Pick<
    DeviceCapabilityControlsProps,
    "setAdvancedFieldValuesByContext"
  >
) {
  return (contextKey: string, path: string, nextValue: string) => {
    setAdvancedFieldValuesByContext((current) => ({
      ...current,
      [contextKey]: {
        ...(current[contextKey] ?? {}),
        [path]: nextValue,
      },
    }));
  };
}

export function updateInlineCommandValue(
  setInlineCommandValues: DeviceCapabilityControlsProps["setInlineCommandValues"],
  capabilityKey: string,
  nextValue: string
) {
  setInlineCommandValues((current) => ({
    ...current,
    [capabilityKey]: nextValue,
  }));
}
