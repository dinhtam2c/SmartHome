import type { CapabilityRegistryMetadata } from "@/features/capabilities";
import type { CapabilityRole } from "@/features/shared/capabilityRoleUtils";

export interface RoomCapabilityOverviewDto {
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

export interface RoomDeviceEndpointOverviewDto {
  endpointId: string;
  name: string | null;
  capabilities: RoomCapabilityOverviewDto[];
}

export interface RoomDeviceOverviewDto {
  id: string;
  name: string;
  isOnline: boolean;
  endpoints: RoomDeviceEndpointOverviewDto[];
}

export interface RoomDetailDto {
  id: string;
  name: string;
  description: string | null;
  createdAt: number;
  deviceCount: number;
  onlineDeviceCount: number;
  temperature: number | null;
  humidity: number | null;
  devices: RoomDeviceOverviewDto[];
}

export interface RoomCreateRequest {
  name: string;
  description: string | null;
}

export interface RoomCreateResponse {
  id: string;
}

export interface RoomUpdateRequest {
  name?: string;
  description?: string | null;
}
