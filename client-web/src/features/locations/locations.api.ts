import { api } from "@/services/http";
import type {
  LocationListElement,
  LocationDetails,
  LocationAddRequest,
  LocationAddResponse,
  LocationUpdateRequest,
} from "./locations.types";

const basePath = "/locations";

export function getLocations(homeId?: string) {
  const url = homeId ? `${basePath}?homeId=${homeId}` : basePath;
  return api<LocationListElement[]>(url);
}

export function getLocationDetails(locationId: string) {
  return api<LocationDetails>(`${basePath}/${locationId}`);
}

export function addLocation(request: LocationAddRequest) {
  return api<LocationAddResponse>(`${basePath}`, {
    method: "POST",
    body: JSON.stringify(request),
  });
}

export function updateLocation(
  locationId: string,
  request: LocationUpdateRequest
) {
  return api(`${basePath}/${locationId}`, {
    method: "PATCH",
    body: JSON.stringify(request),
  });
}

export function deleteLocation(id: string) {
  return api(`${basePath}/${id}`, {
    method: "DELETE",
  });
}
