import { useMemo } from "react";
import { useTranslation } from "react-i18next";
import {
  getControlCapabilities,
  getOperationCapabilities,
  type CapabilityRegistryMap,
  type SelectableDeviceDto,
} from "@/features/capabilities";
import type { HomeRoomOverviewDto } from "@/features/homes";
import { Button } from "@/shared/ui/Button";
import {
  createEmptyInvokeOperationActionDraft,
  createEmptySetStateActionDraft,
  type ActionDraft,
} from "../../services/actionSetFormService";
import {
  groupParallelActions,
  type IndexedActionDraft,
} from "../../services/parallelActionGroupingService";
import { ActionEditor } from "../action/ActionEditor";
import styles from "../action/ActionEditor.module.css";

type Props = {
  actions: ActionDraft[];
  title: string;
  rooms: HomeRoomOverviewDto[];
  availableDevices: SelectableDeviceDto[];
  availableDevicesByRoom: Record<string, SelectableDeviceDto[]>;
  registryMap?: CapabilityRegistryMap;
  disabled: boolean;
  onChangeAction: (index: number, action: ActionDraft) => void;
  onChangeActions: (actions: ActionDraft[]) => void;
  onAddAction: (action: ActionDraft) => void;
  onRemoveAction: (index: number) => void;
};

function getMatchingEndpoints(
  device: SelectableDeviceDto,
  type: ActionDraft["type"],
  registryMap: CapabilityRegistryMap | undefined
) {
  const capabilityFilter =
    type === "setState" ? getControlCapabilities : getOperationCapabilities;

  return device.endpoints.filter(
    (endpoint) =>
      capabilityFilter(endpoint.capabilities, registryMap).length > 0
  );
}

function createActionForDevice(
  type: ActionDraft["type"],
  device: SelectableDeviceDto,
  registryMap: CapabilityRegistryMap | undefined
): ActionDraft {
  const action =
    type === "setState"
      ? createEmptySetStateActionDraft()
      : createEmptyInvokeOperationActionDraft();
  const matchingEndpoints = getMatchingEndpoints(device, type, registryMap);

  return {
    ...action,
    roomId: device.roomId ?? "",
    deviceId: device.id,
    endpointId:
      matchingEndpoints.length === 1 ? matchingEndpoints[0].endpointId : "",
  };
}

function deviceSupportsActionType(
  device: SelectableDeviceDto,
  type: ActionDraft["type"],
  registryMap: CapabilityRegistryMap | undefined
) {
  return getMatchingEndpoints(device, type, registryMap).length > 0;
}

function retargetAction(
  action: ActionDraft,
  device: SelectableDeviceDto,
  registryMap: CapabilityRegistryMap | undefined
): ActionDraft {
  const matchingEndpoints = getMatchingEndpoints(
    device,
    action.type,
    registryMap
  );
  const endpointId =
    matchingEndpoints.length === 1 ? matchingEndpoints[0].endpointId : "";
  const target = {
    roomId: device.roomId ?? "",
    deviceId: device.id,
    endpointId,
    capabilityId: "",
  };

  return action.type === "setState"
    ? { ...action, ...target, stateText: "{}" }
    : { ...action, ...target, operation: "", payloadText: "{}" };
}

function alignActionRoom(
  action: ActionDraft,
  device: SelectableDeviceDto
): ActionDraft {
  const roomId = device.roomId ?? "";
  return action.roomId === roomId ? action : { ...action, roomId };
}

export function ParallelActionEditor({
  actions,
  title,
  rooms,
  availableDevices,
  availableDevicesByRoom,
  registryMap,
  disabled,
  onChangeAction,
  onChangeActions,
  onAddAction,
  onRemoveAction,
}: Props) {
  const { t } = useTranslation("scenes");
  const groups = useMemo(
    () => groupParallelActions(actions, availableDevices, rooms),
    [actions, availableDevices, rooms]
  );

  const addEmptyAction = (type: ActionDraft["type"]) => {
    onAddAction(
      type === "setState"
        ? createEmptySetStateActionDraft()
        : createEmptyInvokeOperationActionDraft()
    );
  };

  const changeGroupDevice = (
    deviceActions: IndexedActionDraft[],
    deviceId: string
  ) => {
    const device = availableDevices.find((item) => item.id === deviceId);
    if (!device) return;

    const nextActions = [...actions];
    deviceActions.forEach(({ action, originalIndex }) => {
      nextActions[originalIndex] = retargetAction(action, device, registryMap);
    });
    onChangeActions(nextActions);
  };

  const addActionToDevice = (
    type: ActionDraft["type"],
    device: SelectableDeviceDto,
    deviceActions: IndexedActionDraft[]
  ) => {
    const insertionIndex =
      deviceActions[deviceActions.length - 1].originalIndex + 1;
    const nextActions = [...actions];
    nextActions.splice(
      insertionIndex,
      0,
      createActionForDevice(type, device, registryMap)
    );
    onChangeActions(nextActions);
  };

  return (
    <div className={styles.stack}>
      <div className={styles.sectionHeader}>
        <div className={styles.sectionTitle}>{title}</div>
      </div>

      {actions.length === 0 ? (
        <div className={styles.fieldHelp}>
          {t("scenes.actionSet.noActions")}
        </div>
      ) : null}

      {groups.rooms.map((roomGroup) => (
        <section
          key={roomGroup.roomId ?? "unassigned"}
          className={styles.parallelRoomSection}
        >
          <div className={styles.parallelRoomHeader}>
            <h3>
              {roomGroup.roomName ?? t("scenes.actionSet.unassignedRoom")}
            </h3>
            <span>
              {t("scenes.actionItemCount", { count: roomGroup.actionCount })}
            </span>
          </div>

          <div className={styles.parallelDeviceList}>
            {roomGroup.devices.map(({ device, actions: deviceActions }) => (
              <section key={device.id} className={styles.parallelDeviceGroup}>
                <div className={styles.parallelDeviceHeader}>
                  <select
                    className={`${styles.select} ${styles.parallelDeviceSelect}`}
                    value={device.id}
                    disabled={disabled}
                    aria-label={t("scenes.deviceName")}
                    onChange={(event) =>
                      changeGroupDevice(deviceActions, event.target.value)
                    }
                  >
                    {availableDevices
                      .filter(
                        (candidate) =>
                          candidate.id === device.id ||
                          deviceActions.every(({ action }) =>
                            deviceSupportsActionType(
                              candidate,
                              action.type,
                              registryMap
                            )
                          )
                      )
                      .map((candidate) => (
                        <option key={candidate.id} value={candidate.id}>
                          {candidate.roomName
                            ? `${candidate.roomName} · ${candidate.name}`
                            : candidate.name}
                        </option>
                      ))}
                  </select>
                  <span className={styles.actionIndex}>
                    {t("scenes.actionItemCount", {
                      count: deviceActions.length,
                    })}
                  </span>
                </div>

                <ActionEditor
                  actions={deviceActions.map(({ action }) =>
                    alignActionRoom(action, device)
                  )}
                  presentation="grouped"
                  showAddActions
                  initiallyCollapsed={false}
                  rooms={rooms}
                  availableDevices={availableDevices}
                  availableDevicesByRoom={availableDevicesByRoom}
                  registryMap={registryMap}
                  disabled={disabled}
                  addSetStateDisabled={
                    !deviceSupportsActionType(device, "setState", registryMap)
                  }
                  addOperationDisabled={
                    !deviceSupportsActionType(
                      device,
                      "invokeOperation",
                      registryMap
                    )
                  }
                  onChangeAction={(index, action) =>
                    onChangeAction(deviceActions[index].originalIndex, action)
                  }
                  onAddAction={(type) =>
                    addActionToDevice(type, device, deviceActions)
                  }
                  onRemoveAction={(index) =>
                    onRemoveAction(deviceActions[index].originalIndex)
                  }
                />
              </section>
            ))}
          </div>
        </section>
      ))}

      {groups.incompleteActions.length > 0 ? (
        <ActionEditor
          actions={groups.incompleteActions.map((item) => item.action)}
          title={t("scenes.actionSet.incompleteActions")}
          rooms={rooms}
          availableDevices={availableDevices}
          availableDevicesByRoom={availableDevicesByRoom}
          registryMap={registryMap}
          disabled={disabled}
          showAddActions={false}
          initiallyCollapsed={false}
          onChangeAction={(index, action) =>
            onChangeAction(
              groups.incompleteActions[index].originalIndex,
              action
            )
          }
          onAddAction={addEmptyAction}
          onRemoveAction={(index) =>
            onRemoveAction(groups.incompleteActions[index].originalIndex)
          }
        />
      ) : null}

      <div className={styles.footer}>
        <Button
          type="button"
          size="sm"
          variant="secondary"
          onClick={() => addEmptyAction("setState")}
          disabled={disabled}
        >
          {t("scenes.actionSet.addSetState")}
        </Button>
        <Button
          type="button"
          size="sm"
          variant="secondary"
          onClick={() => addEmptyAction("invokeOperation")}
          disabled={disabled}
        >
          {t("scenes.actionSet.addOperation")}
        </Button>
      </div>
    </div>
  );
}
