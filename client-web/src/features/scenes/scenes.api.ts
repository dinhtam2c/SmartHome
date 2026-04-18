import { api } from "@/services/http";
import type {
  AddSceneRequest,
  ExecuteSceneRequest,
  PagedResult,
  SceneCreateResponse,
  SceneDetailDto,
  SceneExecutionDetailDto,
  SceneExecutionListItemDto,
  SceneExecutionStatus,
  SceneExecuteResponse,
  SceneListItemDto,
  UpdateSceneRequest,
} from "./scenes.types";

const homesBasePath = "/homes";

function getScenesBasePath(homeId: string) {
  return `${homesBasePath}/${homeId}/scenes`;
}

export function getScenes(homeId: string) {
  return api<SceneListItemDto[]>(getScenesBasePath(homeId));
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

export function executeScene(homeId: string, sceneId: string, request?: ExecuteSceneRequest) {
  return api<SceneExecuteResponse>(`${getScenesBasePath(homeId)}/${sceneId}/execute`, {
    method: "POST",
    body: request ? JSON.stringify(request) : undefined,
  });
}

export function getSceneExecutions(
  homeId: string,
  sceneId: string,
  params?: {
    status?: SceneExecutionStatus;
    page?: number;
    pageSize?: number;
  }
) {
  const query = new URLSearchParams();

  if (params?.status) {
    query.set("status", params.status);
  }

  if (typeof params?.page === "number") {
    query.set("page", String(params.page));
  }

  if (typeof params?.pageSize === "number") {
    query.set("pageSize", String(params.pageSize));
  }

  const suffix = query.toString() ? `?${query.toString()}` : "";

  return api<PagedResult<SceneExecutionListItemDto>>(
    `${getScenesBasePath(homeId)}/${sceneId}/executions${suffix}`
  );
}

export function getSceneExecutionDetail(
  homeId: string,
  sceneId: string,
  executionId: string
) {
  return api<SceneExecutionDetailDto>(
    `${getScenesBasePath(homeId)}/${sceneId}/executions/${executionId}`
  );
}
