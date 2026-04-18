import type {
  CapabilityRegistryMetadata,
  JsonSchemaObject,
} from "@/features/capabilities/capabilities.types";
import type { CapabilityRole } from "@/features/shared/capabilityRoleUtils";

export type DeviceProtocol = "DirectMqtt" | "GatewayZigbee" | "MatterNative";
export type ProvisionState = "PENDING" | "COMPLETED";

export interface DeviceListItemDto {
  id: string;
  name: string;
  homeId: string | null;
  homeName: string | null;
  roomId: string | null;
  roomName: string | null;
  isOnline: boolean;
  provisionState: ProvisionState;
  lastSeenAt: number | null;
  provisionedAt: number | null;
  macAddress: string | null;
  protocol: DeviceProtocol | null;
  firmwareVersion: string | null;
}

export interface DeviceEndpointCapabilityRuntimeDto {
  capabilityId: string;
  capabilityVersion: number;
  supportedOperations: string[];
  lastReportedAt: number;
  state: unknown;
}

export interface DeviceEndpointDto {
  id: string;
  endpointId: string;
  name: string | null;
  capabilities: DeviceEndpointCapabilityRuntimeDto[];
}

export interface DeviceCapabilityDto {
  id: string;
  endpointKey: string;
  capabilityId: string;
  capabilityVersion: number;
  endpointId: string;
  endpointName: string | null;
  supportedOperations: string[];
  state: unknown;
  lastReportedAt: number;
  hasRegistryMetadata: boolean;
  role: CapabilityRole;
  metadata: CapabilityRegistryMetadata | null;
  stateSchema: JsonSchemaObject | null;
  operations: Record<string, JsonSchemaObject> | null;
}

export interface DeviceCommandExecutionDto {
  id: string;
  deviceId: string;
  capabilityId: string;
  endpointId: string;
  correlationId: string;
  operation: string;
  status: "Pending" | "Accepted" | "Completed" | "Failed" | "TimedOut";
  requestPayload: string | null;
  resultPayload: string | null;
  error: string | null;
  requestedAt: number;
}

export type DeviceCommandStatus = DeviceCommandExecutionDto["status"];

export interface DeviceCapabilityHistoryPointDto {
  id: string;
  deviceId: string;
  capabilityId: string;
  endpointId: string;
  statePayload: string;
  state: unknown;
  reportedAt: number;
}

export interface DeviceDetailDto {
  id: string;
  homeId: string | null;
  homeName: string | null;
  macAddress: string;
  name: string;
  roomId: string | null;
  roomName: string | null;
  firmwareVersion: string | null;
  protocol: DeviceProtocol;
  provisionCode: string | null;
  createdAt: number;
  endpoints: DeviceEndpointDto[];
  capabilities: DeviceCapabilityDto[];
  isOnline: boolean;
  provisionState: ProvisionState;
  lastSeenAt: number;
  provisionedAt: number | null;
  uptime: number;
}

export interface DeviceCreateRequest {
  homeId: string;
  roomId?: string | null;
  provisionCode: string;
}

export interface DeviceCreateResponse {
  id: string;
}

export interface DeviceUpdateRequest {
  name: string;
}

export interface DeviceRoomAssignRequest {
  roomId: string;
}

export interface PagedResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
  hasNextPage: boolean;
  hasPreviousPage: boolean;
}

export interface DeviceCommandRequest {
  capabilityId: string;
  endpointId: string;
  operation: string;
  value: unknown;
  correlationId?: string;
}

export type DeviceProvisionMessage = {
  name: string;
  macAddress: string;
  firmwareVersion: string;
  protocol: DeviceProtocol;
  capabilities: DeviceCapabilityProvisionMessage[];
};

export type DeviceCapabilityProvisionMessage = {
  capabilityId: string;
  capabilityVersion: number;
  supportedOperations: string[] | null;
  endpointId: string;
  state: object | null;
};
