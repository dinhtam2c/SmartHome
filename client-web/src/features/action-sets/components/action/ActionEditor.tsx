import { useState } from "react";
import { useTranslation } from "react-i18next";
import {
  getCapabilityDisplayLabel,
  getCapabilityRegistryEntry,
  getControlCapabilities,
  getOperationCapabilities,
  resolveCapabilityDeviceSelection,
  type CapabilityRegistryMap,
  type SelectableDeviceDto,
} from "@/features/capabilities";
import type { HomeRoomOverviewDto } from "@/features/homes";
import { Button } from "@/shared/ui/Button";
import { FormGroup } from "@/shared/ui/FormGroup";
import {
  createEmptyInvokeOperationActionDraft,
  createEmptySetStateActionDraft,
  type ActionDraft,
  type InvokeOperationActionDraft,
  type SetStateActionDraft,
} from "../../services/actionSetFormService";
import {
  ActionSummary,
  InvokeOperationFields,
  SetStateFields,
  SharedTargetFields,
} from "./ActionEditorFields";
import styles from "./ActionEditor.module.css";

type Props = {
  actions: ActionDraft[];
  title?: string;
  emptyText?: string;
  addSetStateLabel?: string;
  addOperationLabel?: string;
  addSetStateDisabled?: boolean;
  addOperationDisabled?: boolean;
  allowReorder?: boolean;
  presentation?: "default" | "grouped";
  showAddActions?: boolean;
  initiallyCollapsed?: boolean;
  rooms?: HomeRoomOverviewDto[];
  availableDevices?: SelectableDeviceDto[];
  availableDevicesByRoom?: Record<string, SelectableDeviceDto[]>;
  registryMap?: CapabilityRegistryMap;
  disabled?: boolean;
  onChangeAction: (index: number, action: ActionDraft) => void;
  onAddAction: (type: ActionDraft["type"]) => void;
  onRemoveAction: (index: number) => void;
  onMoveAction?: (index: number, direction: -1 | 1) => void;
};

function toSetStateDraft(action: ActionDraft): SetStateActionDraft {
  return {
    ...createEmptySetStateActionDraft(),
    key: action.key,
    roomId: action.roomId,
    deviceId: action.deviceId,
    endpointId: action.endpointId,
    capabilityId: "",
  };
}

function toInvokeOperationDraft(action: ActionDraft): InvokeOperationActionDraft {
  return {
    ...createEmptyInvokeOperationActionDraft(),
    key: action.key,
    roomId: action.roomId,
    deviceId: action.deviceId,
    endpointId: action.endpointId,
    capabilityId: "",
  };
}

export function ActionEditor({
  actions,
  title,
  emptyText,
  addSetStateLabel,
  addOperationLabel,
  addSetStateDisabled = false,
  addOperationDisabled = false,
  allowReorder = false,
  presentation = "default",
  showAddActions = true,
  initiallyCollapsed = true,
  rooms = [],
  availableDevices = [],
  availableDevicesByRoom = {},
  registryMap,
  disabled = false,
  onChangeAction,
  onAddAction,
  onRemoveAction,
  onMoveAction,
}: Props) {
  const { t } = useTranslation("scenes");
  const [collapsedActionKeys, setCollapsedActionKeys] = useState<Set<string>>(
    () =>
      initiallyCollapsed
        ? new Set(actions.map((action) => action.key))
        : new Set()
  );

  const toggleCollapsed = (key: string) => {
    setCollapsedActionKeys((current) => {
      const next = new Set(current);
      if (next.has(key)) next.delete(key);
      else next.add(key);
      return next;
    });
  };

  return (
    <div className={styles.stack}>
      {presentation === "default" ? (
        <div className={styles.sectionHeader}>
          <div className={styles.sectionTitle}>
            {title ?? t("scenes.actionSet.actions")}
          </div>
        </div>
      ) : null}

      {actions.length === 0 ? (
        <div className={styles.fieldHelp}>{emptyText ?? t("scenes.actionSet.noActions")}</div>
      ) : null}

      {actions.map((action, index) => {
        const capabilityFilter =
          action.type === "setState" ? getControlCapabilities : getOperationCapabilities;
        const selection = resolveCapabilityDeviceSelection({
          roomId: action.roomId,
          deviceId: action.deviceId,
          endpointId: action.endpointId,
          capabilityId: action.capabilityId,
          availableDevices,
          availableDevicesByRoom,
          registryMap,
          filterCapabilities: capabilityFilter,
        });
        const updateAction = (patch: Partial<ActionDraft>) => {
          onChangeAction(index, { ...action, ...patch } as ActionDraft);
        };
        const isCollapsed = collapsedActionKeys.has(action.key);
        const registryEntry = getCapabilityRegistryEntry(
          selection.selectedCapability,
          registryMap
        );
        const capabilityLabel = selection.selectedCapability
          ? getCapabilityDisplayLabel(
            t,
            selection.selectedCapability.capabilityId,
            typeof registryEntry?.metadata?.defaultName === "string"
              ? registryEntry.metadata.defaultName
              : null
          )
          : t("scenes.unselectedCapability");
        const actionTypeLabel = action.type === "setState"
          ? t("scenes.actionSet.setState")
          : t("scenes.actionSet.invokeOperation");

        return (
          <div
            key={action.key}
            className={`${styles.actionCard} ${isCollapsed ? styles.actionCardCollapsed : ""}`}
          >
            <div className={styles.actionHeader}>
              <div className={styles.actionHeaderCopy}>
                <div className={styles.actionTitleRow}>
                  <div className={styles.deviceName}>
                    {selection.selectedDevice?.name || t("scenes.unselectedDevice")}
                  </div>
                  <span className={styles.actionIndex}>
                    {t("scenes.actionSet.actionItem", { index: index + 1 })}
                  </span>
                  <span className={styles.actionIndex}>{actionTypeLabel}</span>
                </div>
                <ActionSummary
                  action={action}
                  capabilityLabel={capabilityLabel}
                  selectedCapability={selection.selectedCapability}
                  registryEntry={registryEntry}
                />
              </div>

              <div className={styles.actionHeaderActions}>
                {allowReorder ? (
                  <>
                    <Button
                      type="button"
                      size="sm"
                      variant="secondary"
                      className={styles.headerButton}
                      onClick={() => onMoveAction?.(index, -1)}
                      disabled={disabled || index === 0}
                    >
                      {t("scenes.moveUp")}
                    </Button>
                    <Button
                      type="button"
                      size="sm"
                      variant="secondary"
                      className={styles.headerButton}
                      onClick={() => onMoveAction?.(index, 1)}
                      disabled={disabled || index === actions.length - 1}
                    >
                      {t("scenes.moveDown")}
                    </Button>
                  </>
                ) : null}
                <Button
                  type="button"
                  size="sm"
                  variant="secondary"
                  className={styles.headerButton}
                  aria-expanded={!isCollapsed}
                  onClick={() => toggleCollapsed(action.key)}
                >
                  {isCollapsed ? t("scenes.expandItem") : t("scenes.collapseItem")}
                </Button>
                <Button
                  type="button"
                  size="sm"
                  variant="secondary"
                  className={`${styles.headerButton} ${styles.deleteButton}`}
                  onClick={() => onRemoveAction(index)}
                  disabled={disabled}
                >
                  {t("scenes.deleteShort")}
                </Button>
              </div>
            </div>

            <fieldset
              className={`${styles.fields} ${isCollapsed ? styles.fieldsCollapsed : ""}`}
              disabled={disabled || isCollapsed}
            >
              <FormGroup
                label={t("scenes.actionSet.actionType")}
                htmlFor={`action-set-type-${action.key}`}
              >
                <select
                  id={`action-set-type-${action.key}`}
                  className={styles.select}
                  value={action.type}
                  disabled={disabled}
                  onChange={(event) => {
                    const nextType = event.target.value as ActionDraft["type"];
                    onChangeAction(
                      index,
                      nextType === "setState"
                        ? toSetStateDraft(action)
                        : toInvokeOperationDraft(action)
                    );
                  }}
                >
                  <option value="setState">{t("scenes.actionSet.setState")}</option>
                  <option value="invokeOperation">
                    {t("scenes.actionSet.invokeOperation")}
                  </option>
                </select>
              </FormGroup>

              <SharedTargetFields
                action={action}
                index={index}
                rooms={rooms}
                selectedRoomId={selection.selectedRoomId}
                roomDevices={selection.roomDevices}
                selectedDevice={selection.selectedDevice}
                selectedEndpoints={selection.selectedEndpoints}
                selectedEndpoint={selection.selectedEndpoint}
                selectedCapabilities={selection.selectedCapabilities}
                selectedCapability={selection.selectedCapability}
                registryMap={registryMap}
                disabled={disabled}
                onChangeAction={updateAction}
              />

              {action.type === "setState" ? (
                <SetStateFields
                  action={action}
                  selectedCapability={selection.selectedCapability}
                  registryMap={registryMap}
                  disabled={disabled}
                  onChange={updateAction}
                />
              ) : (
                <InvokeOperationFields
                  action={action}
                  selectedCapability={selection.selectedCapability}
                  registryMap={registryMap}
                  disabled={disabled}
                  onChange={updateAction}
                />
              )}
            </fieldset>
          </div>
        );
      })}

      {showAddActions ? (
        <div className={styles.footer}>
          <Button
            type="button"
            size="sm"
            variant="secondary"
            onClick={() => onAddAction("setState")}
            disabled={disabled || addSetStateDisabled}
          >
            {addSetStateLabel ?? t("scenes.actionSet.addSetState")}
          </Button>
          <Button
            type="button"
            size="sm"
            variant="secondary"
            onClick={() => onAddAction("invokeOperation")}
            disabled={disabled || addOperationDisabled}
          >
            {addOperationLabel ?? t("scenes.actionSet.addOperation")}
          </Button>
        </div>
      ) : null}
    </div>
  );
}
