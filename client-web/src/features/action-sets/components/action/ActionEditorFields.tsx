import { useTranslation } from "react-i18next";
import { CapabilitySchemaFieldsEditor } from "@/features/capabilities";
import { LocalizedCapabilityStateValue } from "@/features/capabilities";
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
  isPlainObject,
  toSchemaFields,
  type SelectableCapabilityDto,
  type SelectableDeviceDto,
  type SchemaField,
} from "@/features/capabilities";
import type { HomeRoomOverviewDto } from "@/features/homes";
import { resolveOperationKey } from "@/features/capabilities";
import { FormGroup } from "@/shared/ui/FormGroup";
import {
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

function resolveStateFromCapability(
  capability: SelectableCapabilityDto | null | undefined,
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
  capability: SelectableCapabilityDto | null | undefined,
  registryMap: CapabilityRegistryMap | undefined
) {
  const capabilityEntry = getCapabilityRegistryEntry(capability, registryMap);
  const readOnlyStatePaths = capabilityEntry
    ? getReadOnlyStatePaths(capabilityEntry.stateSchema)
    : [];

  return resolveStateFromCapability(capability, readOnlyStatePaths);
}

function resolveOperationSchema(
  capability: SelectableCapabilityDto | null | undefined,
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
  capability: SelectableCapabilityDto | null | undefined,
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
  capability: SelectableCapabilityDto | null | undefined,
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

export function ActionSummary({
  action,
  capabilityLabel,
  selectedCapability,
  registryEntry,
}: {
  action: ActionDraft;
  capabilityLabel: string;
  selectedCapability: SelectableCapabilityDto | undefined;
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

export function SharedTargetFields({
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
  roomDevices: SelectableDeviceDto[];
  selectedDevice: SelectableDeviceDto | undefined;
  selectedEndpoints: SelectableDeviceDto["endpoints"];
  selectedEndpoint: SelectableDeviceDto["endpoints"][number] | undefined;
  selectedCapabilities: SelectableCapabilityDto[];
  selectedCapability: SelectableCapabilityDto | undefined;
  registryMap: CapabilityRegistryMap | undefined;
  disabled: boolean;
  onChangeAction: (patch: Partial<ActionDraft>) => void;
}) {
  const { t } = useTranslation("scenes");
  const capabilityFilter =
    action.type === "setState" ? getControlCapabilities : getOperationCapabilities;

  const buildCapabilityDefaults = (
    capability: SelectableCapabilityDto | null | undefined
  ) => {
    if (action.type === "setState") {
      return {
        stateText: resolveStateText(capability, registryMap),
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
                ? { stateText: "{}" }
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

export function SetStateFields({
  action,
  selectedCapability,
  registryMap,
  disabled,
  onChange,
}: {
  action: SetStateActionDraft;
  selectedCapability: SelectableCapabilityDto | undefined;
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

      {readOnlyStatePaths.length > 0 ? (
        <div className={styles.fieldHelp}>{t("scenes.readOnlyFieldsSkipped")}</div>
      ) : null}
    </>
  );
}

export function InvokeOperationFields({
  action,
  selectedCapability,
  registryMap,
  disabled,
  onChange,
}: {
  action: InvokeOperationActionDraft;
  selectedCapability: SelectableCapabilityDto | undefined;
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
