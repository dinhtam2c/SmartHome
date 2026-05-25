import type {
  ActionSetDto,
  ActionSetRequest,
} from "@/features/action-sets";

export interface AddSceneRequest {
  name: string;
  description?: string | null;
  isEnabled: boolean;
  actionSet: ActionSetRequest;
}

export interface UpdateSceneRequest {
  name?: string;
  description?: string | null;
  isEnabled?: boolean;
  actionSet?: ActionSetRequest | null;
}

export interface SceneDetailDto {
  id: string;
  homeId: string;
  name: string;
  description: string | null;
  isEnabled: boolean;
  createdAt: number;
  updatedAt: number;
  actionSet: ActionSetDto;
}

export interface SceneCreateResponse {
  id: string;
}

export interface SceneExecuteResponse {
  executionId: string;
}
