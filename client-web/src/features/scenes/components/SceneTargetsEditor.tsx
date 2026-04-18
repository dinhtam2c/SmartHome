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
import { BooleanSwitch } from "@/components/BooleanSwitch";
import { NumericSliderField } from "@/components/NumericSliderField";
import {
  getDesiredStateFieldValue,
  removeJsonObjectFields,
  removeDesiredStateFieldValue,
  updateDesiredStateFieldValue,
  type SceneTargetDraft,
} from "../sceneFormUtils";
import styles from "./SceneTargetsEditor.module.css";
import { isPlainObject, toSchemaFields } from "./schemaFieldUtils";
import { useTranslation } from "react-i18next";

type Props = {
  targets: SceneTargetDraft[];
  rooms?: HomeRoomOverviewDto[];
  availableDevices?: HomeSceneBuilderDeviceDto[];
  availableDevicesByRoom?: Record<string, HomeSceneBuilderDeviceDto[]>;
  registryMap?: CapabilityRegistryMap;
  disabled?: boolean;
  onChangeTarget: (index: number, action: SceneTargetDraft) => void;
  onAddTarget: () => void;
  onRemoveTarget: (index: number) => void;
};

function resolveDesiredStateFromCapability(
  capability: HomeSceneBuilderCapabilityDto | null | undefined,
  readOnlyPaths: string[] = []
) {
  if (!capability?.state || !isPlainObject(capability.state)) {
    return "{}";
  }

  const desiredStateText = JSON.stringify(capability.state, null, 2);
  return removeJsonObjectFields(desiredStateText, readOnlyPaths);
}

function getReadOnlyStatePaths(stateSchema: Record<string, unknown>) {
  return toSchemaFields(stateSchema)
    .filter((field) => field.readOnly)
    .map((field) => field.path.trim())
    .filter((path) => path !== "");
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

function getControlCapabilities(
  capabilities: HomeSceneBuilderCapabilityDto[],
  registryMap: CapabilityRegistryMap | undefined
) {
  if (!registryMap) {
    return [];
  }

  return capabilities.filter((capability) => {
    const registryEntry = getCapabilityRegistryEntry(capability, registryMap);
    return registryEntry?.role === "Control";
  });
}

export function SceneTargetsEditor({
  targets,
  rooms = [],
  availableDevices = [],
  availableDevicesByRoom = {},
  registryMap,
  disabled = false,
  onChangeTarget,
  onAddTarget,
  onRemoveTarget,
}: Props) {
  const { t } = useTranslation("homes");

  const localizeStatePathLabel = (path: string) => {
    const normalizedPath = path.trim();

    if (!normalizedPath) {
      return t("scenes.stateValue", { defaultValue: "Value" });
    }

    const segments = normalizedPath
      .split(".")
      .map((segment) => segment.trim())
      .filter((segment) => segment !== "");

    if (segments.length === 0) {
      return t("scenes.stateValue", { defaultValue: "Value" });
    }

    const leafKey = segments[segments.length - 1];
    const localizedLeaf = t(`scenes.stateKeyLabels.${leafKey}`, {
      defaultValue: leafKey,
    });

    if (segments.length === 1) {
      return localizedLeaf;
    }

    return `${segments.slice(0, -1).join(".")}.${localizedLeaf}`;
  };

  return (
    <div className={styles.stack}>
      <div className={styles.sectionHeader}>
        <div className={styles.sectionTitle}>{t("scenes.targets")}</div>
      </div>

      {targets.length === 0 ? (
        <div className={styles.fieldHelp}>{t("scenes.noTargets")}</div>
      ) : null}

      {targets.map((action, index) => {
        const selectedDeviceFromAll = availableDevices.find(
          (device) => device.id === action.deviceId
        );
        const selectedRoomId =
          action.roomId.trim() || selectedDeviceFromAll?.roomId || "";

        const roomDevices = selectedRoomId
          ? (availableDevicesByRoom[selectedRoomId] ??
            availableDevices.filter((device) => device.roomId === selectedRoomId))
          : availableDevices;

        const selectedDevice =
          roomDevices.find((device) => device.id === action.deviceId) ??
          selectedDeviceFromAll;

        const selectedEndpoints = selectedDevice?.endpoints ?? [];
        const selectedEndpoint = selectedEndpoints.find(
          (endpoint) => endpoint.endpointId === action.endpointId
        );
        const selectedCapabilities = getControlCapabilities(
          selectedEndpoint?.capabilities ?? [],
          registryMap
        );
        const selectedCapability = selectedCapabilities.find(
          (capability) => capability.capabilityId === action.capabilityId
        );

        const registryEntry = getCapabilityRegistryEntry(selectedCapability, registryMap);

        const schemaFields = registryEntry
          ? toSchemaFields(registryEntry.stateSchema)
          : [];
        const writableSchemaFields = schemaFields.filter((field) => !field.readOnly);
        const readOnlyStatePaths = schemaFields
          .filter((field) => field.readOnly)
          .map((field) => field.path.trim())
          .filter((path) => path !== "");

        return (
          <div key={action.key} className={styles.actionCard}>
            <div className={styles.actionHeader}>
              <div className={styles.actionTitle}>
                {t("scenes.targetItem", { index: index + 1 })}
              </div>
              <Button
                type="button"
                size="sm"
                variant="danger"
                onClick={() => onRemoveTarget(index)}
                disabled={disabled}
              >
                {t("scenes.removeTarget")}
              </Button>
            </div>

            <div className={styles.fields}>
              <FormGroup
                label={t("scenes.room")}
                htmlFor={`scene-action-room-${index}`}
                required={false}
              >
                <select
                  id={`scene-action-room-${index}`}
                  className={styles.select}
                  value={selectedRoomId}
                  disabled={disabled}
                  onChange={(event) => {
                    const nextRoomId = event.target.value;

                    onChangeTarget(index, {
                      ...action,
                      roomId: nextRoomId,
                      deviceId: "",
                      endpointId: "",
                      capabilityId: "",
                      desiredStateText: "{}",
                    });
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
                htmlFor={`scene-action-device-${index}`}
              >
                <select
                  id={`scene-action-device-${index}`}
                  className={styles.select}
                  value={action.deviceId}
                  disabled={disabled || roomDevices.length === 0}
                  onChange={(event) => {
                    const nextDeviceId = event.target.value;
                    const nextDevice = roomDevices.find((item) => item.id === nextDeviceId);
                    const nextEndpoint = nextDevice?.endpoints[0];
                    const nextCapabilities = getControlCapabilities(
                      nextEndpoint?.capabilities ?? [],
                      registryMap
                    );
                    const nextCapability = nextCapabilities[0];
                    const nextCapabilityEntry = getCapabilityRegistryEntry(
                      nextCapability,
                      registryMap
                    );
                    const nextReadOnlyStatePaths = nextCapabilityEntry
                      ? getReadOnlyStatePaths(nextCapabilityEntry.stateSchema)
                      : [];

                    onChangeTarget(index, {
                      ...action,
                      roomId: nextDevice?.roomId ?? selectedRoomId,
                      deviceId: nextDeviceId,
                      endpointId: nextEndpoint?.endpointId ?? "",
                      capabilityId: nextCapability?.capabilityId ?? "",
                      desiredStateText: resolveDesiredStateFromCapability(
                        nextCapability,
                        nextReadOnlyStatePaths
                      ),
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
                    <option key={`${action.key}:${device.id}`} value={device.id}>
                      {device.name}
                    </option>
                  ))}
                </select>
              </FormGroup>

              <FormGroup
                label={t("scenes.endpointId")}
                htmlFor={`scene-action-endpoint-${index}`}
              >
                <select
                  id={`scene-action-endpoint-${index}`}
                  className={styles.select}
                  value={action.endpointId}
                  disabled={disabled || !action.deviceId.trim() || selectedEndpoints.length === 0}
                  onChange={(event) => {
                    const nextEndpointId = event.target.value;
                    const nextEndpoint = selectedEndpoints.find(
                      (endpoint) => endpoint.endpointId === nextEndpointId
                    );
                    const nextCapabilities = getControlCapabilities(
                      nextEndpoint?.capabilities ?? [],
                      registryMap
                    );
                    const nextCapability = nextCapabilities[0];
                    const nextCapabilityEntry = getCapabilityRegistryEntry(
                      nextCapability,
                      registryMap
                    );
                    const nextReadOnlyStatePaths = nextCapabilityEntry
                      ? getReadOnlyStatePaths(nextCapabilityEntry.stateSchema)
                      : [];

                    onChangeTarget(index, {
                      ...action,
                      endpointId: nextEndpointId,
                      capabilityId: nextCapability?.capabilityId ?? "",
                      desiredStateText: resolveDesiredStateFromCapability(
                        nextCapability,
                        nextReadOnlyStatePaths
                      ),
                    });
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
                htmlFor={`scene-action-capability-${index}`}
              >
                <select
                  id={`scene-action-capability-${index}`}
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
                    const nextCapabilityEntry = getCapabilityRegistryEntry(
                      nextCapability,
                      registryMap
                    );
                    const nextReadOnlyStatePaths = nextCapabilityEntry
                      ? getReadOnlyStatePaths(nextCapabilityEntry.stateSchema)
                      : [];

                    onChangeTarget(index, {
                      ...action,
                      capabilityId: nextCapabilityId,
                      desiredStateText: resolveDesiredStateFromCapability(
                        nextCapability,
                        nextReadOnlyStatePaths
                      ),
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

              {writableSchemaFields.length > 0 ? (
                <div className={styles.stateFieldGrid}>
                  {writableSchemaFields.map((field) => {
                    const fieldId = `scene-action-state-${index}-${field.path || "value"}`;
                    const fieldValue = getDesiredStateFieldValue(
                      action.desiredStateText,
                      field.path
                    );

                    const setFieldValue = (nextValue: unknown) =>
                      onChangeTarget(index, {
                        ...action,
                        desiredStateText: updateDesiredStateFieldValue(
                          action.desiredStateText,
                          field.path,
                          nextValue
                        ),
                      });

                    const clearFieldValue = () =>
                      onChangeTarget(index, {
                        ...action,
                        desiredStateText: removeDesiredStateFieldValue(
                          action.desiredStateText,
                          field.path
                        ),
                      });

                    if (field.type === "unsupported") {
                      const fieldLabel = localizeStatePathLabel(field.path);

                      return (
                        <div key={fieldId} className={styles.stateField}>
                          <div className={styles.fieldLabel}>{fieldLabel}</div>
                          <div className={styles.fieldHelp}>
                            {t("scenes.unsupportedStateField", {
                              field: fieldLabel,
                            })}
                          </div>
                        </div>
                      );
                    }

                    const fieldLabel = localizeStatePathLabel(field.path);

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
                <div className={styles.stateFieldFallback}>{t("scenes.noSchemaFields")}</div>
              )}

              {readOnlyStatePaths.length > 0 ? (
                <div className={styles.fieldHelp}>{t("scenes.readOnlyFieldsSkipped")}</div>
              ) : null}
            </div>
          </div>
        );
      })}

      <div className={styles.footer}>
        <Button
          type="button"
          size="sm"
          variant="secondary"
          onClick={onAddTarget}
          disabled={disabled}
        >
          {t("scenes.addTarget")}
        </Button>
      </div>
    </div>
  );
}
