export interface HomeListElement {
  id: string;
  name: string;
  description?: string;
  createdAt: number;
  updatedAt: number;
}

export interface HomeDetails {
  id: string;
  name: string;
  description: string;
  createdAt: number;
  updatedAt: number;
}

export interface HomeAddRequest {
  name: string;
  description: string;
}

export interface HomeAddResponse {
  id: string;
  name: string;
  description?: string;
  createdAt: number;
}

export interface HomeUpdateRequest {
  name: string;
  description: string;
}
