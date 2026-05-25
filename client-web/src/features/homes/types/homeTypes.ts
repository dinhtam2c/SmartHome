import type { CapabilityRegistryMetadata } from "@/features/capabilities";
import type { CapabilityRole } from "@/features/capabilities/capabilityRoleUtils";

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
  category: string;
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
  floorCount: number;
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
