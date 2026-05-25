export interface SelectableCapabilityDto {
  capabilityId: string;
  capabilityVersion: number;
  supportedOperations: string[];
  lastReportedAt: number;
  state: Record<string, unknown> | null;
}

export interface SelectableEndpointDto {
  endpointId: string;
  name: string | null;
  capabilities: SelectableCapabilityDto[];
}

export interface SelectableDeviceDto {
  id: string;
  name: string;
  category: string;
  firmwareVersion: string | null;
  isOnline: boolean;
  provisionedAt: number | null;
  lastSeenAt: number | null;
  uptime: number | null;
  roomId: string | null;
  roomName: string | null;
  endpoints: SelectableEndpointDto[];
}
