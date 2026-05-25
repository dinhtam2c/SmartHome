import { useCallback, useEffect, useMemo, useState } from "react";
import type { SyntheticEvent } from "react";
import { useTranslation } from "react-i18next";
import type { RealtimeDeltaEvent } from "@/shared/api/sse";
import { subscribeToRealtimeDeltas } from "@/shared/api/sse";
import { useToast } from "@/shared/ui/Toast";
import { useCapabilityRegistry } from "@/features/capabilities";
import type { SelectableDeviceDto } from "@/features/capabilities";
import { getHomeDevices } from "@/features/homes";
import type { HomeRoomOverviewDto } from "@/features/homes";
import {
  createAutomationRule,
  deleteAutomationRule,
  executeAutomationRule,
  getAutomationRuleDetail,
  getAutomationRules,
  updateAutomationRule,
} from "../api/automationsApi";
import type {
  AutomationRuleListItemDto,
} from "../types/automationTypes";
import {
  buildAutomationConditionRequest,
  buildAutomationTimeWindowRequest,
} from "../services/automationFormService";
import { findConditionField } from "../services/automationSelectionService";
import { useAutomationRuleForm } from "./useAutomationRuleForm";
import {
  buildActionSetRequest,
  getActionReadOnlyPaths,
} from "@/features/action-sets";

type UseHomeAutomationsParams = {
  homeId: string;
  rooms: HomeRoomOverviewDto[];
};

function isAutomationRuleListItem(value: unknown): value is AutomationRuleListItemDto {
  return (
    typeof value === "object" &&
    value !== null &&
    typeof (value as AutomationRuleListItemDto).id === "string" &&
    typeof (value as AutomationRuleListItemDto).homeId === "string" &&
    typeof (value as AutomationRuleListItemDto).name === "string"
  );
}

function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === "object" && value !== null && !Array.isArray(value);
}

function applyAutomationDelta(
  current: AutomationRuleListItemDto[],
  event: RealtimeDeltaEvent
) {
  if (event.entity === "AutomationRule" && event.change === "Deleted") {
    return current.filter((rule) => rule.id !== event.ruleId);
  }

  const ruleDelta = event.delta;
  if (event.entity === "AutomationRule" && isAutomationRuleListItem(ruleDelta)) {
    const exists = current.some((rule) => rule.id === ruleDelta.id);
    return exists
      ? current.map((rule) => rule.id === ruleDelta.id ? ruleDelta : rule)
      : [...current, ruleDelta];
  }

  return current;
}

export function useHomeAutomations({
  homeId,
  rooms,
}: UseHomeAutomationsParams) {
  const { t } = useTranslation("automations");
  const { pushToast } = useToast();
  const capabilityRegistry = useCapabilityRegistry();
  const form = useAutomationRuleForm();

  const [rules, setRules] = useState<AutomationRuleListItemDto[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [hasLoadedRules, setHasLoadedRules] = useState(false);
  const [loadError, setLoadError] = useState<string | null>(null);
  const [availableDevices, setAvailableDevices] = useState<SelectableDeviceDto[]>([]);
  const [availableDevicesByRoom, setAvailableDevicesByRoom] =
    useState<Record<string, SelectableDeviceDto[]>>({});
  const [deletingRuleId, setDeletingRuleId] = useState<string | null>(null);
  const [executingRuleId, setExecutingRuleId] = useState<string | null>(null);

  const sortedRules = useMemo(
    () => [...rules].sort((left, right) => right.updatedAt - left.updatedAt),
    [rules]
  );

  const showErrorToast = useCallback(
    (messageKey: string) => {
      pushToast({
        tone: "error",
        message: t(messageKey, { defaultValue: messageKey }),
      });
    },
    [pushToast, t]
  );

  const loadAvailableDevices = useCallback(async () => {
    const roomIds = rooms.map((room) => room.id);
    const [devices, roomEntries] = await Promise.all([
      getHomeDevices(homeId),
      Promise.all(
        roomIds.map(async (roomId) => {
          const roomDevices = await getHomeDevices(homeId, roomId);
          return [roomId, roomDevices] as const;
        })
      ),
    ]);

    setAvailableDevices(devices);
    setAvailableDevicesByRoom(Object.fromEntries(roomEntries));
    return devices;
  }, [homeId, rooms]);

  const loadRules = useCallback(async (silent = false) => {
    if (!silent) {
      setIsLoading(true);
    }
    setLoadError(null);

    try {
      const [ruleItems] = await Promise.all([
        getAutomationRules(homeId),
        loadAvailableDevices(),
      ]);
      setRules(ruleItems);
    } catch (error) {
      setLoadError((error as Error).message || "automations.errors.loadListFailed");
    } finally {
      setHasLoadedRules(true);
      if (!silent) {
        setIsLoading(false);
      }
    }
  }, [homeId, loadAvailableDevices]);

  useEffect(() => {
    void loadRules();
  }, [loadRules]);

  useEffect(() => {
    const cleanup = subscribeToRealtimeDeltas({
      path: `/homes/${homeId}/events`,
      onDelta: (event) => {
        if (event.homeId && event.homeId !== homeId) {
          return;
        }

        if (event.entity === "AutomationRule") {
          setRules((current) => applyAutomationDelta(current, event));
          return;
        }

        if (
          event.entity === "AutomationExecution" &&
          event.ruleId &&
          isRecord(event.delta) &&
          event.delta.status !== "Running"
        ) {
          setExecutingRuleId((current) =>
            current === event.ruleId ? null : current
          );
        }
      },
      onReconnect: () => {
        void loadRules(true);
      },
    });

    return () => {
      cleanup();
    };
  }, [homeId, loadRules]);

  const openCreateModal = useCallback(async () => {
    form.openCreateForm();

    try {
      await loadAvailableDevices();
    } catch {
      form.setModalError("automations.errors.loadDevicesFailed");
    }
  }, [form, loadAvailableDevices]);

  const openEditModal = useCallback(
    async (ruleId: string) => {
      form.setModalError(null);

      try {
        const [ruleDetail, devices] = await Promise.all([
          getAutomationRuleDetail(homeId, ruleId),
          loadAvailableDevices(),
        ]);

        form.openEditForm(ruleDetail, devices);
      } catch (error) {
        showErrorToast((error as Error).message || "automations.errors.loadDetailFailed");
      }
    },
    [form, homeId, loadAvailableDevices, showErrorToast]
  );

  const handleSave = useCallback(
    async (event: SyntheticEvent<HTMLFormElement>) => {
      event.preventDefault();

      if (!form.name.trim()) {
        form.setModalError("automations.errors.nameRequired");
        return;
      }

      if (form.conditions.length === 0) {
        form.setModalError("automations.errors.atLeastOneConditionRequired");
        return;
      }

      const cooldownMs = Number(form.cooldownMsText.trim() || "0");
      if (!Number.isInteger(cooldownMs) || cooldownMs < 0) {
        form.setModalError("automations.errors.cooldownInvalid");
        return;
      }

      const conditionRequests = [];
      for (let index = 0; index < form.conditions.length; index += 1) {
        const parsed = buildAutomationConditionRequest(
          form.conditions[index],
          findConditionField(
            availableDevices,
            capabilityRegistry.registryMap,
            form.conditions[index]
          )
        );

        if (!parsed.value || parsed.errorKey) {
          form.setModalError(parsed.errorKey ?? "automations.errors.invalidCondition");
          return;
        }

        conditionRequests.push(parsed.value);
      }

      const parsedTimeWindow = buildAutomationTimeWindowRequest(form.timeWindow);
      if (!parsedTimeWindow.value) {
        form.setModalError(parsedTimeWindow.errorKey ?? "automations.errors.invalidTimeWindow");
        return;
      }

      const parsedActionSet = buildActionSetRequest(form.actionSet, {
        getReadOnlyPaths: (action) =>
          getActionReadOnlyPaths(
            action,
            availableDevices,
            capabilityRegistry.registryMap
          ),
      });

      if (!parsedActionSet.value) {
        form.setModalError(parsedActionSet.errorKey ?? "automations.errors.invalidActionSet");
        return;
      }

      form.setIsSaving(true);
      form.setModalError(null);

      try {
        const payload = {
          name: form.name.trim(),
          description: form.description.trim() || null,
          isEnabled: form.isEnabled,
          conditionLogic: form.conditionLogic,
          cooldownMs,
          conditions: conditionRequests,
          timeWindow: parsedTimeWindow.value,
          actionSet: parsedActionSet.value,
        };

        if (form.modalMode === "create") {
          await createAutomationRule(homeId, payload);
        } else {
          if (!form.editingRuleId) {
            form.setModalError("automations.errors.notFound");
            return;
          }

          await updateAutomationRule(homeId, form.editingRuleId, payload);
        }

        form.setIsModalOpen(false);
        await loadRules();
      } catch (error) {
        form.setModalError(
          (error as Error).message ||
          (form.modalMode === "create"
            ? "automations.errors.createFailed"
            : "automations.errors.updateFailed")
        );
      } finally {
        form.setIsSaving(false);
      }
    },
    [availableDevices, capabilityRegistry.registryMap, form, homeId, loadRules]
  );

  const handleDelete = useCallback(
    async (ruleId: string) => {
      if (!window.confirm(t("automations.deleteConfirm"))) {
        return;
      }

      setDeletingRuleId(ruleId);
      try {
        await deleteAutomationRule(homeId, ruleId);
        form.setIsModalOpen(false);
        form.setEditingRuleId(null);
        await loadRules();
      } catch (error) {
        showErrorToast((error as Error).message || "automations.errors.deleteFailed");
      } finally {
        setDeletingRuleId(null);
      }
    },
    [form, homeId, loadRules, showErrorToast, t]
  );

  const handleExecute = useCallback(
    async (ruleId: string, isRuleEnabled: boolean) => {
      if (!isRuleEnabled) {
        return;
      }

      setExecutingRuleId(ruleId);
      try {
        await executeAutomationRule(homeId, ruleId);
        await loadRules();
      } catch (error) {
        showErrorToast((error as Error).message || "automations.errors.executeFailed");
      } finally {
        setExecutingRuleId(null);
      }
    },
    [homeId, loadRules, showErrorToast]
  );

  return {
    availableDevices,
    availableDevicesByRoom,
    capabilityRegistry,
    deletingRuleId,
    executingRuleId,
    form,
    handleDelete,
    handleExecute,
    handleSave,
    hasLoadedRules,
    isLoading,
    loadError,
    openCreateModal,
    openEditModal,
    sortedRules,
  };
}
