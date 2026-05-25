import { api } from "@/shared/api/http";
import type {
  AddSceneRequest,
  SceneCreateResponse,
  SceneDetailDto,
  SceneExecuteResponse,
  UpdateSceneRequest,
} from "../types/sceneTypes";

const homesBasePath = "/homes";

function getScenesBasePath(homeId: string) {
  return `${homesBasePath}/${homeId}/scenes`;
}

export function getSceneDetail(homeId: string, sceneId: string) {
  return api<SceneDetailDto>(`${getScenesBasePath(homeId)}/${sceneId}`);
}

export function createScene(homeId: string, request: AddSceneRequest) {
  return api<SceneCreateResponse>(getScenesBasePath(homeId), {
    method: "POST",
    body: JSON.stringify(request),
  });
}

export function updateScene(homeId: string, sceneId: string, request: UpdateSceneRequest) {
  return api<void>(`${getScenesBasePath(homeId)}/${sceneId}`, {
    method: "PATCH",
    body: JSON.stringify(request),
  });
}

export function deleteScene(homeId: string, sceneId: string) {
  return api<void>(`${getScenesBasePath(homeId)}/${sceneId}`, {
    method: "DELETE",
  });
}

export function executeScene(homeId: string, sceneId: string) {
  return api<SceneExecuteResponse>(`${getScenesBasePath(homeId)}/${sceneId}/execute`, {
    method: "POST",
  });
}
