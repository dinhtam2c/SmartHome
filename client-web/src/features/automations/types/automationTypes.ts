import type {
  ActionSetDto,
  ActionSetRequest,
} from "@/features/action-sets";

export type AutomationConditionLogic = "All" | "Any";
export type AutomationDayOfWeek =
  | "Sunday"
  | "Monday"
  | "Tuesday"
  | "Wednesday"
  | "Thursday"
  | "Friday"
  | "Saturday";

export type AutomationConditionOperator =
  | "Equals"
  | "NotEquals"
  | "GreaterThan"
  | "GreaterThanOrEqual"
  | "LessThan"
  | "LessThanOrEqual"
  | "Between";

export interface AutomationConditionRequest {
  deviceId?: string | null;
  endpointId?: string | null;
  capabilityId?: string | null;
  fieldPath?: string | null;
  operator?: AutomationConditionOperator | null;
  compareValue?: unknown;
}

export interface AutomationTimeWindowRequest {
  enabled: boolean;
  startTime?: string | null;
  endTime?: string | null;
  daysOfWeek?: AutomationDayOfWeek[] | null;
}

export interface AddAutomationRuleRequest {
  name: string;
  description?: string | null;
  isEnabled: boolean;
  conditionLogic: AutomationConditionLogic;
  cooldownMs: number;
  conditions: AutomationConditionRequest[];
  timeWindow: AutomationTimeWindowRequest;
  actionSet: ActionSetRequest;
}

export interface UpdateAutomationRuleRequest {
  name?: string;
  description?: string | null;
  isEnabled?: boolean;
  conditionLogic?: AutomationConditionLogic;
  cooldownMs?: number;
  conditions?: AutomationConditionRequest[] | null;
  timeWindow?: AutomationTimeWindowRequest | null;
  actionSet?: ActionSetRequest | null;
}

export interface ExecuteAutomationRuleRequest {
  triggerSource?: string | null;
}

export interface AutomationCreateResponse {
  id: string;
}

export interface AutomationExecuteResponse {
  executionId: string;
}

export interface AutomationConditionSummaryDto {
  deviceId: string | null;
  endpointId: string | null;
  capabilityId: string | null;
  fieldPath: string | null;
  operator: AutomationConditionOperator | null;
  compareValue: unknown;
}

export interface AutomationTimeWindowDto {
  enabled: boolean;
  startTime: string | null;
  endTime: string | null;
  daysOfWeek: AutomationDayOfWeek[];
}

export interface AutomationRuleListItemDto {
  id: string;
  homeId: string;
  name: string;
  description: string | null;
  isEnabled: boolean;
  conditionLogic: AutomationConditionLogic;
  cooldownMs: number;
  conditionCount: number;
  mainActionCount: number;
  hookActionCount: number;
  timeWindow: AutomationTimeWindowDto;
  firstCondition: AutomationConditionSummaryDto | null;
  lastEvaluationResult: boolean | null;
  lastEvaluatedAt: number | null;
  lastTriggeredAt: number | null;
  updatedAt: number;
}

export interface AutomationConditionDetailDto extends AutomationConditionSummaryDto {
  id: string;
  order: number;
}

export interface AutomationRuleDetailDto {
  id: string;
  homeId: string;
  name: string;
  description: string | null;
  isEnabled: boolean;
  conditionLogic: AutomationConditionLogic;
  cooldownMs: number;
  lastEvaluationResult: boolean | null;
  lastEvaluatedAt: number | null;
  lastTriggeredAt: number | null;
  createdAt: number;
  updatedAt: number;
  timeWindow: AutomationTimeWindowDto;
  conditions: AutomationConditionDetailDto[];
  actionSet: ActionSetDto;
}
