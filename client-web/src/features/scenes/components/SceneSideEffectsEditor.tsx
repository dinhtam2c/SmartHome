import { BooleanSwitch } from "@/components/BooleanSwitch";
import { NumericSliderField } from "@/components/NumericSliderField";
import { Button } from "@/components/Button";
import {
  getCapabilityDisplayLabel,
  getCapabilityRegistryKey,
  type CapabilityRegistryMap,
} from "@/features/capabilities";
import type {
  HomeRoomOverviewDto,
  HomeSceneBuilderCapabilityDto,
  HomeSceneBuilderDeviceDto,
} from "@/features/homes/homes.types";
import { FormGroup } from "@/components/FormGroup";
import { Input } from "@/components/Input";
import type { SceneSideEffectTiming } from "../scenes.types";
import {
  getSideEffectParamFieldValue,
  removeSideEffectParamFieldValue,
  updateSideEffectParamFieldValue,
  type SceneSideEffectDraft,
  type SceneTargetDraft,
} from "../sceneFormUtils";
import styles from "./SceneTargetsEditor.module.css";
import { isPlainObject, toSchemaFields, type SchemaField } from "./schemaFieldUtils";
import { useTranslation } from "react-i18next";

type Props = {
  sideEffects: SceneSideEffectDraft[];
  targets?: SceneTargetDraft[];
  rooms?: HomeRoomOverviewDto[];
  availableDevices?: HomeSceneBuilderDeviceDto[];
  availableDevicesByRoom?: Record<string, HomeSceneBuilderDeviceDto[]>;
  registryMap?: CapabilityRegistryMap;
  disabled?: boolean;
  onChangeSideEffect: (index: number, sideEffect: SceneSideEffectDraft) => void;
  onAddSideEffect: () => void;
  onRemoveSideEffect: (index: number) => void;
};

const SIDE_EFFECT_TIMING_OPTIONS: SceneSideEffectTiming[] = [
  "BeforeTargets",
  "AfterDispatch",
  "AfterVerify",
];

function resolveOperationKey(
  operations: Record<string, Record<string, unknown>>,
  operation: string
) {
  const normalized = operation.trim().toLowerCase();

  if (!normalized) {
    return null;
  }

  const exactMatch = operations[operation];
  if (exactMatch && isPlainObject(exactMatch)) {
    return operation;
  }

  return (
    Object.keys(operations).find(
      (key) => key.trim().toLowerCase() === normalized
    ) ?? null
  );
}

function getCapabilityRegistryEntry(
  capability: HomeSceneBuilderCapabilityDto | null | undefined,
  registryMap: CapabilityRegistryMap | undefined
) {
  if (!capability || !registryMap) {
    return null;
  }

  return registryMap.get(
    getCapabilityRegistryKey(
      capability.capabilityId,
      capability.capabilityVersion
    )
  ) ?? null;
}

function getSideEffectCapabilities(
  capabilities: HomeSceneBuilderCapabilityDto[],
  registryMap: CapabilityRegistryMap | undefined
) {
  if (!registryMap) {
    return [];
  }

  return capabilities.filter((capability) => {
    const registryEntry = getCapabilityRegistryEntry(capability, registryMap);

    if (!registryEntry || registryEntry.role === "Sensor") {
      return false;
    }

    return Object.keys(registryEntry.operations ?? {}).length > 0;
  });
}

function getOperationOptions(
  capability: HomeSceneBuilderCapabilityDto | null | undefined,
  registryMap: CapabilityRegistryMap | undefined
) {
  const registryEntry = getCapabilityRegistryEntry(capability, registryMap);
  if (!registryEntry) {
    return [];
  }

  const registryOperations = Object.keys(registryEntry.operations ?? {});
  if (registryOperations.length === 0) {
    return [];
  }

  const supportedOperations = Array.isArray(capability?.supportedOperations)
    ? capability.supportedOperations
    : [];

  if (supportedOperations.length === 0) {
    return registryOperations;
  }

  const operationByNormalized = new Map(
    registryOperations.map((operation) => [operation.trim().toLowerCase(), operation])
  );

  const intersected = Array.from(
    new Set(
      supportedOperations
        .map((operation) => operationByNormalized.get(operation.trim().toLowerCase()))
        .filter((operation): operation is string => typeof operation === "string")
    )
  );

  return intersected.length > 0 ? intersected : registryOperations;
}

function getDefaultValueForField(field: SchemaField): unknown | null {
  if (field.type === "enum") {
    return field.enumValues[0] ?? null;
  }

  if (field.type === "boolean") {
    return false;
  }

  if (field.type === "integer") {
    if (typeof field.min === "number") {
      return Math.trunc(field.min);
    }

    return 0;
  }

  if (field.type === "number") {
    if (typeof field.min === "number") {
      return field.min;
    }

    return 0;
  }

  if (field.type === "string") {
    return "";
  }

  return null;
}

function buildDefaultParamsText(
  operationSchema: Record<string, unknown> | null | undefined
) {
  if (!operationSchema) {
    return "{}";
  }

  const fields = toSchemaFields(operationSchema);
  let paramsText = "{}";

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

    paramsText = updateSideEffectParamFieldValue(paramsText, field.path, defaultValue);
  });

  return paramsText;
}

export function SceneSideEffectsEditor({
  sideEffects,
  targets = [],
  rooms = [],
  availableDevices = [],
  availableDevicesByRoom = {},
  registryMap,
  disabled = false,
  onChangeSideEffect,
  onAddSideEffect,
  onRemoveSideEffect,
}: Props) {
  const { t } = useTranslation("homes");

  const localizeParamPathLabel = (path: string) => {
    const normalizedPath = path.trim();

    if (!normalizedPath) {
      return t("scenes.stateValue");
    }

    const segments = normalizedPath
      .split(".")
      .map((segment) => segment.trim())
      .filter((segment) => segment !== "");

    if (segments.length === 0) {
      return t("scenes.stateValue");
    }

    const leafKey = segments[segments.length - 1];
    const localizedLeaf = t(`scenes.commandKeyLabels.${leafKey}`, {
      defaultValue: t(`scenes.stateKeyLabels.${leafKey}`, {
        defaultValue: leafKey,
      }),
    });

    if (segments.length === 1) {
      return localizedLeaf;
    }

    return `${segments.slice(0, -1).join(".")}.${localizedLeaf}`;
  };

  const localizeOperationLabel = (operation: string) => {
    const normalized = operation.trim();
    if (!normalized) {
      return normalized;
    }

    return t(`scenes.operationKeyLabels.${normalized.toLowerCase()}`, {
      defaultValue: normalized,
    });
  };

  return (
    <div className={styles.stack}>
      <div className={styles.sectionHeader}>
        <div className={styles.sectionTitle}>{t("scenes.sideEffects")}</div>
      </div>

      {sideEffects.length === 0 ? (
        <div className={styles.fieldHelp}>{t("scenes.noSideEffects")}</div>
      ) : null}

      {sideEffects.map((sideEffect, index) => {
        const selectedDeviceFromAll = availableDevices.find(
          (device) => device.id === sideEffect.deviceId
        );
        const selectedRoomId =
          sideEffect.roomId.trim() || selectedDeviceFromAll?.roomId || "";

        const roomDevices = selectedRoomId
          ? (availableDevicesByRoom[selectedRoomId] ??
            availableDevices.filter((device) => device.roomId === selectedRoomId))
          : availableDevices;

        const selectedDevice =
          roomDevices.find((device) => device.id === sideEffect.deviceId) ??
          selectedDeviceFromAll;

        const selectedEndpoints = selectedDevice?.endpoints ?? [];
        const selectedEndpoint = selectedEndpoints.find(
          (endpoint) => endpoint.endpointId === sideEffect.endpointId
        );
        const selectedCapabilities = getSideEffectCapabilities(
          selectedEndpoint?.capabilities ?? [],
          registryMap
        );
        const selectedCapability = selectedCapabilities.find(
          (capability) => capability.capabilityId === sideEffect.capabilityId
        );

        const selectedRegistryEntry = getCapabilityRegistryEntry(
          selectedCapability,
          registryMap
        );
        const operationOptions = getOperationOptions(selectedCapability, registryMap);
        const normalizedCurrentOperation = sideEffect.operation.trim().toLowerCase();
        const operationOptionsWithCurrent =
          normalizedCurrentOperation &&
            !operationOptions.some(
              (operation) =>
                operation.trim().toLowerCase() === normalizedCurrentOperation
            )
            ? [sideEffect.operation.trim(), ...operationOptions]
            : operationOptions;

        const selectedOperation = sideEffect.operation.trim();

        const operationKey = selectedRegistryEntry
          ? resolveOperationKey(selectedRegistryEntry.operations, selectedOperation)
          : null;
        const operationSchema =
          operationKey && selectedRegistryEntry
            ? selectedRegistryEntry.operations[operationKey]
            : null;

        const allParameterFields =
          operationSchema && isPlainObject(operationSchema)
            ? toSchemaFields(operationSchema)
            : [];
        const parameterFields = allParameterFields.filter((field) => !field.readOnly);
        const hasReadOnlyParameterField = allParameterFields.some(
          (field) => field.readOnly
        );

        const hasDuplicateTarget = targets.some(
          (target) =>
            target.deviceId === sideEffect.deviceId &&
            target.endpointId === sideEffect.endpointId &&
            target.capabilityId === sideEffect.capabilityId
        );

        return (
          <div key={sideEffect.key} className={styles.actionCard}>
            <div className={styles.actionHeader}>
              <div className={styles.actionTitle}>
                {t("scenes.sideEffectItem", { index: index + 1 })}
              </div>
              <Button
                type="button"
                size="sm"
                variant="danger"
                onClick={() => onRemoveSideEffect(index)}
                disabled={disabled}
              >
                {t("scenes.removeSideEffect")}
              </Button>
            </div>

            <div className={styles.fields}>
              <FormGroup
                label={t("scenes.room")}
                htmlFor={`scene-side-effect-room-${index}`}
                required={false}
              >
                <select
                  id={`scene-side-effect-room-${index}`}
                  className={styles.select}
                  value={selectedRoomId}
                  disabled={disabled}
                  onChange={(event) => {
                    const nextRoomId = event.target.value;

                    onChangeSideEffect(index, {
                      ...sideEffect,
                      roomId: nextRoomId,
                      deviceId: "",
                      endpointId: "",
                      capabilityId: "",
                      operation: "",
                      paramsText: "{}",
                    });
                  }}
                >
                  <option value="">{t("scenes.allRooms")}</option>
                  {rooms.map((room) => (
                    <option key={`${sideEffect.key}:room:${room.id}`} value={room.id}>
                      {room.name}
                    </option>
                  ))}
                </select>
              </FormGroup>

              <FormGroup
                label={t("scenes.deviceName")}
                htmlFor={`scene-side-effect-device-${index}`}
              >
                <select
                  id={`scene-side-effect-device-${index}`}
                  className={styles.select}
                  value={sideEffect.deviceId}
                  disabled={disabled || roomDevices.length === 0}
                  onChange={(event) => {
                    const nextDeviceId = event.target.value;
                    const nextDevice = roomDevices.find((device) => device.id === nextDeviceId);
                    const nextEndpoint = nextDevice?.endpoints[0];
                    const nextCapabilities = getSideEffectCapabilities(
                      nextEndpoint?.capabilities ?? [],
                      registryMap
                    );
                    const nextCapability = nextCapabilities[0];
                    const nextOperationOptions = getOperationOptions(nextCapability, registryMap);
                    const nextOperation = nextOperationOptions[0] ?? "";
                    const nextCapabilityRegistryEntry = getCapabilityRegistryEntry(
                      nextCapability,
                      registryMap
                    );
                    const nextOperationKey =
                      nextCapabilityRegistryEntry && nextOperation
                        ? resolveOperationKey(
                          nextCapabilityRegistryEntry.operations,
                          nextOperation
                        )
                        : null;
                    const nextOperationSchema =
                      nextOperationKey && nextCapabilityRegistryEntry
                        ? nextCapabilityRegistryEntry.operations[nextOperationKey]
                        : null;

                    onChangeSideEffect(index, {
                      ...sideEffect,
                      roomId: nextDevice?.roomId ?? selectedRoomId,
                      deviceId: nextDeviceId,
                      endpointId: nextEndpoint?.endpointId ?? "",
                      capabilityId: nextCapability?.capabilityId ?? "",
                      operation: nextOperation,
                      paramsText: buildDefaultParamsText(nextOperationSchema),
                    });
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
                    <option key={`${sideEffect.key}:${device.id}`} value={device.id}>
                      {device.name}
                    </option>
                  ))}
                </select>
              </FormGroup>

              <FormGroup
                label={t("scenes.endpointId")}
                htmlFor={`scene-side-effect-endpoint-${index}`}
              >
                <select
                  id={`scene-side-effect-endpoint-${index}`}
                  className={styles.select}
                  value={sideEffect.endpointId}
                  disabled={disabled || !sideEffect.deviceId.trim() || selectedEndpoints.length === 0}
                  onChange={(event) => {
                    const nextEndpointId = event.target.value;
                    const nextEndpoint = selectedEndpoints.find(
                      (endpoint) => endpoint.endpointId === nextEndpointId
                    );
                    const nextCapabilities = getSideEffectCapabilities(
                      nextEndpoint?.capabilities ?? [],
                      registryMap
                    );
                    const nextCapability = nextCapabilities[0];
                    const nextOperationOptions = getOperationOptions(nextCapability, registryMap);
                    const nextOperation = nextOperationOptions[0] ?? "";
                    const nextCapabilityRegistryEntry = getCapabilityRegistryEntry(
                      nextCapability,
                      registryMap
                    );
                    const nextOperationKey =
                      nextCapabilityRegistryEntry && nextOperation
                        ? resolveOperationKey(
                          nextCapabilityRegistryEntry.operations,
                          nextOperation
                        )
                        : null;
                    const nextOperationSchema =
                      nextOperationKey && nextCapabilityRegistryEntry
                        ? nextCapabilityRegistryEntry.operations[nextOperationKey]
                        : null;

                    onChangeSideEffect(index, {
                      ...sideEffect,
                      endpointId: nextEndpointId,
                      capabilityId: nextCapability?.capabilityId ?? "",
                      operation: nextOperation,
                      paramsText: buildDefaultParamsText(nextOperationSchema),
                    });
                  }}
                  required
                >
                  <option value="">{t("scenes.selectEndpoint")}</option>
                  {selectedEndpoints.map((endpoint) => (
                    <option
                      key={`${sideEffect.key}:${selectedDevice?.id ?? "device"}:${endpoint.endpointId}`}
                      value={endpoint.endpointId}
                    >
                      {endpoint.name || endpoint.endpointId}
                    </option>
                  ))}
                </select>
              </FormGroup>

              <FormGroup
                label={t("scenes.capability")}
                htmlFor={`scene-side-effect-capability-${index}`}
              >
                <select
                  id={`scene-side-effect-capability-${index}`}
                  className={styles.select}
                  value={sideEffect.capabilityId}
                  disabled={
                    disabled ||
                    !sideEffect.deviceId.trim() ||
                    !sideEffect.endpointId.trim() ||
                    selectedCapabilities.length === 0
                  }
                  onChange={(event) => {
                    const nextCapabilityId = event.target.value;
                    const nextCapability = selectedCapabilities.find(
                      (capability) => capability.capabilityId === nextCapabilityId
                    );
                    const nextOperationOptions = getOperationOptions(nextCapability, registryMap);
                    const nextOperation = nextOperationOptions[0] ?? "";
                    const nextCapabilityRegistryEntry = getCapabilityRegistryEntry(
                      nextCapability,
                      registryMap
                    );
                    const nextOperationKey =
                      nextCapabilityRegistryEntry && nextOperation
                        ? resolveOperationKey(
                          nextCapabilityRegistryEntry.operations,
                          nextOperation
                        )
                        : null;
                    const nextOperationSchema =
                      nextOperationKey && nextCapabilityRegistryEntry
                        ? nextCapabilityRegistryEntry.operations[nextOperationKey]
                        : null;

                    onChangeSideEffect(index, {
                      ...sideEffect,
                      capabilityId: nextCapabilityId,
                      operation: nextOperation,
                      paramsText: buildDefaultParamsText(nextOperationSchema),
                    });
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
                        key={`${sideEffect.key}:${selectedDevice?.id ?? "device"}:${selectedEndpoint?.endpointId ?? "endpoint"}:${capability.capabilityId}:${capability.capabilityVersion}`}
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

              <FormGroup
                label={t("scenes.timing")}
                htmlFor={`scene-side-effect-timing-${index}`}
              >
                <select
                  id={`scene-side-effect-timing-${index}`}
                  className={styles.select}
                  value={sideEffect.timing}
                  disabled={disabled}
                  onChange={(event) =>
                    onChangeSideEffect(index, {
                      ...sideEffect,
                      timing: event.target.value as SceneSideEffectTiming,
                    })
                  }
                >
                  {SIDE_EFFECT_TIMING_OPTIONS.map((timingOption) => (
                    <option key={timingOption} value={timingOption}>
                      {t(`scenes.sideEffectTimings.${timingOption}`, {
                        defaultValue: timingOption,
                      })}
                    </option>
                  ))}
                </select>
              </FormGroup>

              <FormGroup
                label={t("scenes.delayMs")}
                htmlFor={`scene-side-effect-delay-${index}`}
              >
                <Input
                  id={`scene-side-effect-delay-${index}`}
                  type="number"
                  value={sideEffect.delayMsText}
                  min={0}
                  step={1}
                  disabled={disabled}
                  onChange={(event) =>
                    onChangeSideEffect(index, {
                      ...sideEffect,
                      delayMsText: event.target.value,
                    })
                  }
                  required
                />
              </FormGroup>

              <FormGroup
                label={t("scenes.operation")}
                htmlFor={`scene-side-effect-operation-${index}`}
              >
                <select
                  id={`scene-side-effect-operation-${index}`}
                  className={styles.select}
                  value={sideEffect.operation}
                  disabled={
                    disabled ||
                    !sideEffect.deviceId.trim() ||
                    !sideEffect.endpointId.trim() ||
                    !sideEffect.capabilityId.trim() ||
                    operationOptionsWithCurrent.length === 0
                  }
                  onChange={(event) => {
                    const nextOperation = event.target.value;
                    const nextOperationKey =
                      selectedRegistryEntry && nextOperation
                        ? resolveOperationKey(selectedRegistryEntry.operations, nextOperation)
                        : null;
                    const nextOperationSchema =
                      nextOperationKey && selectedRegistryEntry
                        ? selectedRegistryEntry.operations[nextOperationKey]
                        : null;

                    onChangeSideEffect(index, {
                      ...sideEffect,
                      operation: nextOperation,
                      paramsText: buildDefaultParamsText(nextOperationSchema),
                    });
                  }}
                  required
                >
                  <option value="">{t("scenes.selectOperation")}</option>
                  {operationOptionsWithCurrent.map((operation) => (
                    <option key={`${sideEffect.key}:operation:${operation}`} value={operation}>
                      {localizeOperationLabel(operation)}
                    </option>
                  ))}
                </select>
              </FormGroup>

              {hasDuplicateTarget ? (
                <div className={styles.fieldHelp}>
                  {t("scenes.sideEffectTargetDuplicateHint")}
                </div>
              ) : null}

              {parameterFields.length > 0 ? (
                <div className={styles.stateFieldGrid}>
                  {parameterFields.map((field) => {
                    const fieldId = `scene-side-effect-param-${index}-${field.path || "value"}`;
                    const fieldValue = getSideEffectParamFieldValue(
                      sideEffect.paramsText,
                      field.path
                    );

                    const setFieldValue = (nextValue: unknown) =>
                      onChangeSideEffect(index, {
                        ...sideEffect,
                        paramsText: updateSideEffectParamFieldValue(
                          sideEffect.paramsText,
                          field.path,
                          nextValue
                        ),
                      });

                    const clearFieldValue = () =>
                      onChangeSideEffect(index, {
                        ...sideEffect,
                        paramsText: removeSideEffectParamFieldValue(
                          sideEffect.paramsText,
                          field.path
                        ),
                      });

                    if (field.type === "unsupported") {
                      const fieldLabel = localizeParamPathLabel(field.path);

                      return (
                        <div key={fieldId} className={styles.stateField}>
                          <div className={styles.fieldLabel}>{fieldLabel}</div>
                          <div className={styles.fieldHelp}>
                            {t("scenes.unsupportedSideEffectField", {
                              field: fieldLabel,
                            })}
                          </div>
                        </div>
                      );
                    }

                    const fieldLabel = localizeParamPathLabel(field.path);

                    return (
                      <FormGroup
                        key={fieldId}
                        label={fieldLabel}
                        htmlFor={fieldId}
                        required={field.required}
                      >
                        {field.type === "enum" ? (
                          <select
                            id={fieldId}
                            className={styles.select}
                            value={fieldValue === undefined ? "" : String(fieldValue)}
                            disabled={disabled}
                            onChange={(event) => {
                              if (event.target.value === "") {
                                clearFieldValue();
                                return;
                              }

                              setFieldValue(event.target.value);
                            }}
                          >
                            <option value="">-</option>
                            {field.enumValues.map((enumValue) => (
                              <option key={`${fieldId}:${enumValue}`} value={enumValue}>
                                {enumValue}
                              </option>
                            ))}
                          </select>
                        ) : field.type === "boolean" ? (
                          <div className={styles.booleanToggle}>
                            <BooleanSwitch
                              id={fieldId}
                              checked={Boolean(fieldValue)}
                              disabled={disabled}
                              label={
                                Boolean(fieldValue)
                                  ? t("scenes.stateOn")
                                  : t("scenes.stateOff")
                              }
                              onChange={(nextValue) => setFieldValue(nextValue)}
                            />
                            {!field.required ? (
                              <button
                                type="button"
                                className={styles.clearFieldButton}
                                onClick={(event) => {
                                  event.preventDefault();
                                  clearFieldValue();
                                }}
                                disabled={disabled}
                              >
                                {t("scenes.clear")}
                              </button>
                            ) : null}
                          </div>
                        ) : (field.type === "number" || field.type === "integer") &&
                          field.min !== null &&
                          field.max !== null ? (
                          <div className={styles.sliderField}>
                            <NumericSliderField
                              id={fieldId}
                              inputValue={fieldValue === undefined ? "" : String(fieldValue)}
                              sliderValue={
                                typeof fieldValue === "number" && Number.isFinite(fieldValue)
                                  ? fieldValue
                                  : field.min
                              }
                              min={field.min}
                              max={field.max}
                              step={field.step ?? (field.type === "integer" ? 1 : undefined)}
                              disabled={disabled}
                              onInputChange={(nextRawValue) => {
                                if (nextRawValue.trim() === "") {
                                  clearFieldValue();
                                  return;
                                }

                                const parsed = Number(nextRawValue);
                                if (!Number.isFinite(parsed)) {
                                  return;
                                }

                                setFieldValue(
                                  field.type === "integer"
                                    ? Math.trunc(parsed)
                                    : parsed
                                );
                              }}
                              onSliderChange={(nextValue) => {
                                setFieldValue(
                                  field.type === "integer"
                                    ? Math.trunc(nextValue)
                                    : nextValue
                                );
                              }}
                            />
                          </div>
                        ) : (
                          <Input
                            id={fieldId}
                            type={field.type === "number" || field.type === "integer" ? "number" : "text"}
                            value={fieldValue === undefined ? "" : String(fieldValue)}
                            min={field.min ?? undefined}
                            max={field.max ?? undefined}
                            step={
                              field.type === "number" || field.type === "integer"
                                ? (field.step ?? (field.type === "integer" ? 1 : undefined))
                                : undefined
                            }
                            disabled={disabled}
                            onChange={(event) => {
                              const nextRawValue = event.target.value;

                              if (nextRawValue.trim() === "") {
                                clearFieldValue();
                                return;
                              }

                              if (field.type === "number" || field.type === "integer") {
                                const parsed = Number(nextRawValue);
                                if (!Number.isFinite(parsed)) {
                                  return;
                                }

                                setFieldValue(
                                  field.type === "integer"
                                    ? Math.trunc(parsed)
                                    : parsed
                                );
                                return;
                              }

                              setFieldValue(nextRawValue);
                            }}
                          />
                        )}
                      </FormGroup>
                    );
                  })}
                </div>
              ) : (
                <div className={styles.stateFieldFallback}>
                  {t("scenes.noSideEffectSchemaFields")}
                </div>
              )}

              {hasReadOnlyParameterField ? (
                <div className={styles.fieldHelp}>{t("scenes.readOnlyFieldsSkipped")}</div>
              ) : null}
            </div>
          </div>
        );
      })}

      <div className={styles.footer}>
        <Button type="button" size="sm" variant="secondary" onClick={onAddSideEffect} disabled={disabled}>
          {t("scenes.addSideEffect")}
        </Button>
      </div>
    </div>
  );
}
