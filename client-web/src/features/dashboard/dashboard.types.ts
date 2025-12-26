export interface DashboardHomeListItemDto {
  id: string;
  name: string;
  description?: string;
}

export interface DashboardHomeDto {
  id: string;
  name: string;
  description?: string;
  summary: DashboardHomeSummaryDto;
  locations: DashboardLocationListItemDto[];
}

export interface DashboardHomeSummaryDto {
  deviceCount: number;
  onlineDeviceCount: number;
}

export interface DashboardLocationListItemDto {
  id: string;
  name: string;
  description?: string;
  deviceCount: number;
  onlineDeviceCount: number;
}

export interface DashboardLocationDto {
  id: string;
  name: string;
  description?: string;
  summary: DashboardLocationSummaryDto;
  devices: DashboardDeviceListItemDto[];
}

export interface DashboardLocationSummaryDto {
  deviceCount: number;
  onlineDeviceCount: number;
}

export interface DashboardDeviceListItemDto {
  id: string;
  name: string;
  isOnline: boolean;
  latestSensorData: DashboardSensorDataDto[];
  actuatorStates: DashboardActuatorStateDto[];
}

export interface DashboardSensorDataDto {
  sensorId: string;
  sensorName: string;
  type: string;
  unit: string;
  value: number;
  timestamp: number;
}

export interface DashboardActuatorStateDto {
  actuatorId: string;
  actuatorName: string;
  states: {
    [key: string]: string;
  };
}

export interface DashboardDeviceDto {
  id: string;
  name: string;
  isOnline: boolean;
  upTime: number;
  lastSeenAt: number;
  latestSensorData: DashboardSensorDataDto[];
  actuatorStates: DashboardActuatorStateDto[];
}

export interface DeviceCommandRequest {
  actuatorId: string;
  command: string;
  parameters?: unknown;
}
