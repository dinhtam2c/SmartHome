import { useState } from "react";
import { useTranslation } from "react-i18next";
import { CapabilitySchemaFieldsEditor } from "@/features/capabilities/components/CapabilitySchemaFieldsEditor";
import { LocalizedCapabilityStateValue } from "@/features/capabilities/components/CapabilityStateValue";
import {
  CAPABILITY_LABEL_KEYS,
  getCapabilityBooleanLabels,
  getCapabilityColorLabel,
  getCapabilityDisplayLabel,
  getCapabilityPrimaryStateValue,
  getCapabilityUnit,
  localizeCapabilityCommandPath,
  localizeCapabilityOperation,
  localizeCapabilityStatePath,
  resolveCapabilityFieldEditorRender,
  type CapabilityRegistryMap,
} from "@/features/capabilities";
import {
  getCapabilityRegistryEntry,
  getControlCapabilities,
  getOperationCapabilities,
  parseJsonObjectLoose,
  removeJsonObjectFields,
  resolveCapabilityDeviceSelection,
  isPlainObject,
  toSchemaFields,
  type BuilderCapabilityDto,
  type BuilderDeviceDto,
  type SchemaField,
} from "@/features/capability-builder";
import type { HomeRoomOverviewDto } from "@/features/homes";
import { resolveOperationKey } from "@/shared/lib/operationSchemaUtils";
import { Button } from "@/shared/ui/Button";
import { FormGroup } from "@/shared/ui/FormGroup";
import {
  createEmptyInvokeOperationActionDraft,
  createEmptySetStateActionDraft,
  getActionPayloadFieldValue,
  getActionStateFieldValue,
  removeActionPayloadFieldValue,
  removeActionStateFieldValue,
  updateActionPayloadFieldValue,
  updateActionStateFieldValue,
  type ActionDraft,
  type InvokeOperationActionDraft,
  type SetStateActionDraft,
} from "../../services/actionSetFormService";
import styles from "./ActionEditor.module.css";

type Props = {
  actions: ActionDraft[];
  title?: string;
  emptyText?: string;
  addSetStateLabel?: string;
  addOperationLabel?: string;
  allowReorder?: boolean;
  rooms?: HomeRoomOverviewDto[];
  availableDevices?: BuilderDeviceDto[];
  availableDevicesByRoom?: Record<string, BuilderDeviceDto[]>;
  registryMap?: CapabilityRegistryMap;
  disabled?: boolean;
  onChangeAction: (index: number, action: ActionDraft) => void;
  onAddAction: (type: ActionDraft["type"]) => void;
  onRemoveAction: (index: number) => void;
  onMoveAction?: (index: number, direction: -1 | 1) => void;
};

function resolveStateFromCapability(
  capability: BuilderCapabilityDto | null | undefined,
  readOnlyPaths: string[] = []
) {
  if (!capability?.state || !isPlainObject(capability.state)) {
    return "{}";
  }

  return removeJsonObjectFields(JSON.stringify(capability.state, null, 2), readOnlyPaths);
}

function getReadOnlyStatePaths(stateSchema: Record<string, unknown>) {
  return toSchemaFields(stateSchema)
    .filter((field) => field.readOnly)
    .map((field) => field.path.trim())
    .filter((path) => path !== "");
}

function resolveStateText(
  capability: BuilderCapabilityDto | null | undefined,
  registryMap: CapabilityRegistryMap | undefined
) {
  const capabilityEntry = getCapabilityRegistryEntry(capability, registryMap);
  const readOnlyStatePaths = capabilityEntry
    ? getReadOnlyStatePaths(capabilityEntry.stateSchema)
    : [];

  return resolveStateFromCapability(capability, readOnlyStatePaths);
}

function resolveOperationSchema(
  capability: BuilderCapabilityDto | null | undefined,
  registryMap: CapabilityRegistryMap | undefined,
  operation: string
) {
  const registryEntry = getCapabilityRegistryEntry(capability, registryMap);
  if (!registryEntry || !operation.trim()) {
    return null;
  }

  const operationKey = resolveOperationKey(registryEntry.operations, operation);
  return operationKey ? registryEntry.operations[operationKey] ?? null : null;
}

function getOperationOptions(
  capability: BuilderCapabilityDto | null | undefined,
  registryMap: CapabilityRegistryMap | undefined
) {
  const registryEntry = getCapabilityRegistryEntry(capability, registryMap);
  if (!registryEntry) {
    return [];
  }

  const registryOperations = Object.keys(registryEntry.operations ?? {}).filter(
    (operation) => {
      const operationSchema = registryEntry.operations[operation];
      const fields = isPlainObject(operationSchema)
        ? toSchemaFields(operationSchema)
        : [];
      const renderPlan = resolveCapabilityFieldEditorRender(
        capability?.capabilityId,
        fields,
        operation
      );

      return renderPlan.kind !== "unsupported" && renderPlan.skippedFields.length === 0;
    }
  );

  const supportedOperations = Array.isArray(capability?.supportedOperations)
    ? capability.supportedOperations
    : [];
  if (registryOperations.length === 0 || supportedOperations.length === 0) {
    return [];
  }

  const operationByNormalized = new Map(
    registryOperations.map((operation) => [operation.trim().toLowerCase(), operation])
  );

  return Array.from(
    new Set(
      supportedOperations
        .map((operation) => operationByNormalized.get(operation.trim().toLowerCase()))
        .filter((operation): operation is string => typeof operation === "string")
    )
  );
}

function getDefaultValueForField(field: SchemaField): unknown | null {
  if (field.type === "enum") {
    return field.enumValues[0] ?? null;
  }

  if (field.type === "boolean") {
    return false;
  }

  if (field.type === "integer") {
    return typeof field.min === "number" ? Math.trunc(field.min) : 0;
  }

  if (field.type === "number") {
    return typeof field.min === "number" ? field.min : 0;
  }

  if (field.type === "string") {
    return "";
  }

  return null;
}

function buildDefaultPayloadText(
  operationSchema: Record<string, unknown> | null | undefined
) {
  if (!operationSchema) {
    return "{}";
  }

  const fields = toSchemaFields(operationSchema);
  let payloadText = "{}";

  fields.forEach((field) => {
    if (
      !field.required ||
      field.readOnly ||
      field.type === "unsupported" ||
      field.path === ""
    ) {
      return;
    }

    const defaultValue = getDefaultValueForField(field);
    if (defaultValue === null) {
      return;
    }

    payloadText = updateActionPayloadFieldValue(payloadText, field.path, defaultValue);
  });

  return payloadText;
}

function resolveDefaultOperationDraft(
  capability: BuilderCapabilityDto | null | undefined,
  registryMap: CapabilityRegistryMap | undefined
) {
  const operationOptions = getOperationOptions(capability, registryMap);
  const operation = operationOptions[0] ?? "";

  return {
    operation,
    payloadText: buildDefaultPayloadText(
      resolveOperationSchema(capability, registryMap, operation)
    ),
  };
}

function formatFallbackStateValue(value: Record<string, unknown>, fallbackText: string) {
  return Object.keys(value).length === 0 ? fallbackText : JSON.stringify(value);
}

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
  allowReorder = false,
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
    () => new Set(actions.map((action) => action.key))
  );

  const toggleCollapsed = (key: string) => {
    setCollapsedActionKeys((current) => {
      const next = new Set(current);

      if (next.has(key)) {
        next.delete(key);
      } else {
        next.add(key);
      }

      return next;
    });
  };

  return (
    <div className={styles.stack}>
      <div className={styles.sectionHeader}>
        <div className={styles.sectionTitle}>{title ?? t("scenes.actionSet.actions")}</div>
      </div>

      {actions.length === 0 ? (
        <div className={styles.fieldHelp}>{emptyText ?? t("scenes.actionSet.noActions")}</div>
      ) : null}

      {actions.map((action, index) => {
        const capabilityFilter =
          action.type === "setState" ? getControlCapabilities : getOperationCapabilities;
        const {
          selectedRoomId,
          roomDevices,
          selectedDevice,
          selectedEndpoints,
          selectedEndpoint,
          selectedCapabilities,
          selectedCapability,
        } = resolveCapabilityDeviceSelection({
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
          onChangeAction(index, {
            ...action,
            ...patch,
          } as ActionDraft);
        };

        const isCollapsed = collapsedActionKeys.has(action.key);
        const registryEntry = getCapabilityRegistryEntry(selectedCapability, registryMap);
        const capabilityLabel = selectedCapability
          ? getCapabilityDisplayLabel(
            t,
            selectedCapability.capabilityId,
            typeof registryEntry?.metadata?.defaultName === "string"
              ? registryEntry.metadata.defaultName
              : null
          )
          : t("scenes.unselectedCapability");
        const cardClassName = [
          styles.actionCard,
          isCollapsed ? styles.actionCardCollapsed : null,
        ]
          .filter(Boolean)
          .join(" ");

        const actionTypeLabel =
          action.type === "setState"
            ? t("scenes.actionSet.setState")
            : t("scenes.actionSet.invokeOperation");

        return (
          <div key={action.key} className={cardClassName}>
            <div className={styles.actionHeader}>
              <div className={styles.actionHeaderCopy}>
                <div className={styles.actionTitleRow}>
                  <div className={styles.deviceName}>
                    {selectedDevice?.name || t("scenes.unselectedDevice")}
                  </div>
                  <span className={styles.actionIndex}>
                    {t("scenes.actionSet.actionItem", { index: index + 1 })}
                  </span>
                  <span className={styles.actionIndex}>{actionTypeLabel}</span>
                </div>
                <ActionSummary
                  action={action}
                  capabilityLabel={capabilityLabel}
                  selectedCapability={selectedCapability}
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
                selectedRoomId={selectedRoomId}
                roomDevices={roomDevices}
                selectedDevice={selectedDevice}
                selectedEndpoints={selectedEndpoints}
                selectedEndpoint={selectedEndpoint}
                selectedCapabilities={selectedCapabilities}
                selectedCapability={selectedCapability}
                registryMap={registryMap}
                disabled={disabled}
                onChangeAction={(patch) => updateAction(patch)}
              />

              {action.type === "setState" ? (
                <SetStateFields
                  action={action}
                  selectedCapability={selectedCapability}
                  registryMap={registryMap}
                  disabled={disabled}
                  onChange={(patch) => updateAction(patch)}
                />
              ) : (
                <InvokeOperationFields
                  action={action}
                  selectedCapability={selectedCapability}
                  registryMap={registryMap}
                  disabled={disabled}
                  onChange={(patch) => updateAction(patch)}
                />
              )}
            </fieldset>
          </div>
        );
      })}

      <div className={styles.footer}>
        <Button
          type="button"
          size="sm"
          variant="secondary"
          onClick={() => onAddAction("setState")}
          disabled={disabled}
        >
          {addSetStateLabel ?? t("scenes.actionSet.addSetState")}
        </Button>
        <Button
          type="button"
          size="sm"
          variant="secondary"
          onClick={() => onAddAction("invokeOperation")}
          disabled={disabled}
        >
          {addOperationLabel ?? t("scenes.actionSet.addOperation")}
        </Button>
      </div>
    </div>
  );
}

function ActionSummary({
  action,
  capabilityLabel,
  selectedCapability,
  registryEntry,
}: {
  action: ActionDraft;
  capabilityLabel: string;
  selectedCapability: BuilderCapabilityDto | undefined;
  registryEntry: ReturnType<typeof getCapabilityRegistryEntry>;
}) {
  const { t } = useTranslation("scenes");

  if (action.type === "setState") {
    const desiredState = parseJsonObjectLoose(action.stateText);
    const primaryStateValue = getCapabilityPrimaryStateValue(
      desiredState,
      registryEntry?.metadata?.primary?.state
    );

    return (
      <div className={styles.actionSummary}>
        <LocalizedCapabilityStateValue
          t={t}
          labelKeys={CAPABILITY_LABEL_KEYS.scene}
          capabilityId={selectedCapability?.capabilityId}
          label={capabilityLabel}
          value={primaryStateValue}
          fallbackText={formatFallbackStateValue(desiredState, t("notAvailable"))}
          metadata={registryEntry?.metadata}
          unit={getCapabilityUnit(registryEntry?.metadata)}
          className={styles.summaryState}
        />
      </div>
    );
  }

  const operationLabel = action.operation.trim()
    ? localizeCapabilityOperation(
      t,
      action.operation.trim(),
      CAPABILITY_LABEL_KEYS.scene
    )
    : t("scenes.unselectedOperation");

  return (
    <div className={styles.actionSummary}>
      <span className={styles.summaryText}>{capabilityLabel}</span>
      <span className={`${styles.summaryText} ${styles.summaryTextStrong}`}>
        {operationLabel}
      </span>
    </div>
  );
}

function SharedTargetFields({
  action,
  index,
  rooms,
  selectedRoomId,
  roomDevices,
  selectedDevice,
  selectedEndpoints,
  selectedEndpoint,
  selectedCapabilities,
  selectedCapability,
  registryMap,
  disabled,
  onChangeAction,
}: {
  action: ActionDraft;
  index: number;
  rooms: HomeRoomOverviewDto[];
  selectedRoomId: string;
  roomDevices: BuilderDeviceDto[];
  selectedDevice: BuilderDeviceDto | undefined;
  selectedEndpoints: BuilderDeviceDto["endpoints"];
  selectedEndpoint: BuilderDeviceDto["endpoints"][number] | undefined;
  selectedCapabilities: BuilderCapabilityDto[];
  selectedCapability: BuilderCapabilityDto | undefined;
  registryMap: CapabilityRegistryMap | undefined;
  disabled: boolean;
  onChangeAction: (patch: Partial<ActionDraft>) => void;
}) {
  const { t } = useTranslation("scenes");
  const capabilityFilter =
    action.type === "setState" ? getControlCapabilities : getOperationCapabilities;

  const buildCapabilityDefaults = (
    capability: BuilderCapabilityDto | null | undefined
  ) => {
    if (action.type === "setState") {
      return {
        stateText: resolveStateText(capability, registryMap),
        optionsText: "{}",
      };
    }

    return resolveDefaultOperationDraft(capability, registryMap);
  };

  return (
    <>
      <FormGroup
        label={t("scenes.room")}
        htmlFor={`action-set-room-${action.key}-${index}`}
        required={false}
      >
        <select
          id={`action-set-room-${action.key}-${index}`}
          className={styles.select}
          value={selectedRoomId}
          disabled={disabled}
          onChange={(event) => {
            onChangeAction({
              roomId: event.target.value,
              deviceId: "",
              endpointId: "",
              capabilityId: "",
              ...(action.type === "setState"
                ? { stateText: "{}", optionsText: "{}" }
                : { operation: "", payloadText: "{}" }),
            } as Partial<ActionDraft>);
          }}
        >
          <option value="">{t("scenes.allRooms")}</option>
          {rooms.map((room) => (
            <option key={`${action.key}:room:${room.id}`} value={room.id}>
              {room.name}
            </option>
          ))}
        </select>
      </FormGroup>

      <FormGroup
        label={t("scenes.deviceName")}
        htmlFor={`action-set-device-${action.key}-${index}`}
      >
        <select
          id={`action-set-device-${action.key}-${index}`}
          className={styles.select}
          value={action.deviceId}
          disabled={disabled || roomDevices.length === 0}
          onChange={(event) => {
            const nextDeviceId = event.target.value;
            const nextDevice = roomDevices.find((device) => device.id === nextDeviceId);
            const nextEndpoint = nextDevice?.endpoints[0];
            const nextCapabilities = capabilityFilter(
              nextEndpoint?.capabilities ?? [],
              registryMap
            );
            const nextCapability = nextCapabilities[0];

            onChangeAction({
              roomId: nextDevice?.roomId ?? selectedRoomId,
              deviceId: nextDeviceId,
              endpointId: nextEndpoint?.endpointId ?? "",
              capabilityId: nextCapability?.capabilityId ?? "",
              ...buildCapabilityDefaults(nextCapability),
            } as Partial<ActionDraft>);
          }}
          required
        >
          <option value="">{t("scenes.selectDevice")}</option>
          {roomDevices.length === 0 ? (
            <option value="" disabled>
              {t("scenes.noDevicesAvailable")}
            </option>
          ) : null}
          {roomDevices.map((device) => (
            <option key={`${action.key}:${device.id}`} value={device.id}>
              {device.name}
            </option>
          ))}
        </select>
      </FormGroup>

      <FormGroup
        label={t("scenes.endpointId")}
        htmlFor={`action-set-endpoint-${action.key}-${index}`}
      >
        <select
          id={`action-set-endpoint-${action.key}-${index}`}
          className={styles.select}
          value={action.endpointId}
          disabled={disabled || !action.deviceId.trim() || selectedEndpoints.length === 0}
          onChange={(event) => {
            const nextEndpointId = event.target.value;
            const nextEndpoint = selectedEndpoints.find(
              (endpoint) => endpoint.endpointId === nextEndpointId
            );
            const nextCapabilities = capabilityFilter(
              nextEndpoint?.capabilities ?? [],
              registryMap
            );
            const nextCapability = nextCapabilities[0];

            onChangeAction({
              endpointId: nextEndpointId,
              capabilityId: nextCapability?.capabilityId ?? "",
              ...buildCapabilityDefaults(nextCapability),
            } as Partial<ActionDraft>);
          }}
          required
        >
          <option value="">{t("scenes.selectEndpoint")}</option>
          {selectedEndpoints.map((endpoint) => (
            <option
              key={`${action.key}:${selectedDevice?.id ?? "device"}:${endpoint.endpointId}`}
              value={endpoint.endpointId}
            >
              {endpoint.name || endpoint.endpointId}
            </option>
          ))}
        </select>
      </FormGroup>

      <FormGroup
        label={t("scenes.capability")}
        htmlFor={`action-set-capability-${action.key}-${index}`}
      >
        <select
          id={`action-set-capability-${action.key}-${index}`}
          className={styles.select}
          value={action.capabilityId}
          disabled={
            disabled ||
            !action.deviceId.trim() ||
            !action.endpointId.trim() ||
            selectedCapabilities.length === 0
          }
          onChange={(event) => {
            const nextCapabilityId = event.target.value;
            const nextCapability = selectedCapabilities.find(
              (capability) => capability.capabilityId === nextCapabilityId
            );

            onChangeAction({
              capabilityId: nextCapabilityId,
              ...buildCapabilityDefaults(nextCapability),
            } as Partial<ActionDraft>);
          }}
          required
        >
          <option value="">{t("scenes.selectCapability")}</option>
          {selectedCapabilities.map((capability) => {
            const capabilityRegistryEntry = getCapabilityRegistryEntry(
              capability,
              registryMap
            );

            return (
              <option
                key={`${action.key}:${selectedDevice?.id ?? "device"}:${selectedEndpoint?.endpointId ?? "endpoint"}:${capability.capabilityId}:${capability.capabilityVersion}`}
                value={capability.capabilityId}
              >
                {getCapabilityDisplayLabel(
                  t,
                  capability.capabilityId,
                  typeof capabilityRegistryEntry?.metadata?.defaultName === "string"
                    ? capabilityRegistryEntry.metadata.defaultName
                    : null
                )}
              </option>
            );
          })}
        </select>
      </FormGroup>

      {selectedCapability ? null : (
        <div className={styles.fieldHelp}>{t("scenes.noCommandableCapability")}</div>
      )}
    </>
  );
}

function SetStateFields({
  action,
  selectedCapability,
  registryMap,
  disabled,
  onChange,
}: {
  action: SetStateActionDraft;
  selectedCapability: BuilderCapabilityDto | undefined;
  registryMap: CapabilityRegistryMap | undefined;
  disabled: boolean;
  onChange: (patch: Partial<SetStateActionDraft>) => void;
}) {
  const { t } = useTranslation("scenes");
  const localizeStatePathLabel = (path: string) =>
    localizeCapabilityStatePath(t, path, CAPABILITY_LABEL_KEYS.scene);
  const registryEntry = getCapabilityRegistryEntry(selectedCapability, registryMap);
  const schemaFields = registryEntry ? toSchemaFields(registryEntry.stateSchema) : [];
  const writableSchemaFields = schemaFields.filter((field) => !field.readOnly);
  const readOnlyStatePaths = schemaFields
    .filter((field) => field.readOnly)
    .map((field) => field.path.trim())
    .filter((path) => path !== "");

  return (
    <>
      {writableSchemaFields.length > 0 ? (
        <CapabilitySchemaFieldsEditor
          capabilityId={action.capabilityId}
          idPrefix={`action-set-state-${action.key}`}
          fields={writableSchemaFields}
          disabled={disabled}
          getValue={(path) => getActionStateFieldValue(action.stateText, path)}
          setValue={(path, nextValue) =>
            onChange({
              stateText: updateActionStateFieldValue(
                action.stateText,
                path,
                nextValue
              ),
            })
          }
          clearValue={(path) =>
            onChange({
              stateText: removeActionStateFieldValue(action.stateText, path),
            })
          }
          getFieldLabel={localizeStatePathLabel}
          labels={{
            clear: t("scenes.clear"),
            ...getCapabilityBooleanLabels(t, CAPABILITY_LABEL_KEYS.scene),
            rgbColor: getCapabilityColorLabel(t, CAPABILITY_LABEL_KEYS.scene),
          }}
        />
      ) : (
        <div className={styles.stateFieldFallback}>{t("scenes.noSchemaFields")}</div>
      )}

      {readOnlyStatePaths.length > 0 ? (
        <div className={styles.fieldHelp}>{t("scenes.readOnlyFieldsSkipped")}</div>
      ) : null}
    </>
  );
}

function InvokeOperationFields({
  action,
  selectedCapability,
  registryMap,
  disabled,
  onChange,
}: {
  action: InvokeOperationActionDraft;
  selectedCapability: BuilderCapabilityDto | undefined;
  registryMap: CapabilityRegistryMap | undefined;
  disabled: boolean;
  onChange: (patch: Partial<InvokeOperationActionDraft>) => void;
}) {
  const { t } = useTranslation("scenes");
  const localizeParamPathLabel = (path: string) =>
    localizeCapabilityCommandPath(t, path, CAPABILITY_LABEL_KEYS.scene);
  const operationOptions = getOperationOptions(selectedCapability, registryMap);
  const selectedOperation = action.operation.trim();
  const operationSchema = resolveOperationSchema(
    selectedCapability,
    registryMap,
    selectedOperation
  );
  const allParameterFields =
    operationSchema && isPlainObject(operationSchema)
      ? toSchemaFields(operationSchema)
      : [];
  const parameterFields = allParameterFields.filter((field) => !field.readOnly);
  const hasReadOnlyParameterField = allParameterFields.some((field) => field.readOnly);

  return (
    <>
      <FormGroup
        label={t("scenes.operation")}
        htmlFor={`action-set-operation-${action.key}`}
      >
        <select
          id={`action-set-operation-${action.key}`}
          className={styles.select}
          value={action.operation}
          disabled={
            disabled ||
            !action.deviceId.trim() ||
            !action.endpointId.trim() ||
            !action.capabilityId.trim() ||
            operationOptions.length === 0
          }
          onChange={(event) => {
            const nextOperation = event.target.value;
            const nextOperationSchema = resolveOperationSchema(
              selectedCapability,
              registryMap,
              nextOperation
            );

            onChange({
              operation: nextOperation,
              payloadText: buildDefaultPayloadText(nextOperationSchema),
            });
          }}
          required
        >
          <option value="">{t("scenes.selectOperation")}</option>
          {operationOptions.map((operation) => (
            <option key={`${action.key}:operation:${operation}`} value={operation}>
              {localizeCapabilityOperation(t, operation, CAPABILITY_LABEL_KEYS.scene)}
            </option>
          ))}
        </select>
      </FormGroup>

      {parameterFields.length > 0 ? (
        <CapabilitySchemaFieldsEditor
          capabilityId={action.capabilityId}
          operation={selectedOperation}
          idPrefix={`action-set-operation-payload-${action.key}`}
          fields={parameterFields}
          disabled={disabled}
          getValue={(path) => getActionPayloadFieldValue(action.payloadText, path)}
          setValue={(path, nextValue) =>
            onChange({
              payloadText: updateActionPayloadFieldValue(
                action.payloadText,
                path,
                nextValue
              ),
            })
          }
          clearValue={(path) =>
            onChange({
              payloadText: removeActionPayloadFieldValue(action.payloadText, path),
            })
          }
          getFieldLabel={localizeParamPathLabel}
          labels={{
            clear: t("scenes.clear"),
            ...getCapabilityBooleanLabels(t, CAPABILITY_LABEL_KEYS.scene),
            rgbColor: getCapabilityColorLabel(t, CAPABILITY_LABEL_KEYS.scene),
          }}
        />
      ) : (
        <div className={styles.stateFieldFallback}>
          {t("scenes.noOperationSchemaFields")}
        </div>
      )}

      {hasReadOnlyParameterField ? (
        <div className={styles.fieldHelp}>{t("scenes.readOnlyFieldsSkipped")}</div>
      ) : null}
    </>
  );
}
