export type ActionType = "setState" | "invokeOperation";
export type ActionSetSection = "main" | "before" | "onSuccess" | "onFailure";
export type ActionExecutionMode = "sequential" | "parallel";

export interface ActionTargetRequest {
  deviceId: string;
  endpointId: string;
  capabilityId: string;
}

export interface SetStateActionRequest {
  type: "setState";
  target: ActionTargetRequest;
  state: Record<string, unknown>;
}

export interface InvokeOperationActionRequest {
  type: "invokeOperation";
  target: ActionTargetRequest;
  operation: string;
  payload: Record<string, unknown>;
}

export type ActionRequest = SetStateActionRequest | InvokeOperationActionRequest;

export interface ActionSetExecutionPolicyRequest {
  mode: ActionExecutionMode;
  continueOnError: boolean;
}

export interface ActionSetHooksRequest {
  before: ActionRequest[];
  onSuccess: ActionRequest[];
  onFailure: ActionRequest[];
}

export interface ActionSetRequest {
  executionPolicy: ActionSetExecutionPolicyRequest;
  actions: ActionRequest[];
  hooks: ActionSetHooksRequest;
}

export type ActionTargetDto = ActionTargetRequest;

export interface SetStateActionDto {
  id: string;
  type: "setState";
  target: ActionTargetDto;
  state: Record<string, unknown>;
  order: number;
}

export interface InvokeOperationActionDto {
  id: string;
  type: "invokeOperation";
  target: ActionTargetDto;
  operation: string;
  payload: Record<string, unknown>;
  order: number;
}

export type ActionDto = SetStateActionDto | InvokeOperationActionDto;

export interface ActionSetExecutionPolicyDto {
  mode: ActionExecutionMode;
  continueOnError: boolean;
}

export interface ActionSetHooksDto {
  before: ActionDto[];
  onSuccess: ActionDto[];
  onFailure: ActionDto[];
}

export interface ActionSetDto {
  executionPolicy: ActionSetExecutionPolicyDto;
  actions: ActionDto[];
  hooks: ActionSetHooksDto;
}
