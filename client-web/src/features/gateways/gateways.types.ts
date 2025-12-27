export interface GatewayListElement {
  id: string;
  name: string | null;
  homeName: string | null;
}

export interface GatewayDetails {
  id: string;
  name: string;
  homeId: string | null;
  homeName: string | null;
  manufacturer: string | null;
  model: string | null;
  firmwareVersion: string;
  mac: string;
  createdAt: number;
  updatedAt: number;
}

export interface GatewayHomeAssignRequest {
  homeId: string;
}
