export interface LocationListElement {
  id: string;
  homeId: string;
  name: string;
  description: string | null;
  createdAt: number;
  updatedAt: number;
}

export interface LocationDetails {
  id: string;
  homeId: string;
  name: string;
  description: string | null;
  createdAt: number;
  updatedAt: number;
}

export interface LocationAddRequest {
  homeId: string;
  name: string;
  description: string;
}

export interface LocationAddResponse {
  id: string;
  homeId: string;
  name: string;
  description: string | null;
  createdAt: number;
}

export interface LocationUpdateRequest {
  name: string;
  description: string;
}
