import type { CapabilityRole } from "@/features/shared/capabilityRoleUtils";

export type JsonSchemaObject = Record<string, unknown>;

export type CapabilityPrimaryMetadata = {
  state?: string;
  operation?: string;
  [key: string]: unknown;
};

export type CapabilityRegistryMetadata = Record<string, unknown> & {
  defaultName?: string;
  primary?: CapabilityPrimaryMetadata;
  unit?: string;
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
