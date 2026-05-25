import { useCallback, useState } from "react";
import type {
  AutomationConditionLogic,
  AutomationRuleDetailDto,
} from "../types/automationTypes";
import {
  DEFAULT_COOLDOWN_MS,
  conditionDetailToDraft,
  createEmptyAutomationConditionDraft,
  createDefaultAutomationTimeWindowDraft,
  timeWindowDetailToDraft,
  type AutomationConditionDraft,
  type AutomationTimeWindowDraft,
} from "../services/automationFormService";
import { resolveDeviceRoomId } from "../services/automationSelectionService";
import type { BuilderDeviceDto } from "@/features/capability-builder";
import {
  actionSetDtoToDraft,
  createEmptyActionSetDraft,
  type ActionDraft,
  type ActionSetDraft,
} from "@/features/action-sets";

export type AutomationModalMode = "create" | "edit";

function attachRoomIdToAction(action: ActionDraft, devices: BuilderDeviceDto[]): ActionDraft {
  return {
    ...action,
    roomId: resolveDeviceRoomId(devices, action.deviceId),
  };
}

function attachRoomIdsToActionSetDraft(
  draft: ActionSetDraft,
  devices: BuilderDeviceDto[]
): ActionSetDraft {
  return {
    ...draft,
    actions: draft.actions.map((action) => attachRoomIdToAction(action, devices)),
    hooks: {
      before: draft.hooks.before.map((action) => attachRoomIdToAction(action, devices)),
      onSuccess: draft.hooks.onSuccess.map((action) => attachRoomIdToAction(action, devices)),
      onFailure: draft.hooks.onFailure.map((action) => attachRoomIdToAction(action, devices)),
    },
  };
}

export function useAutomationRuleForm() {
  const [isModalOpen, setIsModalOpen] = useState(false);
  const [modalMode, setModalMode] = useState<AutomationModalMode>("create");
  const [editingRuleId, setEditingRuleId] = useState<string | null>(null);
  const [editingRuleDetail, setEditingRuleDetail] = useState<AutomationRuleDetailDto | null>(null);
  const [name, setName] = useState("");
  const [description, setDescription] = useState("");
  const [isEnabled, setIsEnabled] = useState(true);
  const [conditionLogic, setConditionLogic] =
    useState<AutomationConditionLogic>("All");
  const [cooldownMsText, setCooldownMsText] = useState(
    String(DEFAULT_COOLDOWN_MS)
  );
  const [conditions, setConditions] = useState<AutomationConditionDraft[]>([]);
  const [timeWindow, setTimeWindow] = useState<AutomationTimeWindowDraft>(() =>
    createDefaultAutomationTimeWindowDraft()
  );
  const [actionSet, setActionSet] = useState<ActionSetDraft>(() =>
    createEmptyActionSetDraft()
  );
  const [modalError, setModalError] = useState<string | null>(null);
  const [isSaving, setIsSaving] = useState(false);

  const resetModalState = useCallback(() => {
    setEditingRuleDetail(null);
    setName("");
    setDescription("");
    setIsEnabled(true);
    setConditionLogic("All");
    setCooldownMsText(String(DEFAULT_COOLDOWN_MS));
    setConditions([createEmptyAutomationConditionDraft()]);
    setTimeWindow(createDefaultAutomationTimeWindowDraft());
    setActionSet(createEmptyActionSetDraft());
    setModalError(null);
  }, []);

  const openCreateForm = useCallback(() => {
    setModalMode("create");
    setEditingRuleId(null);
    resetModalState();
    setIsModalOpen(true);
  }, [resetModalState]);

  const openEditForm = useCallback(
    (ruleDetail: AutomationRuleDetailDto, devices: BuilderDeviceDto[]) => {
      setModalMode("edit");
      setEditingRuleId(ruleDetail.id);
      setEditingRuleDetail(ruleDetail);
      setName(ruleDetail.name);
      setDescription(ruleDetail.description ?? "");
      setIsEnabled(ruleDetail.isEnabled);
      setConditionLogic(ruleDetail.conditionLogic);
      setCooldownMsText(String(ruleDetail.cooldownMs));
      setConditions(
        ruleDetail.conditions
          .slice()
          .sort((left, right) => left.order - right.order)
          .map((condition) =>
            conditionDetailToDraft(
              condition,
              condition.deviceId ? resolveDeviceRoomId(devices, condition.deviceId) : ""
            )
          )
      );
      setTimeWindow(timeWindowDetailToDraft(ruleDetail.timeWindow));
      setActionSet(attachRoomIdsToActionSetDraft(
        actionSetDtoToDraft(ruleDetail.actionSet),
        devices
      ));
      setModalError(null);
      setIsModalOpen(true);
    },
    []
  );

  const closeForm = useCallback(() => {
    setIsModalOpen(false);
    setEditingRuleId(null);
    setEditingRuleDetail(null);
    setModalError(null);
  }, []);

  const changeCondition = useCallback(
    (index: number, condition: AutomationConditionDraft) => {
      setConditions((current) =>
        current.map((item, currentIndex) =>
          currentIndex === index ? condition : item
        )
      );
    },
    []
  );

  const addCondition = useCallback(() => {
    setConditions((current) => [...current, createEmptyAutomationConditionDraft()]);
  }, []);

  const removeCondition = useCallback((index: number) => {
    setConditions((current) =>
      current.filter((_, currentIndex) => currentIndex !== index)
    );
  }, []);

  return {
    addCondition,
    changeCondition,
    closeForm,
    conditionLogic,
    conditions,
    timeWindow,
    cooldownMsText,
    description,
    editingRuleId,
    editingRuleDetail,
    isEnabled,
    isModalOpen,
    isSaving,
    modalError,
    modalMode,
    name,
    openCreateForm,
    openEditForm,
    removeCondition,
    setConditionLogic,
    setCooldownMsText,
    setDescription,
    setEditingRuleId,
    setIsEnabled,
    setIsModalOpen,
    setIsSaving,
    setModalError,
    setName,
    actionSet,
    setTimeWindow,
    setActionSet,
  };
}
