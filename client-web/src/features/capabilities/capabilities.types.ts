import type { CapabilityRole } from "@/features/capabilities/capabilityRoleUtils";

export type JsonSchemaObject = Record<string, unknown>;

export type CapabilityPrimaryMetadata = {
  state?: string;
  operation?: string;
  [key: string]: unknown;
};

export type CapabilityVisualIcon =
  | "brightness"
  | "buzzer"
  | "effect"
  | "fan"
  | "humidity"
  | "illuminance"
  | "light"
  | "lock"
  | "motion"
  | "palette"
  | "power"
  | "temperature"
  | "timer";

export type CapabilityVisualTone =
  | "amber"
  | "blue"
  | "green"
  | "neutral"
  | "red"
  | "violet";

export type CapabilityVisualMetadata = {
  icon?: CapabilityVisualIcon;
  image?: string;
  precision?: number;
  tone?: CapabilityVisualTone;
};

export type CapabilityControlCommitPolicy =
  | "commitOnRelease"
  | "formOnly"
  | "immediate"
  | "liveThrottle";

export type CapabilityControlMetadata = {
  commitPolicy?: CapabilityControlCommitPolicy;
  throttleMs?: number;
};

export type CapabilityRegistryMetadata = Record<string, unknown> & {
  defaultName?: string;
  control?: CapabilityControlMetadata;
  primary?: CapabilityPrimaryMetadata;
  unit?: string;
  visual?: CapabilityVisualMetadata;
  overviewVisible?: boolean;
  order?: number;
};

export type CapabilityRegistryPrerequisite = {
  capabilityId: string;
  requiredState: JsonSchemaObject;
  autoFix?: boolean;
};

export type CapabilityRegistryApplyStrategy = {
  operation: string;
  stateMapping: Record<string, string>;
  readOnlyFields?: string[];
  partialUpdate?: boolean;
};

export interface CapabilityRegistryEntry {
  id: string;
  version: number;
  role: Exclude<CapabilityRole, "Unknown">;
  stateSchema: JsonSchemaObject;
  operations: Record<string, JsonSchemaObject>;
  metadata: CapabilityRegistryMetadata;
  conflictsWith?: string[];
  prerequisite?: CapabilityRegistryPrerequisite | null;
  applyStrategy?: CapabilityRegistryApplyStrategy | null;
}

export type CapabilityRegistryMap = Map<string, CapabilityRegistryEntry>;
