import { useCallback, useEffect, useMemo, useRef, useState } from "react";
import type { SyntheticEvent } from "react";
import type { TFunction } from "i18next";
import {
  CAPABILITY_LABEL_KEYS,
  composeValueByPath,
  DEVICE_LEVEL_ENDPOINT_KEY,
  formatRgbValue,
  getCapabilityBooleanLabel,
  getCapabilityBooleanLabels,
  getCapabilityRgbLabels,
  getRgbValue,
  isRgbCapabilityId,
  localizeCapabilityStatePath,
  toEndpointKey,
} from "@/features/capabilities";
import { getHomeDetail } from "@/features/homes";
import type { HomeRoomOverviewDto } from "@/features/homes";
import {
  assignDeviceRoom,
  deleteDevice,
  sendDeviceCommand,
  updateDevice,
} from "../api/devicesApi";
import type { DeviceCapabilityDto } from "../types/deviceTypes";
import {
  type OperationRule,
  buildCommandValue,
  formatCapabilityState,
} from "../services/deviceCapabilityService";
import { useDeviceDetail } from "./useDeviceDetail";

type CapabilityGroup = {
  endpointKey: string;
  endpointLabel: string;
  capabilities: DeviceCapabilityDto[];
};

type UseDeviceDetailPageParams = {
  deviceId: string | null;
  homeId: string | null;
  routeRoomId: string | null;
  navigateTo: (to: string) => void;
  t: TFunction<"devices">;
};

function getLocalizedBooleanLabel(
  value: boolean,
  t: TFunction<"devices">,
  capabilityId?: string | null
) {
  return getCapabilityBooleanLabel(
    capabilityId,
    value,
    getCapabilityBooleanLabels(t, CAPABILITY_LABEL_KEYS.device)
  );
}

function replaceBooleanValues(
  value: unknown,
  t: TFunction<"devices">,
  capabilityId?: string | null
): unknown {
  if (typeof value === "boolean") {
    return getLocalizedBooleanLabel(value, t, capabilityId);
  }

  if (Array.isArray(value)) {
    return value.map((item) => replaceBooleanValues(item, t, capabilityId));
  }

  if (value && typeof value === "object") {
    return Object.fromEntries(
      Object.entries(value as Record<string, unknown>).map(
        ([key, nestedValue]) => [
          key,
          replaceBooleanValues(nestedValue, t, capabilityId),
        ]
      )
    );
  }

  return value;
}

function localizeObjectKeys(
  value: unknown,
  localizeKey: (key: string) => string
): unknown {
  if (getRgbValue(value)) {
    return value;
  }

  if (Array.isArray(value)) {
    return value.map((item) => localizeObjectKeys(item, localizeKey));
  }

  if (value && typeof value === "object") {
    return Object.fromEntries(
      Object.entries(value as Record<string, unknown>).map(
        ([key, nestedValue]) => [
          localizeKey(key),
          localizeObjectKeys(nestedValue, localizeKey),
        ]
      )
    );
  }

  return value;
}

export function useDeviceDetailPage({
  deviceId,
  homeId,
  routeRoomId,
  navigateTo,
  t,
}: UseDeviceDetailPageParams) {
  const [isEditNameOpen, setIsEditNameOpen] = useState(false);
  const [isAssignRoomOpen, setIsAssignRoomOpen] = useState(false);
  const [isDeleteBusy, setIsDeleteBusy] = useState(false);
  const [isSaving, setIsSaving] = useState(false);
  const [actionError, setActionError] = useState<string | null>(null);
  const [quickActionError, setQuickActionError] = useState<string | null>(null);
  const [quickToggleBusyCapabilityId, setQuickToggleBusyCapabilityId] =
    useState<string | null>(null);
  const [optimisticToggleValues, setOptimisticToggleValues] = useState<
    Record<string, boolean | undefined>
  >({});
  const [inlineCommandValues, setInlineCommandValues] = useState<
    Record<string, string>
  >({});
  const [editName, setEditName] = useState("");
  const [homeRooms, setHomeRooms] = useState<HomeRoomOverviewDto[]>([]);
  const [roomId, setRoomId] = useState(routeRoomId ?? "");

  const inlineCommandSendTimersRef = useRef<
    Record<string, ReturnType<typeof setTimeout> | undefined>
  >({});

  const { device, isLoading, error, reload } = useDeviceDetail(deviceId);

  const backPath =
    homeId && routeRoomId ? `/homes/${homeId}/rooms/${routeRoomId}` : "/homes";

  const endpointNameByKey = useMemo(() => {
    const grouped = new Map<string, string>();

    (device?.endpoints ?? []).forEach((endpoint) => {
      const endpointName = endpoint.name?.trim();

      if (endpointName) {
        grouped.set(toEndpointKey(endpoint.endpointId), endpointName);
      }
    });

    return grouped;
  }, [device?.endpoints]);

  const getEndpointLabel = useCallback(
    (endpointKey: string) => {
      if (endpointKey === DEVICE_LEVEL_ENDPOINT_KEY) {
        return t("deviceLevel");
      }

      return endpointNameByKey.get(endpointKey) ?? endpointKey;
    },
    [endpointNameByKey, t]
  );

  const capabilityOptions = useMemo(() => device?.capabilities ?? [], [device]);

  const capabilityGroups = useMemo<CapabilityGroup[]>(() => {
    const grouped = new Map<string, DeviceCapabilityDto[]>();

    capabilityOptions.forEach((capability) => {
      const key = toEndpointKey(capability.endpointId);
      const current = grouped.get(key) ?? [];
      current.push(capability);
      grouped.set(key, current);
    });

    const deviceLevelCapabilities = grouped.get(DEVICE_LEVEL_ENDPOINT_KEY);
    const deviceLevelGroup = deviceLevelCapabilities
      ? [
          {
            endpointKey: DEVICE_LEVEL_ENDPOINT_KEY,
            endpointLabel: getEndpointLabel(DEVICE_LEVEL_ENDPOINT_KEY),
            capabilities: deviceLevelCapabilities,
          },
        ]
      : [];

    const endpointGroups = Array.from(grouped.entries())
      .filter(([endpointKey]) => endpointKey !== DEVICE_LEVEL_ENDPOINT_KEY)
      .sort(([left], [right]) => left.localeCompare(right))
      .map(([endpointKey, capabilities]) => ({
        endpointKey,
        endpointLabel: getEndpointLabel(endpointKey),
        capabilities,
      }));

    return [...deviceLevelGroup, ...endpointGroups];
  }, [capabilityOptions, getEndpointLabel]);

  const getLocalizedStateKeyLabel = useCallback(
    (key: string) =>
      localizeCapabilityStatePath(t, key, CAPABILITY_LABEL_KEYS.device),
    [t]
  );

  const localizeStateShape = useCallback(
    (value: unknown) => localizeObjectKeys(value, getLocalizedStateKeyLabel),
    [getLocalizedStateKeyLabel]
  );

  const canControlDevice = device?.isOnline;

  const handleInlineCommandSend = useCallback(
    async (
      capabilityId: string,
      capabilityKey: string,
      endpointId: string,
      operation: string,
      rule: OperationRule | null,
      rawValue: string,
      valuePath: string | null = null,
      valueOverride?: unknown
    ) => {
      if (!deviceId) return;

      const value =
        valueOverride !== undefined
          ? valueOverride
          : buildCommandValue(rule, rawValue);

      if (valueOverride === undefined && value === null) {
        setQuickActionError("errors.invalidCommandValue");
        return;
      }

      setQuickActionError(null);
      setQuickToggleBusyCapabilityId(capabilityKey);
      try {
        await sendDeviceCommand(deviceId, {
          capabilityId,
          endpointId,
          operation,
          value: composeValueByPath(valuePath, value),
        });
      } catch (submitError) {
        setQuickActionError(
          (submitError as Error).message || "errors.sendCommandFailed"
        );
      } finally {
        setQuickToggleBusyCapabilityId((current) =>
          current === capabilityKey ? null : current
        );
      }
    },
    [deviceId]
  );

  const handleLiveInlineCommandSend = useCallback(
    async (
      capabilityId: string,
      capabilityKey: string,
      endpointId: string,
      operation: string,
      rule: OperationRule | null,
      rawValue: string,
      valuePath: string | null = null,
      valueOverride?: unknown
    ) => {
      if (!deviceId) return;

      const value =
        valueOverride !== undefined
          ? valueOverride
          : buildCommandValue(rule, rawValue);

      if (valueOverride === undefined && value === null) {
        setQuickActionError("errors.invalidCommandValue");
        return;
      }

      const pendingTimer = inlineCommandSendTimersRef.current[capabilityKey];
      if (pendingTimer) {
        clearTimeout(pendingTimer);
        inlineCommandSendTimersRef.current[capabilityKey] = undefined;
      }

      setQuickActionError(null);

      try {
        await sendDeviceCommand(deviceId, {
          capabilityId,
          endpointId,
          operation,
          value: composeValueByPath(valuePath, value),
        });
      } catch (submitError) {
        setQuickActionError(
          (submitError as Error).message || "errors.sendCommandFailed"
        );
      }
    },
    [deviceId]
  );

  const handleBooleanToggleSend = useCallback(
    async (
      capabilityId: string,
      capabilityKey: string,
      endpointId: string,
      operation: string,
      nextValue: boolean,
      previousValue: boolean | null,
      valuePath: string | null = null
    ) => {
      if (!deviceId) return;

      setQuickActionError(null);
      setQuickToggleBusyCapabilityId(capabilityKey);
      setOptimisticToggleValues((current) => ({
        ...current,
        [capabilityKey]: nextValue,
      }));

      try {
        await sendDeviceCommand(deviceId, {
          capabilityId,
          endpointId,
          operation,
          value: composeValueByPath(valuePath, nextValue),
        });
      } catch (submitError) {
        setOptimisticToggleValues((current) => ({
          ...current,
          [capabilityKey]: previousValue ?? undefined,
        }));
        setQuickActionError(
          (submitError as Error).message || "errors.sendCommandFailed"
        );
      } finally {
        setQuickToggleBusyCapabilityId((current) =>
          current === capabilityKey ? null : current
        );
      }
    },
    [deviceId]
  );

  const clearInlineCommandSendTimer = useCallback((capabilityKey: string) => {
    const timer = inlineCommandSendTimersRef.current[capabilityKey];
    if (timer) {
      clearTimeout(timer);
      inlineCommandSendTimersRef.current[capabilityKey] = undefined;
    }
  }, []);

  const scheduleInlineCommandSend = useCallback(
    (
      capabilityId: string,
      capabilityKey: string,
      endpointId: string,
      operation: string,
      rule: OperationRule | null,
      rawValue: string,
      valuePath: string | null = null,
      delayMs = 1200
    ) => {
      clearInlineCommandSendTimer(capabilityKey);

      inlineCommandSendTimersRef.current[capabilityKey] = setTimeout(() => {
        inlineCommandSendTimersRef.current[capabilityKey] = undefined;
        void handleInlineCommandSend(
          capabilityId,
          capabilityKey,
          endpointId,
          operation,
          rule,
          rawValue,
          valuePath
        );
      }, delayMs);
    },
    [clearInlineCommandSendTimer, handleInlineCommandSend]
  );

  const formatCapabilityStateWithI18n = useCallback(
    (state: unknown, capabilityId?: string | null) => {
      const rgbText = isRgbCapabilityId(capabilityId)
        ? formatRgbValue(
            state,
            getCapabilityRgbLabels(t, CAPABILITY_LABEL_KEYS.device)
          )
        : null;

      if (rgbText) {
        return rgbText;
      }

      if (state === null || state === undefined) {
        return t("notAvailable");
      }

      const localizedState = localizeStateShape(
        replaceBooleanValues(state, t, capabilityId)
      );

      if (
        typeof localizedState === "string" ||
        typeof localizedState === "number" ||
        typeof localizedState === "boolean"
      ) {
        return formatCapabilityState(localizedState);
      }

      return formatCapabilityState(localizedState);
    },
    [localizeStateShape, t]
  );

  const openEditNameModal = useCallback(() => {
    if (!device) return;

    setActionError(null);
    setEditName(device.name);
    setIsEditNameOpen(true);
  }, [device]);

  const closeEditNameModal = useCallback(() => {
    setIsEditNameOpen(false);
  }, []);

  const openAssignRoomModal = useCallback(() => {
    if (!device) return;

    setActionError(null);
    setRoomId(device.roomId ?? "");
    setIsAssignRoomOpen(true);
  }, [device]);

  const closeAssignRoomModal = useCallback(() => {
    setIsAssignRoomOpen(false);
  }, []);

  const handleSaveName = useCallback(
    async (event: SyntheticEvent<HTMLFormElement>) => {
      event.preventDefault();
      if (!deviceId || !editName.trim()) return;

      setIsSaving(true);
      setActionError(null);

      try {
        await updateDevice(deviceId, { name: editName.trim() });
        await reload(true);
        setIsEditNameOpen(false);
      } catch (saveError) {
        setActionError(
          (saveError as Error).message || "errors.updateDeviceNameFailed"
        );
      } finally {
        setIsSaving(false);
      }
    },
    [deviceId, editName, reload]
  );

  const handleAssignRoom = useCallback(
    async (event: SyntheticEvent<HTMLFormElement>) => {
      event.preventDefault();
      if (!deviceId || !roomId) return;

      setIsSaving(true);
      setActionError(null);

      try {
        await assignDeviceRoom(deviceId, { roomId });
        await reload(true);
        setIsAssignRoomOpen(false);
      } catch (assignError) {
        setActionError(
          (assignError as Error).message || "errors.assignRoomFailed"
        );
      } finally {
        setIsSaving(false);
      }
    },
    [deviceId, roomId, reload]
  );

  const handleDeleteDevice = useCallback(async () => {
    if (!deviceId) return;
    if (!window.confirm(t("deleteConfirm"))) return;

    setIsDeleteBusy(true);
    try {
      await deleteDevice(deviceId);
      navigateTo(backPath);
    } finally {
      setIsDeleteBusy(false);
    }
  }, [backPath, deviceId, navigateTo, t]);

  useEffect(() => {
    async function loadHomeRooms() {
      if (!device?.homeId) {
        setHomeRooms([]);
        return;
      }

      try {
        const home = await getHomeDetail(device.homeId);
        setHomeRooms(home.rooms);
      } catch {
        setHomeRooms([]);
      }
    }

    void loadHomeRooms();
  }, [device?.homeId]);

  useEffect(() => {
    setRoomId(routeRoomId ?? device?.roomId ?? "");
  }, [device?.roomId, routeRoomId]);

  useEffect(() => {
    const timerRef = inlineCommandSendTimersRef;

    return () => {
      Object.values(timerRef.current).forEach((timer) => {
        if (timer) {
          clearTimeout(timer);
        }
      });
    };
  }, []);

  return {
    actionError,
    backPath,
    canControlDevice: Boolean(canControlDevice),
    capabilityGroups,
    closeAssignRoomModal,
    closeEditNameModal,
    device,
    editName,
    error,
    formatCapabilityState: formatCapabilityStateWithI18n,
    handleAssignRoom,
    handleBooleanToggleSend,
    handleDeleteDevice,
    handleInlineCommandSend,
    handleLiveInlineCommandSend,
    handleSaveName,
    homeRooms,
    inlineCommandValues,
    isAssignRoomOpen,
    isDeleteBusy,
    isEditNameOpen,
    isLoading,
    isSaving,
    roomId,
    openAssignRoomModal,
    openEditNameModal,
    optimisticToggleValues,
    quickActionError,
    quickToggleBusyCapabilityId,
    scheduleInlineCommandSend,
    setEditName,
    setInlineCommandValues,
    setRoomId,
  };
}
