import { api } from "../../services/http";
import type {
  HomeListElement,
  HomeDetails,
  HomeAddRequest,
  HomeAddResponse,
  HomeUpdateRequest
} from "./home.types";

const basePath = "/homes";

export function getHomes() {
  return api<HomeListElement[]>(`${basePath}`);
}

export function getHomeDetails(homeId: string) {
  return api<HomeDetails>(`${basePath}/${homeId}`);
}

export function addHome(request: HomeAddRequest) {
  return api<HomeAddResponse>(`${basePath}`, {
    method: "POST",
    body: JSON.stringify(request)
  });
}

export function updateHome(homeId: string, request: HomeUpdateRequest) {
  return api(`${basePath}/${homeId}`, {
    method: "PATCH",
    body: JSON.stringify(request)
  });
}

export function deleteHome(id: string) {
  return api(`${basePath}/${id}`, {
    method: "DELETE"
  });
}
