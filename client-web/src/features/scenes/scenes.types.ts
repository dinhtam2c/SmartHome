export interface SceneTargetRequest {
  deviceId: string;
  endpointId: string;
  capabilityId: string;
  desiredState: Record<string, unknown>;
}

export type SceneSideEffectTiming =
  | "BeforeTargets"
  | "AfterDispatch"
  | "AfterVerify";

export interface SceneSideEffectRequest {
  deviceId: string;
  endpointId: string;
  capabilityId: string;
  operation: string;
  params: Record<string, unknown>;
  timing: SceneSideEffectTiming;
  delayMs: number;
}

export interface AddSceneRequest {
  name: string;
  description?: string | null;
  isEnabled: boolean;
  targets: SceneTargetRequest[];
  sideEffects?: SceneSideEffectRequest[] | null;
}

export interface UpdateSceneRequest {
  name?: string;
  description?: string | null;
  isEnabled?: boolean;
  targets?: SceneTargetRequest[] | null;
  sideEffects?: SceneSideEffectRequest[] | null;
}

export interface ExecuteSceneRequest {
  triggerSource?: string | null;
  onlyEndpoints?: string[] | null;
  excludeCapabilities?: string[] | null;
}

export interface SceneListItemDto {
  id: string;
  homeId: string;
  name: string;
  description: string | null;
  isEnabled: boolean;
  targetCount: number;
  updatedAt: number;
}

export interface SceneDetailTargetDto {
  id: string;
  deviceId: string;
  endpointId: string;
  capabilityId: string;
  desiredState: Record<string, unknown>;
  order: number;
}

export interface SceneDetailDto {
  id: string;
  homeId: string;
  name: string;
  description: string | null;
  isEnabled: boolean;
  createdAt: number;
  updatedAt: number;
  targets: SceneDetailTargetDto[];
  sideEffects: SceneDetailSideEffectDto[];
}

export interface SceneDetailSideEffectDto {
  id: string;
  deviceId: string;
  endpointId: string;
  capabilityId: string;
  operation: string;
  params: Record<string, unknown>;
  timing: SceneSideEffectTiming;
  delayMs: number;
  order: number;
}

export interface SceneCreateResponse {
  id: string;
}

export interface SceneExecuteResponse {
  executionId: string;
}

export type SceneExecutionStatus =
  | "Running"
  | "Completed"
  | "CompletedWithErrors";

export type SceneExecutionTargetStatus =
  | "PendingEvaluation"
  | "SkippedAlreadySatisfied"
  | "CommandPending"
  | "CommandAccepted"
  | "CommandCompleted"
  | "Verified"
  | "VerificationFailed"
  | "DeviceNotFound"
  | "CapabilityNotFound"
  | "CapabilityAmbiguous"
  | "UnsupportedCapabilityRole"
  | "CommandGenerationFailed"
  | "CommandDispatchFailed"
  | "CommandFailed"
  | "CommandTimedOut";

export interface SceneExecutionListItemDto {
  id: string;
  sceneId: string;
  homeId: string;
  status: SceneExecutionStatus;
  triggerSource: string | null;
  startedAt: number;
  finishedAt: number | null;
  totalTargets: number;
  pendingTargets: number;
  skippedTargets: number;
  successfulTargets: number;
  failedTargets: number;
}

export interface SceneExecutionTargetDto {
  id: string;
  sceneTargetId: string;
  deviceId: string;
  endpointId: string;
  capabilityId: string;
  desiredState: Record<string, unknown>;
  status: SceneExecutionTargetStatus;
  commandCorrelationId: string | null;
  unresolvedDiff: Record<string, unknown> | null;
  error: string | null;
  order: number;
  updatedAt: number;
}

export type SceneExecutionSideEffectStatus =
  | "Pending"
  | "Succeeded"
  | "Failed"
  | "Skipped";

export interface SceneExecutionSideEffectDto {
  id: string;
  sceneSideEffectId: string;
  deviceId: string;
  endpointId: string;
  capabilityId: string;
  operation: string;
  params: Record<string, unknown>;
  timing: SceneSideEffectTiming;
  delayMs: number;
  status: SceneExecutionSideEffectStatus;
  commandCorrelationId: string | null;
  error: string | null;
  order: number;
  updatedAt: number;
}

export interface SceneExecutionDetailDto {
  id: string;
  sceneId: string;
  homeId: string;
  status: SceneExecutionStatus;
  triggerSource: string | null;
  startedAt: number;
  finishedAt: number | null;
  totalTargets: number;
  pendingTargets: number;
  skippedTargets: number;
  successfulTargets: number;
  failedTargets: number;
  targets: SceneExecutionTargetDto[];
  sideEffects: SceneExecutionSideEffectDto[];
}

export interface PagedResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
  hasNextPage: boolean;
  hasPreviousPage: boolean;
}
