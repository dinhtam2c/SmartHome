import { api } from "@/shared/api/http";
import type {
  AddAutomationRuleRequest,
  AutomationCreateResponse,
  AutomationExecuteResponse,
  AutomationRuleDetailDto,
  AutomationRuleListItemDto,
  UpdateAutomationRuleRequest,
} from "../types/automationTypes";

function getAutomationsBasePath(homeId: string) {
  return `/homes/${homeId}/automations`;
}

export function getAutomationRules(homeId: string) {
  return api<AutomationRuleListItemDto[]>(getAutomationsBasePath(homeId));
}

export function getAutomationRuleDetail(homeId: string, ruleId: string) {
  return api<AutomationRuleDetailDto>(`${getAutomationsBasePath(homeId)}/${ruleId}`);
}

export function createAutomationRule(
  homeId: string,
  request: AddAutomationRuleRequest
) {
  return api<AutomationCreateResponse>(getAutomationsBasePath(homeId), {
    method: "POST",
    body: JSON.stringify(request),
  });
}

export function updateAutomationRule(
  homeId: string,
  ruleId: string,
  request: UpdateAutomationRuleRequest
) {
  return api<void>(`${getAutomationsBasePath(homeId)}/${ruleId}`, {
    method: "PATCH",
    body: JSON.stringify(request),
  });
}

export function deleteAutomationRule(homeId: string, ruleId: string) {
  return api<void>(`${getAutomationsBasePath(homeId)}/${ruleId}`, {
    method: "DELETE",
  });
}

export function executeAutomationRule(
  homeId: string,
  ruleId: string
) {
  return api<AutomationExecuteResponse>(
    `${getAutomationsBasePath(homeId)}/${ruleId}/execute`,
    {
      method: "POST",
    }
  );
}
