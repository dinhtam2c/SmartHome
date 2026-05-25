import { useState } from "react";
import { useTranslation } from "react-i18next";
import { Button } from "@/shared/ui/Button";
import { FormGroup } from "@/shared/ui/FormGroup";
import {
  CAPABILITY_LABEL_KEYS,
  getCapabilityBooleanLabels,
  getCapabilityDisplayLabel,
  getValueByPath,
  localizeCapabilityStatePath,
  type CapabilityRegistryMap,
  type SchemaField,
} from "@/features/capabilities";
import { CapabilityBooleanControl } from "@/features/capabilities/components/CapabilityBooleanControl";
import type { HomeRoomOverviewDto } from "@/features/homes";
import type { BuilderCapabilityDto, BuilderDeviceDto } from "@/features/capability-builder";
import {
  getCapabilityRegistryEntry,
  resolveCapabilityDeviceSelection,
} from "@/features/capability-builder";
import { toSchemaFields } from "@/features/capability-builder";
import {
  BASIC_OPERATORS,
  getSupportedOperators,
  stringifyConditionValue,
  type AutomationConditionDraft,
} from "../../services/automationFormService";
import { getConditionCapabilities } from "../../services/automationSelectionService";
import type { AutomationConditionLogic, AutomationConditionOperator } from "../../types/automationTypes";
import styles from "../AutomationEditors.module.css";

type Props = {
  conditions: AutomationConditionDraft[];
  rooms?: HomeRoomOverviewDto[];
  availableDevices?: BuilderDeviceDto[];
  availableDevicesByRoom?: Record<string, BuilderDeviceDto[]>;
  registryMap?: CapabilityRegistryMap;
  disabled?: boolean;
  collapseOnOpen?: boolean;
  conditionLogic: AutomationConditionLogic;
  onConditionLogicChange: (value: AutomationConditionLogic) => void;
  onChangeCondition: (index: number, condition: AutomationConditionDraft) => void;
  onAddCondition: () => void;
  onRemoveCondition: (index: number) => void;
};

function getConditionFields(
  capability: BuilderCapabilityDto | undefined,
  registryMap: CapabilityRegistryMap | undefined
) {
  const registryEntry = getCapabilityRegistryEntry(capability, registryMap);
  if (!registryEntry) {
    return [];
  }

  return toSchemaFields(registryEntry.stateSchema)
    .filter((field) => field.type !== "unsupported" && field.path.trim() !== "");
}

function getDefaultValueText(
  field: SchemaField | undefined,
  capability: BuilderCapabilityDto | undefined
) {
  const currentValue = field ? getValueByPath(capability?.state, field.path) : undefined;

  if (currentValue !== undefined && currentValue !== null) {
    return stringifyConditionValue(currentValue);
  }

  if (field?.type === "boolean") {
    return "true";
  }

  if (field?.type === "enum") {
    return field.enumValues[0] ?? "";
  }

  if (field?.type === "number" || field?.type === "integer") {
    return String(field.min ?? 0);
  }

  return "";
}

function normalizeOperator(
  operator: AutomationConditionOperator,
  field: SchemaField | undefined
) {
  const supportedOperators = getSupportedOperators(field);
  return supportedOperators.includes(operator) ? operator : supportedOperators[0];
}

function formatConditionValue(
  condition: AutomationConditionDraft,
  field: SchemaField | undefined,
  t: ReturnType<typeof useTranslation<"automations">>["t"],
  tScenes: ReturnType<typeof useTranslation<"scenes">>["t"]
) {
  if (condition.operator === "Between") {
    const min = condition.betweenMinText.trim() || t("notAvailable");
    const max = condition.betweenMaxText.trim() || t("notAvailable");
    return `${min} - ${max}`;
  }

  if (field?.type === "boolean") {
    return condition.compareValueText === "true"
      ? tScenes("scenes.stateOn")
      : tScenes("scenes.stateOff");
  }

  return condition.compareValueText.trim() || t("notAvailable");
}

export function AutomationConditionsEditor({
  conditions,
  rooms = [],
  availableDevices = [],
  availableDevicesByRoom = {},
  registryMap,
  disabled = false,
  collapseOnOpen = false,
  conditionLogic,
  onConditionLogicChange,
  onChangeCondition,
  onAddCondition,
  onRemoveCondition,
}: Props) {
  const { t } = useTranslation("automations");
  const { t: tScenes } = useTranslation("scenes");
  const [collapsedKeys, setCollapsedKeys] = useState<Set<string>>(
    () =>
      collapseOnOpen
        ? new Set(conditions.map((condition) => condition.key))
        : new Set()
  );

  const localizeFieldLabel = (path: string) =>
    localizeCapabilityStatePath(tScenes, path, CAPABILITY_LABEL_KEYS.scene);

  const toggleCollapsed = (key: string) => {
    setCollapsedKeys((current) => {
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
        <div className={styles.sectionTitle}>{t("automations.when")}</div>
      </div>

      <div className={styles.segmentedRow}>
        <div id="automation-condition-logic" className={styles.segmented}>
          {(["All", "Any"] as AutomationConditionLogic[]).map((logic) => (
            <button
              key={`automation-logic-${logic}`}
              type="button"
              className={`${styles.segmentButton} ${conditionLogic === logic ? styles.segmentButtonActive : ""}`}
              disabled={disabled}
              onClick={() => onConditionLogicChange(logic)}
            >
              {t(`automations.conditionLogicOptions.${logic}`)}
            </button>
          ))}
        </div>
      </div>

      {conditions.length === 0 ? (
        <div className={styles.hint}>{t("automations.noConditions")}</div>
      ) : null}

      {conditions.map((condition, index) => {
        const {
          selectedRoomId,
          roomDevices,
          selectedDevice,
          selectedEndpoints,
          selectedCapabilities,
          selectedCapability,
        } = resolveCapabilityDeviceSelection({
          roomId: condition.roomId,
          deviceId: condition.deviceId,
          endpointId: condition.endpointId,
          capabilityId: condition.capabilityId,
          availableDevices,
          availableDevicesByRoom,
          registryMap,
          filterCapabilities: getConditionCapabilities,
        });

        const conditionFields = getConditionFields(selectedCapability, registryMap);
        const selectedField = conditionFields.find(
          (field) => field.path === condition.fieldPath
        );
        const supportedOperators = getSupportedOperators(selectedField);
        const registryEntry = getCapabilityRegistryEntry(selectedCapability, registryMap);
        const capabilityLabel = selectedCapability
          ? getCapabilityDisplayLabel(
            tScenes,
            selectedCapability.capabilityId,
            typeof registryEntry?.metadata?.defaultName === "string"
              ? registryEntry.metadata.defaultName
              : null
          )
          : tScenes("scenes.unselectedCapability");
        const isCollapsed = collapsedKeys.has(condition.key);
        const fieldLabel = condition.fieldPath
          ? localizeFieldLabel(condition.fieldPath)
          : t("automations.selectField");
        const conditionSummary = `${fieldLabel} ${t(
          `automations.operators.${condition.operator}`
        )} ${formatConditionValue(condition, selectedField, t, tScenes)}`;
        const cardClassName = [
          styles.actionCard,
          isCollapsed ? styles.actionCardCollapsed : null,
        ]
          .filter(Boolean)
          .join(" ");

        const updateCondition = (patch: Partial<AutomationConditionDraft>) => {
          onChangeCondition(index, {
            ...condition,
            ...patch,
          });
        };

        const applySelectedCapability = (
          capability: BuilderCapabilityDto | undefined
        ) => {
          const fields = getConditionFields(capability, registryMap);
          const nextField = fields[0];
          return {
            capabilityId: capability?.capabilityId ?? "",
            fieldPath: nextField?.path ?? "",
            operator: normalizeOperator(condition.operator, nextField),
            compareValueText: getDefaultValueText(nextField, capability),
            betweenMinText: "",
            betweenMaxText: "",
          };
        };

        return (
          <div key={condition.key} className={cardClassName}>
            <div className={styles.actionHeader}>
              <div className={styles.actionHeaderCopy}>
                <div className={styles.actionTitleRow}>
                  <div className={styles.deviceName}>
                    {selectedDevice?.name || tScenes("scenes.unselectedDevice")}
                  </div>
                  <span className={styles.actionIndex}>
                    {t("automations.conditionItem", { index: index + 1 })}
                  </span>
                </div>
                <div className={styles.actionSummary}>
                  <span className={styles.summaryText}>{capabilityLabel}</span>
                  <span className={`${styles.summaryText} ${styles.summaryTextStrong}`}>
                    {conditionSummary}
                  </span>
                </div>
              </div>

              <div className={styles.actionHeaderActions}>
                <Button
                  type="button"
                  size="sm"
                  variant="secondary"
                  className={styles.headerButton}
                  aria-expanded={!isCollapsed}
                  onClick={() => toggleCollapsed(condition.key)}
                >
                  {isCollapsed ? tScenes("scenes.expandItem") : tScenes("scenes.collapseItem")}
                </Button>
                <Button
                  type="button"
                  size="sm"
                  variant="secondary"
                  className={`${styles.headerButton} ${styles.deleteButton}`}
                  disabled={disabled}
                  onClick={() => onRemoveCondition(index)}
                >
                  {tScenes("scenes.deleteShort")}
                </Button>
              </div>
            </div>

            <fieldset
              className={`${styles.fields} ${isCollapsed ? styles.fieldsCollapsed : ""}`}
              disabled={disabled || isCollapsed}
            >
              <FormGroup
                label={tScenes("scenes.room")}
                htmlFor={`automation-condition-room-${condition.key}`}
                required={false}
              >
                <select
                  id={`automation-condition-room-${condition.key}`}
                  className={styles.select}
                  value={selectedRoomId}
                  onChange={(event) =>
                    updateCondition({
                      roomId: event.target.value,
                      deviceId: "",
                      endpointId: "",
                      capabilityId: "",
                      fieldPath: "",
                      compareValueText: "",
                    })
                  }
                >
                  <option value="">{tScenes("scenes.allRooms")}</option>
                  {rooms.map((room) => (
                    <option key={`${condition.key}:room:${room.id}`} value={room.id}>
                      {room.name}
                    </option>
                  ))}
                </select>
              </FormGroup>

              <FormGroup
                label={tScenes("scenes.deviceName")}
                htmlFor={`automation-condition-device-${condition.key}`}
              >
                <select
                  id={`automation-condition-device-${condition.key}`}
                  className={styles.select}
                  value={condition.deviceId}
                  disabled={disabled || roomDevices.length === 0}
                  onChange={(event) => {
                    const nextDevice = roomDevices.find(
                      (device) => device.id === event.target.value
                    );
                    const nextEndpoint = nextDevice?.endpoints[0];
                    const nextCapabilities = getConditionCapabilities(
                      nextEndpoint?.capabilities ?? [],
                      registryMap
                    );
                    const nextCapability = nextCapabilities[0];

                    updateCondition({
                      roomId: nextDevice?.roomId ?? selectedRoomId,
                      deviceId: event.target.value,
                      endpointId: nextEndpoint?.endpointId ?? "",
                      ...applySelectedCapability(nextCapability),
                    });
                  }}
                  required
                >
                  <option value="">{tScenes("scenes.selectDevice")}</option>
                  {roomDevices.length === 0 ? (
                    <option value="" disabled>
                      {tScenes("scenes.noDevicesAvailable")}
                    </option>
                  ) : null}
                  {roomDevices.map((device) => (
                    <option key={`${condition.key}:device:${device.id}`} value={device.id}>
                      {device.name}
                    </option>
                  ))}
                </select>
              </FormGroup>

              <FormGroup
                label={tScenes("scenes.endpointId")}
                htmlFor={`automation-condition-endpoint-${condition.key}`}
              >
                <select
                  id={`automation-condition-endpoint-${condition.key}`}
                  className={styles.select}
                  value={condition.endpointId}
                  disabled={disabled || !condition.deviceId || selectedEndpoints.length === 0}
                  onChange={(event) => {
                    const nextEndpoint = selectedEndpoints.find(
                      (endpoint) => endpoint.endpointId === event.target.value
                    );
                    const nextCapabilities = getConditionCapabilities(
                      nextEndpoint?.capabilities ?? [],
                      registryMap
                    );
                    const nextCapability = nextCapabilities[0];

                    updateCondition({
                      endpointId: event.target.value,
                      ...applySelectedCapability(nextCapability),
                    });
                  }}
                  required
                >
                  <option value="">{tScenes("scenes.selectEndpoint")}</option>
                  {selectedEndpoints.map((endpoint) => (
                    <option
                      key={`${condition.key}:endpoint:${endpoint.endpointId}`}
                      value={endpoint.endpointId}
                    >
                      {endpoint.name || endpoint.endpointId}
                    </option>
                  ))}
                </select>
              </FormGroup>

              <FormGroup
                label={tScenes("scenes.capability")}
                htmlFor={`automation-condition-capability-${condition.key}`}
              >
                <select
                  id={`automation-condition-capability-${condition.key}`}
                  className={styles.select}
                  value={condition.capabilityId}
                  disabled={
                    disabled ||
                    !condition.endpointId ||
                    selectedCapabilities.length === 0
                  }
                  onChange={(event) => {
                    const nextCapability = selectedCapabilities.find(
                      (capability) => capability.capabilityId === event.target.value
                    );

                    updateCondition(applySelectedCapability(nextCapability));
                  }}
                  required
                >
                  <option value="">{tScenes("scenes.selectCapability")}</option>
                  {selectedCapabilities.map((capability) => {
                    const entry = getCapabilityRegistryEntry(capability, registryMap);

                    return (
                      <option
                        key={`${condition.key}:capability:${capability.capabilityId}:${capability.capabilityVersion}`}
                        value={capability.capabilityId}
                      >
                        {getCapabilityDisplayLabel(
                          tScenes,
                          capability.capabilityId,
                          typeof entry?.metadata?.defaultName === "string"
                            ? entry.metadata.defaultName
                            : null
                        )}
                      </option>
                    );
                  })}
                </select>
              </FormGroup>

              <FormGroup
                label={t("automations.field")}
                htmlFor={`automation-condition-field-${condition.key}`}
              >
                <select
                  id={`automation-condition-field-${condition.key}`}
                  className={styles.select}
                  value={condition.fieldPath}
                  disabled={disabled || conditionFields.length === 0}
                  onChange={(event) => {
                    const nextField = conditionFields.find(
                      (field) => field.path === event.target.value
                    );
                    updateCondition({
                      fieldPath: event.target.value,
                      operator: normalizeOperator(condition.operator, nextField),
                      compareValueText: getDefaultValueText(nextField, selectedCapability),
                      betweenMinText: "",
                      betweenMaxText: "",
                    });
                  }}
                  required
                >
                  <option value="">{t("automations.selectField")}</option>
                  {conditionFields.map((field) => (
                    <option key={`${condition.key}:field:${field.path}`} value={field.path}>
                      {localizeFieldLabel(field.path)}
                    </option>
                  ))}
                </select>
              </FormGroup>

              <FormGroup
                label={t("automations.operator")}
                htmlFor={`automation-condition-operator-${condition.key}`}
              >
                <select
                  id={`automation-condition-operator-${condition.key}`}
                  className={styles.select}
                  value={condition.operator}
                  onChange={(event) =>
                    updateCondition({
                      operator: event.target.value as AutomationConditionOperator,
                    })
                  }
                >
                  {(selectedField ? supportedOperators : BASIC_OPERATORS).map((operator) => (
                    <option key={`${condition.key}:operator:${operator}`} value={operator}>
                      {t(`automations.operators.${operator}`)}
                    </option>
                  ))}
                </select>
              </FormGroup>

              <ConditionValueEditor
                condition={condition}
                field={selectedField}
                disabled={disabled}
                onChange={updateCondition}
              />
            </fieldset>
          </div>
        );
      })}

      <div className={styles.buttonRow}>
        <Button
          type="button"
          size="sm"
          variant="secondary"
          onClick={onAddCondition}
          disabled={disabled}
        >
          {t("automations.addCondition")}
        </Button>
      </div>
    </div>
  );
}

type ConditionValueEditorProps = {
  condition: AutomationConditionDraft;
  field: SchemaField | undefined;
  disabled: boolean;
  onChange: (patch: Partial<AutomationConditionDraft>) => void;
};

function ConditionValueEditor({
  condition,
  field,
  disabled,
  onChange,
}: ConditionValueEditorProps) {
  const { t } = useTranslation("automations");
  const { t: tScenes } = useTranslation("scenes");
  const isBetween = condition.operator === "Between";

  if (isBetween) {
    return (
      <>
        <FormGroup
          label={t("automations.minValue")}
          htmlFor={`automation-condition-min-${condition.key}`}
        >
          <input
            id={`automation-condition-min-${condition.key}`}
            className={styles.textInput}
            type="number"
            value={condition.betweenMinText}
            min={field?.min ?? undefined}
            max={field?.max ?? undefined}
            step={field?.step ?? (field?.type === "integer" ? 1 : undefined)}
            disabled={disabled}
            onChange={(event) => onChange({ betweenMinText: event.target.value })}
            required
          />
        </FormGroup>
        <FormGroup
          label={t("automations.maxValue")}
          htmlFor={`automation-condition-max-${condition.key}`}
        >
          <input
            id={`automation-condition-max-${condition.key}`}
            className={styles.textInput}
            type="number"
            value={condition.betweenMaxText}
            min={field?.min ?? undefined}
            max={field?.max ?? undefined}
            step={field?.step ?? (field?.type === "integer" ? 1 : undefined)}
            disabled={disabled}
            onChange={(event) => onChange({ betweenMaxText: event.target.value })}
            required
          />
        </FormGroup>
      </>
    );
  }

  if (field?.type === "boolean") {
    const checked = condition.compareValueText === "true";
    const booleanLabels = getCapabilityBooleanLabels(tScenes, CAPABILITY_LABEL_KEYS.scene);
    const handleChange = (nextChecked: boolean) =>
      onChange({ compareValueText: nextChecked ? "true" : "false" });

    return (
      <div className={styles.fullRow}>
        <div className={styles.toggleRow}>
          <CapabilityBooleanControl
            capabilityId={condition.capabilityId}
            id={`automation-condition-value-${condition.key}`}
            checked={checked}
            disabled={disabled}
            labels={booleanLabels}
            onChange={handleChange}
          />
        </div>
      </div>
    );
  }

  if (field?.type === "enum") {
    return (
      <FormGroup
        label={t("automations.compareValue")}
        htmlFor={`automation-condition-value-${condition.key}`}
      >
        <select
          id={`automation-condition-value-${condition.key}`}
          className={styles.select}
          value={condition.compareValueText}
          disabled={disabled}
          onChange={(event) => onChange({ compareValueText: event.target.value })}
          required
        >
          {field.enumValues.map((value) => (
            <option key={`${condition.key}:enum:${value}`} value={value}>
              {value}
            </option>
          ))}
        </select>
      </FormGroup>
    );
  }

  return (
    <FormGroup
      label={t("automations.compareValue")}
      htmlFor={`automation-condition-value-${condition.key}`}
    >
      <input
        id={`automation-condition-value-${condition.key}`}
        className={styles.textInput}
        type={field?.type === "number" || field?.type === "integer" ? "number" : "text"}
        value={condition.compareValueText}
        min={field?.min ?? undefined}
        max={field?.max ?? undefined}
        step={field?.step ?? (field?.type === "integer" ? 1 : undefined)}
        disabled={disabled}
        onChange={(event) => onChange({ compareValueText: event.target.value })}
        required
      />
    </FormGroup>
  );
}
