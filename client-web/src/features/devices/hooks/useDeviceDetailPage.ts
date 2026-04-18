import { useCallback, useEffect, useMemo, useRef, useState } from "react";
import type { SyntheticEvent } from "react";
import type { TFunction } from "i18next";
import {
  composeValueByPath,
  DEVICE_LEVEL_ENDPOINT_KEY,
  toEndpointKey,
} from "@/features/capabilities";
import { getHomeDetail } from "@/features/homes/homes.api";
import type { HomeRoomOverviewDto } from "@/features/homes/homes.types";
import {
  assignDeviceRoom,
  deleteDevice,
  getDeviceCapabilityHistory,
  getDeviceCommands,
  sendDeviceCommand,
  updateDevice,
} from "../devices.api";
import {
  type DeviceCapabilityDto,
  type DeviceCapabilityHistoryPointDto,
  type DeviceCommandExecutionDto,
  type DeviceCommandStatus,
} from "../devices.types";
import { type OperationRule, buildCommandValue, formatCapabilityState } from "../deviceCapabilityUtils";
import { useDeviceDetail } from "./useDeviceDetail";

const COLLAPSED_ITEM_COUNT = 4;

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

function toEpochSecondsFromLocalDateTime(input: string) {
  if (!input) return undefined;
  const epochMs = Date.parse(input);
  return Number.isFinite(epochMs) ? Math.floor(epochMs / 1000) : undefined;
}

function toEpochMs(timestamp: number) {
  return timestamp < 1e12 ? timestamp * 1000 : timestamp;
}

function getCommandStatusTone(status: DeviceCommandStatus) {
  if (status === "Pending" || status === "Accepted") return "pending" as const;
  if (status === "Completed") return "completed" as const;
  if (status === "Failed" || status === "TimedOut") return "failed" as const;
  return "neutral" as const;
}

function getLocalizedBooleanLabel(value: boolean, t: TFunction<"devices">) {
  return value ? t("on") : t("off");
}

function replaceBooleanValues(
  value: unknown,
  t: TFunction<"devices">
): unknown {
  if (typeof value === "boolean") {
    return getLocalizedBooleanLabel(value, t);
  }

  if (Array.isArray(value)) {
    return value.map((item) => replaceBooleanValues(item, t));
  }

  if (value && typeof value === "object") {
    return Object.fromEntries(
      Object.entries(value as Record<string, unknown>).map(([key, nestedValue]) => [
        key,
        replaceBooleanValues(nestedValue, t),
      ])
    );
  }

  return value;
}

function localizeObjectKeys(
  value: unknown,
  localizeKey: (key: string) => string
): unknown {
  if (Array.isArray(value)) {
    return value.map((item) => localizeObjectKeys(item, localizeKey));
  }

  if (value && typeof value === "object") {
    return Object.fromEntries(
      Object.entries(value as Record<string, unknown>).map(([key, nestedValue]) => [
        localizeKey(key),
        localizeObjectKeys(nestedValue, localizeKey),
      ])
    );
  }

  return value;
}

function getCommandStatusLabel(status: DeviceCommandStatus, t: TFunction<"devices">) {
  return t(`commandStatuses.${status}`, { defaultValue: status });
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

  const [commandHistory, setCommandHistory] = useState<DeviceCommandExecutionDto[]>([]);
  const [commandError, setCommandError] = useState<string | null>(null);
  const [commandPage, setCommandPage] = useState(1);
  const [commandPageSize, setCommandPageSize] = useState(20);
  const [commandTotalCount, setCommandTotalCount] = useState(0);
  const [commandFilterStatus, setCommandFilterStatus] =
    useState<"all" | DeviceCommandStatus>("all");
  const [commandFilterEndpoint, setCommandFilterEndpoint] = useState("all");
  const [commandFilterCapability, setCommandFilterCapability] = useState("all");
  const [commandFrom, setCommandFrom] = useState("");
  const [commandTo, setCommandTo] = useState("");
  const [commandLimit, setCommandLimit] = useState(20);
  const [showAllCommands, setShowAllCommands] = useState(false);
  const [isCommandHistoryVisible, setIsCommandHistoryVisible] = useState(false);

  const [historyFilterEndpoint, setHistoryFilterEndpoint] = useState("all");
  const [selectedCapabilityForHistory, setSelectedCapabilityForHistory] =
    useState("all");
  const [historyFrom, setHistoryFrom] = useState("");
  const [historyTo, setHistoryTo] = useState("");
  const [capabilityHistory, setCapabilityHistory] = useState<
    DeviceCapabilityHistoryPointDto[]
  >([]);
  const [capabilityHistoryError, setCapabilityHistoryError] = useState<
    string | null
  >(null);
  const [capabilityHistoryPage, setCapabilityHistoryPage] = useState(1);
  const [capabilityHistoryPageSize, setCapabilityHistoryPageSize] = useState(50);
  const [capabilityHistoryTotalCount, setCapabilityHistoryTotalCount] = useState(0);
  const [isCapabilityHistoryLoading, setIsCapabilityHistoryLoading] =
    useState(false);
  const [showAllCapabilityHistory, setShowAllCapabilityHistory] = useState(false);
  const [isCapabilityHistoryVisible, setIsCapabilityHistoryVisible] =
    useState(false);

  const inlineCommandSendTimersRef = useRef<
    Record<string, ReturnType<typeof setTimeout> | undefined>
  >({});

  const { device, isLoading, error, reload } = useDeviceDetail(deviceId);

  const backPath =
    homeId && routeRoomId
      ? `/homes/${homeId}/rooms/${routeRoomId}`
      : "/homes";

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

  const capabilityIdFilterOptions = useMemo(
    () =>
      Array.from(
        new Set(capabilityOptions.map((capability) => capability.capabilityId))
      ),
    [capabilityOptions]
  );

  const capabilityEndpointFilterOptions = useMemo(() => {
    const uniqueKeys = Array.from(
      new Set(
        capabilityOptions.map((capability) => toEndpointKey(capability.endpointId))
      )
    );

    return uniqueKeys
      .sort((left, right) => {
        if (left === DEVICE_LEVEL_ENDPOINT_KEY) return -1;
        if (right === DEVICE_LEVEL_ENDPOINT_KEY) return 1;
        return left.localeCompare(right);
      })
      .map((key) => ({ value: key, label: getEndpointLabel(key) }));
  }, [capabilityOptions, getEndpointLabel]);

  const selectedCapabilityForHistoryDetail = useMemo(
    () =>
      capabilityOptions.find(
        (capability) => capability.id === selectedCapabilityForHistory
      ) ?? null,
    [capabilityOptions, selectedCapabilityForHistory]
  );

  const historyCapabilityOptions = useMemo(
    () =>
      capabilityOptions.filter((capability) => {
        const endpointKey = toEndpointKey(capability.endpointId);
        return (
          historyFilterEndpoint === "all" || endpointKey === historyFilterEndpoint
        );
      }),
    [capabilityOptions, historyFilterEndpoint]
  );

  const getCommandEndpointKey = useCallback(
    (command: DeviceCommandExecutionDto) => {
      if (typeof command.endpointId === "string" && command.endpointId.trim() !== "") {
        return toEndpointKey(command.endpointId);
      }

      return null;
    },
    []
  );

  const getCommandEndpointLabel = useCallback(
    (command: DeviceCommandExecutionDto) => {
      const endpointKey = getCommandEndpointKey(command);
      return endpointKey ? getEndpointLabel(endpointKey) : t("unknown");
    },
    [getCommandEndpointKey, getEndpointLabel, t]
  );

  const getCommandStatusLabelLocalized = useCallback(
    (status: DeviceCommandStatus) => getCommandStatusLabel(status, t),
    [t]
  );

  const filteredCommandHistory = useMemo(
    () =>
      commandHistory.filter((command) => {
        const matchesStatus =
          commandFilterStatus === "all" || command.status === commandFilterStatus;
        const matchesEndpoint =
          commandFilterEndpoint === "all" ||
          getCommandEndpointKey(command) === commandFilterEndpoint;
        const matchesCapability =
          commandFilterCapability === "all" ||
          command.capabilityId === commandFilterCapability;
        const requestedAt = toEpochMs(command.requestedAt);
        const fromMs = commandFrom ? Date.parse(commandFrom) : NaN;
        const toMs = commandTo ? Date.parse(commandTo) : NaN;
        const matchesFrom = Number.isFinite(fromMs) ? requestedAt >= fromMs : true;
        const matchesTo = Number.isFinite(toMs) ? requestedAt <= toMs : true;

        return (
          matchesStatus &&
          matchesEndpoint &&
          matchesCapability &&
          matchesFrom &&
          matchesTo
        );
      }),
    [
      commandFilterCapability,
      commandFilterEndpoint,
      commandFilterStatus,
      commandFrom,
      commandHistory,
      commandTo,
      getCommandEndpointKey,
    ]
  );

  const visibleCommandHistory = useMemo(
    () =>
      showAllCommands
        ? filteredCommandHistory
        : filteredCommandHistory.slice(0, COLLAPSED_ITEM_COUNT),
    [filteredCommandHistory, showAllCommands]
  );

  const visibleCapabilityHistory = useMemo(
    () =>
      showAllCapabilityHistory
        ? capabilityHistory
        : capabilityHistory.slice(0, COLLAPSED_ITEM_COUNT),
    [capabilityHistory, showAllCapabilityHistory]
  );

  const getLocalizedStateKeyLabel = useCallback(
    (key: string) => {
      const normalized = key.trim();
      if (!normalized) {
        return key;
      }

      return t(`stateKeyLabels.${normalized}`, {
        defaultValue: normalized,
      });
    },
    [t]
  );

  const getLocalizedCommandKeyLabel = useCallback(
    (key: string) => {
      const normalized = key.trim();
      if (!normalized) {
        return key;
      }

      const translatedCommandKey = t(`commandKeyLabels.${normalized}`, {
        defaultValue: "",
      });

      if (
        typeof translatedCommandKey === "string" &&
        translatedCommandKey.trim() !== ""
      ) {
        return translatedCommandKey;
      }

      return getLocalizedStateKeyLabel(normalized);
    },
    [getLocalizedStateKeyLabel, t]
  );

  const localizeStateShape = useCallback(
    (value: unknown) => localizeObjectKeys(value, getLocalizedStateKeyLabel),
    [getLocalizedStateKeyLabel]
  );

  const localizeCommandShape = useCallback(
    (value: unknown) => localizeObjectKeys(value, getLocalizedCommandKeyLabel),
    [getLocalizedCommandKeyLabel]
  );

  const formatCompactValue = useCallback(
    (value: unknown) => {
      if (value === null || value === undefined) return t("nullValue");
      if (typeof value === "string") return value;
      if (typeof value === "number") {
        return String(value);
      }

      if (typeof value === "boolean") {
        return getLocalizedBooleanLabel(value, t);
      }

      if (Array.isArray(value)) return `[${t("itemsCount", { count: value.length })}]`;
      if (typeof value === "object") {
        return `{${t("fieldsCount", {
          count: Object.keys(value as Record<string, unknown>).length,
        })}}`;
      }
      return String(value);
    },
    [t]
  );

  const summarizePayload = useCallback(
    (payload: string | null) => {
      if (!payload || payload.trim() === "") {
        return t("notAvailable");
      }

      try {
        const parsed = JSON.parse(payload) as unknown;
        const parsedWithLocalizedBooleans = replaceBooleanValues(parsed, t);
        const localizedPayload = localizeCommandShape(parsedWithLocalizedBooleans);

        if (localizedPayload === null) {
          return t("nullValue");
        }

        if (
          typeof localizedPayload === "string" ||
          typeof localizedPayload === "number"
        ) {
          return String(localizedPayload);
        }

        if (typeof localizedPayload === "boolean") {
          return getLocalizedBooleanLabel(localizedPayload, t);
        }

        if (Array.isArray(localizedPayload)) {
          if (localizedPayload.length === 0) return t("arrayCount", { count: 0 });
          const preview = localizedPayload
            .slice(0, 3)
            .map((value) => formatCompactValue(value))
            .join(", ");
          return t("arrayPreview", {
            count: localizedPayload.length,
            preview: `${preview}${localizedPayload.length > 3 ? ", ..." : ""}`,
          });
        }

        if (typeof localizedPayload === "object") {
          const entries = Object.entries(localizedPayload as Record<string, unknown>);
          if (entries.length === 0) {
            return t("objectCount", { count: 0 });
          }

          const preview = entries
            .slice(0, 4)
            .map(([key, value]) => `${key}: ${formatCompactValue(value)}`)
            .join(" | ");
          return `${preview}${entries.length > 4
            ? ` | ${t("moreCount", { count: entries.length - 4 })}`
            : ""
            }`;
        }

        return payload;
      } catch {
        return payload.length > 140 ? `${payload.slice(0, 140)}...` : payload;
      }
    },
    [formatCompactValue, localizeCommandShape, t]
  );

  const canControlDevice = device?.isOnline;

  const deviceRealtimeMarker = useMemo(() => {
    if (!device) return "";

    const latestCapabilityReportAt = device.capabilities.reduce(
      (latest, capability) => Math.max(latest, capability.lastReportedAt ?? 0),
      0
    );

    return [
      device.lastSeenAt,
      latestCapabilityReportAt,
      device.roomId ?? "",
      device.isOnline ? "1" : "0",
    ].join("|");
  }, [device]);

  const hasRealtimeMarkerHydratedRef = useRef(false);
  const lastProcessedRealtimeMarkerRef = useRef("");

  const commandBlockReason =
    !device?.isOnline ? t("commandBlockedOffline") : null;

  const refreshCommandHistory = useCallback(
    async (limit: number) => {
      if (!deviceId) return;

      setCommandError(null);
      try {
        const fromTimestamp = toEpochSecondsFromLocalDateTime(commandFrom);
        const toTimestamp = toEpochSecondsFromLocalDateTime(commandTo);
        const endpointIdFilter =
          commandFilterEndpoint !== "all" &&
            commandFilterEndpoint !== DEVICE_LEVEL_ENDPOINT_KEY
            ? commandFilterEndpoint
            : undefined;

        const response = await getDeviceCommands(deviceId, {
          endpointId: endpointIdFilter,
          capabilityId:
            commandFilterCapability === "all"
              ? undefined
              : commandFilterCapability,
          status: commandFilterStatus === "all" ? undefined : commandFilterStatus,
          from: fromTimestamp,
          to: toTimestamp,
          page: 1,
          pageSize: limit,
        });

        const sortedItems = response.items
          .slice()
          .sort((left, right) => toEpochMs(right.requestedAt) - toEpochMs(left.requestedAt));

        setCommandHistory(sortedItems);
        setCommandPage(response.page);
        setCommandPageSize(response.pageSize);
        setCommandTotalCount(response.totalCount);
      } catch (historyError) {
        setCommandError(
          (historyError as Error).message || "errors.loadCommandHistoryFailed"
        );
      }
    },
    [
      commandFilterCapability,
      commandFilterEndpoint,
      commandFilterStatus,
      commandFrom,
      commandTo,
      deviceId,
    ]
  );

  const refreshCapabilityHistory = useCallback(async () => {
    if (!deviceId || selectedCapabilityForHistory === "all") {
      setCapabilityHistory([]);
      setCapabilityHistoryError(null);
      return;
    }

    setIsCapabilityHistoryLoading(true);
    setCapabilityHistoryError(null);

    try {
      const fromTimestamp = toEpochSecondsFromLocalDateTime(historyFrom);
      const toTimestamp = toEpochSecondsFromLocalDateTime(historyTo);

      if (!selectedCapabilityForHistoryDetail) {
        setCapabilityHistory([]);
        setCapabilityHistoryError(null);
        return;
      }

      const data = await getDeviceCapabilityHistory(deviceId, {
        endpointId: selectedCapabilityForHistoryDetail.endpointId,
        capabilityId: selectedCapabilityForHistoryDetail.capabilityId,
        from: fromTimestamp,
        to: toTimestamp,
        page: 1,
        pageSize: 50,
      });

      setCapabilityHistory(data.items);
      setCapabilityHistoryPage(data.page);
      setCapabilityHistoryPageSize(data.pageSize);
      setCapabilityHistoryTotalCount(data.totalCount);
    } catch (historyError) {
      setCapabilityHistoryError(
        (historyError as Error).message || "errors.loadCapabilityHistoryFailed"
      );
    } finally {
      setIsCapabilityHistoryLoading(false);
    }
  }, [
    deviceId,
    historyFrom,
    historyTo,
    selectedCapabilityForHistory,
    selectedCapabilityForHistoryDetail,
  ]);

  const handleCommandSent = useCallback(async () => {
    const tasks: Promise<unknown>[] = [];

    if (isCommandHistoryVisible) {
      tasks.push(refreshCommandHistory(commandLimit));
    }

    if (isCapabilityHistoryVisible) {
      tasks.push(refreshCapabilityHistory());
    }

    if (tasks.length > 0) {
      await Promise.all(tasks);
    }
  }, [
    commandLimit,
    isCapabilityHistoryVisible,
    isCommandHistoryVisible,
    refreshCapabilityHistory,
    refreshCommandHistory,
  ]);

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
        valueOverride !== undefined ? valueOverride : buildCommandValue(rule, rawValue);

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

        await handleCommandSent();
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
    [deviceId, handleCommandSent]
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
    (state: unknown) => {
      if (state === null || state === undefined) {
        return t("notAvailable");
      }

      const localizedState = localizeStateShape(replaceBooleanValues(state, t));

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

  const toggleCommandHistoryVisibility = useCallback(() => {
    setIsCommandHistoryVisible((current) => !current);
  }, []);

  const toggleCapabilityHistoryVisibility = useCallback(() => {
    setIsCapabilityHistoryVisible((current) => !current);
  }, []);

  const openCommandHistory = useCallback(() => {
    setIsCommandHistoryVisible(true);
  }, []);

  const closeCommandHistory = useCallback(() => {
    setIsCommandHistoryVisible(false);
  }, []);

  const openCapabilityHistory = useCallback(() => {
    setIsCapabilityHistoryVisible(true);
  }, []);

  const closeCapabilityHistory = useCallback(() => {
    setIsCapabilityHistoryVisible(false);
  }, []);

  useEffect(() => {
    if (!isCommandHistoryVisible) return;
    void refreshCommandHistory(commandLimit);
  }, [commandLimit, isCommandHistoryVisible, refreshCommandHistory]);

  useEffect(() => {
    setShowAllCommands(false);
  }, [
    commandFilterEndpoint,
    commandFilterStatus,
    commandFilterCapability,
    commandFrom,
    commandTo,
    commandLimit,
  ]);

  useEffect(() => {
    const nextCapability =
      historyCapabilityOptions.find(
        (capability) => capability.id === selectedCapabilityForHistory
      ) ??
      historyCapabilityOptions[0] ??
      null;

    setSelectedCapabilityForHistory(nextCapability?.id ?? "all");
  }, [historyCapabilityOptions, selectedCapabilityForHistory]);

  useEffect(() => {
    if (!isCapabilityHistoryVisible) return;
    void refreshCapabilityHistory();
  }, [isCapabilityHistoryVisible, refreshCapabilityHistory]);

  useEffect(() => {
    setShowAllCapabilityHistory(false);
  }, [historyFilterEndpoint, selectedCapabilityForHistory, historyFrom, historyTo]);

  useEffect(() => {
    if (!deviceRealtimeMarker) return;

    if (!hasRealtimeMarkerHydratedRef.current) {
      hasRealtimeMarkerHydratedRef.current = true;
      lastProcessedRealtimeMarkerRef.current = deviceRealtimeMarker;
      return;
    }

    if (lastProcessedRealtimeMarkerRef.current === deviceRealtimeMarker) {
      return;
    }

    lastProcessedRealtimeMarkerRef.current = deviceRealtimeMarker;

    if (isCommandHistoryVisible) {
      void refreshCommandHistory(commandLimit);
    }

    if (isCapabilityHistoryVisible) {
      void refreshCapabilityHistory();
    }
  }, [
    commandLimit,
    deviceRealtimeMarker,
    isCapabilityHistoryVisible,
    isCommandHistoryVisible,
    refreshCapabilityHistory,
    refreshCommandHistory,
  ]);

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
    capabilityEndpointFilterOptions,
    capabilityHistory,
    capabilityHistoryError,
    capabilityHistoryPage,
    capabilityHistoryPageSize,
    capabilityHistoryTotalCount,
    capabilityGroups,
    capabilityIdFilterOptions,
    closeCapabilityHistory,
    closeAssignRoomModal,
    closeCommandHistory,
    closeEditNameModal,
    collapsedItemCount: COLLAPSED_ITEM_COUNT,
    commandBlockReason,
    commandError,
    commandFilterCapability,
    commandFilterEndpoint,
    commandFilterStatus,
    commandFrom,
    commandHistory,
    commandLimit,
    commandPage,
    commandPageSize,
    commandTo,
    commandTotalCount,
    device,
    editName,
    error,
    filteredCommandHistory,
    formatCapabilityState: formatCapabilityStateWithI18n,
    getCommandEndpointLabel,
    getCommandStatusLabel: getCommandStatusLabelLocalized,
    getCommandStatusTone,
    handleAssignRoom,
    handleBooleanToggleSend,
    handleCommandSent,
    handleDeleteDevice,
    handleInlineCommandSend,
    handleSaveName,
    historyCapabilityOptions,
    historyFilterEndpoint,
    historyFrom,
    historyTo,
    homeRooms,
    inlineCommandValues,
    isAssignRoomOpen,
    isCapabilityHistoryLoading,
    isCapabilityHistoryVisible,
    isCommandHistoryVisible,
    isDeleteBusy,
    isEditNameOpen,
    isLoading,
    isSaving,
    roomId,
    openAssignRoomModal,
    openCapabilityHistory,
    openCommandHistory,
    openEditNameModal,
    optimisticToggleValues,
    quickActionError,
    quickToggleBusyCapabilityId,
    scheduleInlineCommandSend,
    selectedCapabilityForHistory,
    setCommandFilterCapability,
    setCommandFilterEndpoint,
    setCommandFilterStatus,
    setCommandFrom,
    setCommandLimit,
    setCommandTo,
    setEditName,
    setHistoryFilterEndpoint,
    setHistoryFrom,
    setHistoryTo,
    setInlineCommandValues,
    setRoomId,
    setSelectedCapabilityForHistory,
    showAllCapabilityHistory,
    showAllCommands,
    summarizePayload,
    toggleCapabilityHistoryVisibility,
    toggleCommandHistoryVisibility,
    toggleShowAllCapabilityHistory: () =>
      setShowAllCapabilityHistory((current) => !current),
    toggleShowAllCommands: () => setShowAllCommands((current) => !current),
    visibleCapabilityHistory,
    visibleCommandHistory,
  };
}
