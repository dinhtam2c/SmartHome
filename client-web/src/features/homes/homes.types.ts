import type { CapabilityRegistryMetadata } from "@/features/capabilities";
import type { CapabilityRole } from "@/features/shared/capabilityRoleUtils";

export interface HomeUnassignedCapabilityOverviewDto {
  id: string;
  capabilityId: string;
  capabilityVersion: number;
  supportedOperations: string[];
  lastReportedAt: number;
  role: CapabilityRole;
  metadata: CapabilityRegistryMetadata | null;
  state: unknown;
  hasRegistryMetadata: boolean;
}

export interface HomeUnassignedDeviceEndpointOverviewDto {
  endpointId: string;
  name: string | null;
  capabilities: HomeUnassignedCapabilityOverviewDto[];
}

export interface HomeUnassignedDeviceOverviewDto {
  id: string;
  name: string;
  isOnline: boolean;
  endpoints: HomeUnassignedDeviceEndpointOverviewDto[];
}

export interface HomeListItemDto {
  id: string;
  name: string;
  description: string | null;
  createdAt: number;
}

export interface HomeDetailDto {
  id: string;
  name: string;
  description: string | null;
  createdAt: number;
  roomCount: number;
  deviceCount: number;
  onlineDeviceCount: number;
  rooms: HomeRoomOverviewDto[];
  scenes: HomeSceneSummaryDto[];
  unassignedDevices: HomeUnassignedDeviceOverviewDto[];
}

export interface HomeSceneSummaryDto {
  id: string;
  name: string;
  isEnabled: boolean;
}

export interface HomeRoomOverviewDto {
  id: string;
  name: string;
  description: string | null;
  deviceCount: number;
  onlineDeviceCount: number;
  temperature: number | null;
  humidity: number | null;
}

export interface HomeCreateRequest {
  name: string;
  description: string;
}

export interface HomeCreateResponse {
  id: string;
}

export interface HomeUpdateRequest {
  name?: string;
  description?: string | null;
}

export interface HomeSceneBuilderCapabilityDto {
  capabilityId: string;
  capabilityVersion: number;
  supportedOperations: string[];
  lastReportedAt: number;
  state: Record<string, unknown> | null;
}

export interface HomeSceneBuilderEndpointDto {
  endpointId: string;
  name: string | null;
  capabilities: HomeSceneBuilderCapabilityDto[];
}

export interface HomeSceneBuilderDeviceDto {
  id: string;
  name: string;
  firmwareVersion: string | null;
  isOnline: boolean;
  provisionedAt: number | null;
  lastSeenAt: number | null;
  uptime: number | null;
  roomId: string | null;
  roomName: string | null;
  endpoints: HomeSceneBuilderEndpointDto[];
}
