export interface DeviceListElement {
  id: string;
  identifier: string;
  name: string;
  gatewayName: string | null;
  home: string | null;
}

export interface SensorDetail {
  id: string;
  name: string;
  type: number;
  unit: string;
  min: number;
  max: number;
  accuracy: number;
}

export interface ActuatorDetail {
  id: string;
  name: string;
  type: number;
  supportedStates: string[] | null;
  supportedCommands: string[] | null;
}

export interface DeviceDetails {
  id: string;
  identifier: string;
  name: string;
  gatewayId: string | null;
  gatewayName: string | null;
  locationId: string | null;
  locationName: string | null;
  manufacturer: string | null;
  model: string | null;
  firmwareVersion: string | null;
  createdAt: number;
  updatedAt: number;
  sensors: SensorDetail[] | null;
  actuators: ActuatorDetail[] | null;
}

export interface DeviceAddRequest {
  gatewayId?: string;
  identifier: string;
  name?: string;
}

export interface DeviceAddResponse {
  deviceId: string;
  gatewayId: string | null;
  identifier: string;
  name: string;
}

export interface DeviceLocationAssignRequest {
  locationId: string;
}

export interface DeviceGatewayAssignRequest {
  gatewayId: string;
}

export interface DeviceUpdateRequest {
  name: string;
}
