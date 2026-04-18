import { useState, type Dispatch, type SetStateAction } from "react";
import { Button } from "@/components/Button";
import { BooleanSwitch } from "@/components/BooleanSwitch";
import { Cell } from "@/components/Cell";
import { CellGrid } from "@/components/CellGrid";
import { Input } from "@/components/Input";
import { NumericSliderField } from "@/components/NumericSliderField";
import {
  composeValueByPath,
  getCapabilityDisplayLabel,
  getCapabilityDisplayOrder,
  getCapabilityPrimaryStateValue,
  getCapabilityUnit,
  parsePrimaryOperationReference,
} from "@/features/capabilities";
import type { DeviceCapabilityDto } from "../devices.types";
import {
  type OperationRule,
  defaultValueForRule,
  getDefaultNumericValue,
  getNumericStepForRule,
  getOperationRule,
  getOperationRuleForPath,
  getOperationSchema,
  isNumericSliderRule,
  normalizeRuleType,
  resolveSupportedOperation,
  validateCommandValue,
} from "../deviceCapabilityUtils";
import styles from "@/features/shared/featurePage.module.css";
import pageStyles from "../DeviceDetailPage.module.css";
import { useTranslation } from "react-i18next";

type CapabilityGroup = {
  endpointKey: string;
  endpointLabel: string;
  capabilities: DeviceCapabilityDto[];
};

type Props = {
  capabilityGroups: CapabilityGroup[];
  canControlDevice: boolean;
  quickToggleBusyCapabilityId: string | null;
  optimisticToggleValues: Record<string, boolean | undefined>;
  inlineCommandValues: Record<string, string>;
  setInlineCommandValues: Dispatch<SetStateAction<Record<string, string>>>;
  onBooleanToggleSend: (
    capabilityId: string,
    capabilityKey: string,
    endpointId: string,
    operation: string,
    nextValue: boolean,
    previousValue: boolean | null,
    valuePath?: string | null
  ) => void;
  onScheduleInlineCommandSend: (
    capabilityId: string,
    capabilityKey: string,
    endpointId: string,
    operation: string,
    rule: OperationRule | null,
    rawValue: string,
    valuePath?: string | null,
    delayMs?: number
  ) => void;
  onInlineCommandSend: (
    capabilityId: string,
    capabilityKey: string,
    endpointId: string,
    operation: string,
    rule: OperationRule | null,
    rawValue: string,
    valuePath?: string | null,
    valueOverride?: unknown
  ) => void;
  quickActionError: string | null;
  formatCapabilityState: (state: unknown) => string;
};

type AdvancedOperationField = {
  path: string;
  label: string;
  required: boolean;
  rule: OperationRule | null;
  unsupported: boolean;
};

function getDisplayedToggleValue(
  optimisticToggleValues: Record<string, boolean | undefined>,
  capabilityKey: string,
  fallbackValue: boolean | null
) {
  const optimisticValue = optimisticToggleValues[capabilityKey];
  if (typeof optimisticValue === "boolean") {
    return optimisticValue;
  }

  return fallbackValue;
}

function getBooleanValue(value: unknown) {
  if (typeof value === "boolean") {
    return value;
  }

  return null;
}

function getNumericValue(value: unknown) {
  if (typeof value === "number") {
    return String(value);
  }

  if (typeof value === "string" && value.trim() !== "") {
    const parsed = Number(value);
    if (Number.isFinite(parsed)) {
      return value;
    }
  }

  return null;
}

function getDisplayValue(
  primaryStateValue: unknown,
  t: (key: string) => string,
  formatCapabilityState: (state: unknown) => string
) {
  if (primaryStateValue === undefined) {
    return t("notAvailable");
  }

  if (primaryStateValue === null) {
    return t("nullValue");
  }

  if (
    typeof primaryStateValue === "string" ||
    typeof primaryStateValue === "number"
  ) {
    return String(primaryStateValue);
  }

  if (typeof primaryStateValue === "boolean") {
    return primaryStateValue ? t("on") : t("off");
  }

  return formatCapabilityState(primaryStateValue);
}

function isPlainObject(value: unknown): value is Record<string, unknown> {
  return typeof value === "object" && value !== null && !Array.isArray(value);
}

function getSchemaPrimaryType(schema: Record<string, unknown>) {
  const schemaType = schema.type;

  if (typeof schemaType === "string") {
    return schemaType.trim().toLowerCase();
  }

  if (Array.isArray(schemaType)) {
    const normalized = schemaType
      .filter((candidate): candidate is string => typeof candidate === "string")
      .map((candidate) => candidate.trim().toLowerCase());

    return normalized.find((candidate) => candidate !== "null") ?? normalized[0] ?? null;
  }

  return null;
}

function getRequiredFields(schema: Record<string, unknown>) {
  const required = schema.required;

  if (!Array.isArray(required)) {
    return new Set<string>();
  }

  return new Set(
    required
      .filter((field): field is string => typeof field === "string")
      .map((field) => field.trim())
      .filter((field) => field !== "")
  );
}

function isUnsupportedAdvancedRule(rule: OperationRule | null) {
  const type = normalizeRuleType(rule);

  return !rule || type === "array" || type === "object";
}

function mergeRecords(
  target: Record<string, unknown>,
  source: Record<string, unknown>
) {
  const merged: Record<string, unknown> = { ...target };

  Object.entries(source).forEach(([key, value]) => {
    const existing = merged[key];

    if (isPlainObject(existing) && isPlainObject(value)) {
      merged[key] = mergeRecords(existing, value);
      return;
    }

    merged[key] = value;
  });

  return merged;
}

function getAdvancedOperationFields(
  operations: Record<string, Record<string, unknown>> | null,
  operation: string
): AdvancedOperationField[] {
  const operationSchema = getOperationSchema(operations, operation);

  if (!operationSchema) {
    return [];
  }

  const fields: AdvancedOperationField[] = [];

  const pushLeafField = (path: string, required: boolean) => {
    const rule = path
      ? getOperationRuleForPath(operations, operation, path)
      : getOperationRule(operations, operation);

    fields.push({
      path,
      label: path || "value",
      required,
      rule,
      unsupported: isUnsupportedAdvancedRule(rule),
    });
  };

  const collect = (
    schema: Record<string, unknown>,
    currentPath: string,
    currentRequired: boolean
  ) => {
    const schemaType = getSchemaPrimaryType(schema);
    const properties = isPlainObject(schema.properties)
      ? (schema.properties as Record<string, unknown>)
      : null;

    if ((schemaType === "object" || schemaType === null) && properties) {
      const requiredFields = getRequiredFields(schema);
      const propertyEntries = Object.entries(properties).filter(([, propertySchema]) =>
        isPlainObject(propertySchema)
      );

      if (propertyEntries.length === 0) {
        pushLeafField(currentPath, currentRequired);
        return;
      }

      propertyEntries.forEach(([propertyName, propertySchema]) => {
        const normalizedPropertyName = propertyName.trim();
        if (!normalizedPropertyName) {
          return;
        }

        const propertyPath = currentPath
          ? `${currentPath}.${normalizedPropertyName}`
          : normalizedPropertyName;
        const propertyRequired = currentRequired && requiredFields.has(normalizedPropertyName);

        collect(propertySchema as Record<string, unknown>, propertyPath, propertyRequired);
      });

      return;
    }

    pushLeafField(currentPath, currentRequired);
  };

  collect(operationSchema as Record<string, unknown>, "", true);

  return fields;
}

function buildOperationFieldStates(
  fields: AdvancedOperationField[],
  fieldValues: Record<string, string>
) {
  return fields.map((field) => {
    const defaultFieldValue = field.required
      ? defaultValueForRule(field.rule)
      : "";
    const rawValue =
      fieldValues[field.path] ?? defaultFieldValue;
    const validation =
      rawValue.trim() === "" && !field.required
        ? {
          value: null,
          errorKey: null,
          errorOptions: undefined,
        }
        : validateCommandValue(field.rule, rawValue);

    return {
      ...field,
      rawValue,
      validation,
    };
  });
}

export function DeviceCapabilitiesSection({
  capabilityGroups,
  canControlDevice,
  quickToggleBusyCapabilityId,
  optimisticToggleValues,
  inlineCommandValues,
  setInlineCommandValues,
  onBooleanToggleSend,
  onScheduleInlineCommandSend,
  onInlineCommandSend,
  quickActionError,
  formatCapabilityState,
}: Props) {
  const { t } = useTranslation("devices");

  const localizeStatePathLabel = (path: string) => {
    const normalizedPath = path.trim();

    if (!normalizedPath) {
      return t("value");
    }

    const segments = normalizedPath
      .split(".")
      .map((segment) => segment.trim())
      .filter((segment) => segment !== "");

    if (segments.length === 0) {
      return t("value");
    }

    const leafKey = segments[segments.length - 1];
    const localizedLeaf = t(`stateKeyLabels.${leafKey}`, {
      defaultValue: leafKey,
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

    return t(`operationKeyLabels.${normalized.toLowerCase()}`, {
      defaultValue: normalized,
    });
  };

  const [expandedCapabilityId, setExpandedCapabilityId] = useState<string | null>(null);
  const [advancedOperationByCapability, setAdvancedOperationByCapability] = useState<
    Record<string, string>
  >({});
  const [advancedFieldValuesByContext, setAdvancedFieldValuesByContext] = useState<
    Record<string, Record<string, string>>
  >({});

  return (
    <section className={styles.section}>
      <div className={styles.sectionHeader}>
        <h2 className={styles.sectionTitle}>{t("capabilities")}</h2>
      </div>

      {capabilityGroups.map((group) => (
        <div key={group.endpointKey} className={pageStyles.endpointGroup}>
          <div className={pageStyles.endpointGroupHeader}>
            <h3 className={pageStyles.endpointGroupTitle}>{t("scope")}: {group.endpointLabel}</h3>
            <span className={pageStyles.endpointGroupCount}>{group.capabilities.length} {t("capabilities")}</span>
          </div>

          <CellGrid>
            {group.capabilities
              .map((capability) => ({
                capability,
                displayLabel: getCapabilityDisplayLabel(
                  t,
                  capability.capabilityId,
                  capability.metadata?.defaultName
                ),
              }))
              .sort((left, right) => {
                const orderDelta =
                  getCapabilityDisplayOrder(left.capability.metadata) -
                  getCapabilityDisplayOrder(right.capability.metadata);

                if (orderDelta !== 0) {
                  return orderDelta;
                }

                const labelDelta = left.displayLabel.localeCompare(right.displayLabel);
                if (labelDelta !== 0) {
                  return labelDelta;
                }

                return left.capability.capabilityId.localeCompare(
                  right.capability.capabilityId
                );
              })
              .map(({ capability, displayLabel }) => {
                const primaryStatePath = capability.metadata?.primary?.state ?? null;
                const primaryStateValue = getCapabilityPrimaryStateValue(
                  capability.state,
                  primaryStatePath
                );
                const displayValue = getDisplayValue(
                  primaryStateValue,
                  t,
                  formatCapabilityState
                );

                const capabilityUnit = getCapabilityUnit(capability.metadata);
                const canShowUnit =
                  capabilityUnit !== null &&
                  displayValue !== t("notAvailable") &&
                  (typeof primaryStateValue === "number" ||
                    typeof primaryStateValue === "string");
                const titleWithPrimaryValue = `${displayLabel}: ${displayValue}${canShowUnit ? ` ${capabilityUnit}` : ""
                  }`;

                const canSendFromCapability =
                  capability.hasRegistryMetadata && capability.supportedOperations.length > 0;

                const {
                  operation: primaryOperationReference,
                  valuePath: primaryValuePathFromMetadata,
                } =
                  parsePrimaryOperationReference(capability.metadata?.primary?.operation);
                const primaryOperationFromMetadata = resolveSupportedOperation(
                  capability.supportedOperations,
                  primaryOperationReference
                );
                const primaryOperation = primaryOperationFromMetadata;

                const primaryOperationFields = primaryOperation
                  ? getAdvancedOperationFields(
                    capability.operations as Record<string, Record<string, unknown>> | null,
                    primaryOperation
                  )
                  : [];
                const explicitPrimaryField =
                  primaryValuePathFromMetadata
                    ? primaryOperationFields.find(
                      (field) => field.path === primaryValuePathFromMetadata
                    ) ?? null
                    : null;
                const primaryFieldFromStatePath =
                  !explicitPrimaryField && primaryStatePath
                    ? primaryOperationFields.find(
                      (field) => field.path === primaryStatePath
                    ) ?? null
                    : null;
                const inferredPrimaryField =
                  !explicitPrimaryField &&
                    !primaryFieldFromStatePath &&
                    primaryOperationFields.length === 1
                    ? primaryOperationFields[0]
                    : null;
                const selectedPrimaryField =
                  explicitPrimaryField ??
                  primaryFieldFromStatePath ??
                  inferredPrimaryField;
                const primaryValuePath =
                  selectedPrimaryField?.path ?? primaryValuePathFromMetadata ?? null;
                const primaryRule = selectedPrimaryField?.rule ?? (primaryOperation
                  ? getOperationRuleForPath(
                    capability.operations,
                    primaryOperation,
                    primaryValuePath
                  )
                  : null);

                const primaryRuleType = normalizeRuleType(primaryRule);
                const canQuickPrimaryMultiField =
                  canSendFromCapability &&
                  Boolean(primaryOperation) &&
                  primaryOperationFields.length >= 2;
                const canQuickToggle =
                  canSendFromCapability &&
                  primaryRuleType === "boolean" &&
                  !canQuickPrimaryMultiField;
                const canQuickNumberSet =
                  canSendFromCapability &&
                  (primaryRuleType === "integer" || primaryRuleType === "number") &&
                  !canQuickPrimaryMultiField;
                const canQuickScalarSet =
                  canSendFromCapability &&
                  !canQuickToggle &&
                  !canQuickNumberSet &&
                  Boolean(primaryRuleType) &&
                  primaryRuleType !== "array" &&
                  primaryRuleType !== "object" &&
                  !canQuickPrimaryMultiField;
                const useNumericSlider = isNumericSliderRule(primaryRule);
                const hasInlineQuickControl =
                  canQuickToggle ||
                  canQuickNumberSet ||
                  canQuickScalarSet ||
                  canQuickPrimaryMultiField;

                const booleanStateValue = getBooleanValue(primaryStateValue);
                const numericStateValue = getNumericValue(primaryStateValue);
                const isQuickToggleBusy = quickToggleBusyCapabilityId === capability.id;
                const inlineCommandValue =
                  inlineCommandValues[capability.id] ??
                  numericStateValue ??
                  (useNumericSlider
                    ? getDefaultNumericValue(primaryRule)
                    : defaultValueForRule(primaryRule));
                const minValue = primaryRule?.min ?? undefined;
                const maxValue = primaryRule?.max ?? undefined;
                const numericMinValue = typeof minValue === "number" ? minValue : 0;
                const numericMaxValue = typeof maxValue === "number" ? maxValue : 100;
                const parsedInlineSliderValue = Number(inlineCommandValue);
                const inlineSliderValue = Number.isFinite(parsedInlineSliderValue)
                  ? parsedInlineSliderValue
                  : numericMinValue;
                const inlineValueValidation = validateCommandValue(
                  primaryRule,
                  inlineCommandValue
                );

                const primaryContextKey = `${capability.id}|__primary__|${primaryOperation ?? ""}`;
                const primaryFieldValues = advancedFieldValuesByContext[primaryContextKey] ?? {};
                const primaryFieldStates = canQuickPrimaryMultiField
                  ? buildOperationFieldStates(primaryOperationFields, primaryFieldValues)
                  : [];
                const hasUnsupportedPrimaryField = primaryFieldStates.some(
                  (field) => field.unsupported
                );
                const hasPrimaryValidationError = primaryFieldStates.some(
                  (field) => field.validation.errorKey
                );
                const rootPrimaryField = primaryFieldStates.find(
                  (field) => field.path === ""
                );

                const normalizedPrimaryOperation = primaryOperation?.trim().toLowerCase();
                const advancedOperations = capability.supportedOperations.filter((operation) =>
                  normalizedPrimaryOperation
                    ? operation.trim().toLowerCase() !== normalizedPrimaryOperation
                    : true
                );
                const canShowAdvancedControls =
                  canSendFromCapability && advancedOperations.length > 0;

                const isAdvancedOpen = expandedCapabilityId === capability.id;
                const selectedAdvancedOperationCandidate =
                  advancedOperationByCapability[capability.id];
                const selectedAdvancedOperation =
                  resolveSupportedOperation(
                    advancedOperations,
                    selectedAdvancedOperationCandidate
                  ) ?? advancedOperations[0] ?? "";
                const advancedRule = getOperationRule(
                  capability.operations,
                  selectedAdvancedOperation
                );
                const advancedFields = selectedAdvancedOperation
                  ? getAdvancedOperationFields(
                    capability.operations as Record<string, Record<string, unknown>> | null,
                    selectedAdvancedOperation
                  )
                  : [];

                const advancedContextKey = `${capability.id}|${selectedAdvancedOperation}`;
                const advancedFieldValues =
                  advancedFieldValuesByContext[advancedContextKey] ?? {};
                const advancedFieldStates = buildOperationFieldStates(
                  advancedFields,
                  advancedFieldValues
                );

                const hasUnsupportedAdvancedField = advancedFieldStates.some(
                  (field) => field.unsupported
                );
                const hasAdvancedValidationError = advancedFieldStates.some(
                  (field) => field.validation.errorKey
                );
                const rootAdvancedField = advancedFieldStates.find(
                  (field) => field.path === ""
                );

                return (
                  <div key={capability.id} className={pageStyles.capabilityCardWrap}>
                    <Cell
                      id={capability.id}
                      title={titleWithPrimaryValue}
                      onClick={() => void 0}
                      disabled={false}
                    />

                    <div className={pageStyles.capabilityActions}>
                      <div className={pageStyles.inlineControlArea}>
                        {canQuickToggle && primaryOperation ? (
                          <>
                            {(() => {
                              const displayedToggleValue = getDisplayedToggleValue(
                                optimisticToggleValues,
                                capability.id,
                                booleanStateValue
                              );

                              return (
                                <BooleanSwitch
                                  checked={Boolean(displayedToggleValue)}
                                  disabled={!canControlDevice || isQuickToggleBusy || booleanStateValue === null}
                                  label={
                                    isQuickToggleBusy
                                      ? t("sending")
                                      : displayedToggleValue
                                        ? t("on")
                                        : t("off")
                                  }
                                  onChange={(nextChecked) => {
                                    onBooleanToggleSend(
                                      capability.capabilityId,
                                      capability.id,
                                      capability.endpointId,
                                      primaryOperation,
                                      nextChecked,
                                      displayedToggleValue,
                                      primaryValuePath
                                    );
                                  }}
                                />
                              );
                            })()}
                          </>
                        ) : useNumericSlider && primaryOperation ? (
                          <div className={pageStyles.inlineSliderControl}>
                            <NumericSliderField
                              inputValue={inlineCommandValue}
                              sliderValue={inlineSliderValue}
                              min={numericMinValue}
                              max={numericMaxValue}
                              step={getNumericStepForRule(primaryRule)}
                              placeholder={t("enterValue")}
                              disabled={!canControlDevice || isQuickToggleBusy}
                              onInputChange={(nextValue) => {
                                setInlineCommandValues((current) => ({
                                  ...current,
                                  [capability.id]: nextValue,
                                }));

                                onScheduleInlineCommandSend(
                                  capability.capabilityId,
                                  capability.id,
                                  capability.endpointId,
                                  primaryOperation,
                                  primaryRule,
                                  nextValue,
                                  primaryValuePath
                                );
                              }}
                              onSliderChange={(nextValue) => {
                                const nextRawValue = String(nextValue);

                                setInlineCommandValues((current) => ({
                                  ...current,
                                  [capability.id]: nextRawValue,
                                }));

                                onScheduleInlineCommandSend(
                                  capability.capabilityId,
                                  capability.id,
                                  capability.endpointId,
                                  primaryOperation,
                                  primaryRule,
                                  nextRawValue,
                                  primaryValuePath
                                );
                              }}
                            />
                            <div className={pageStyles.capabilityHint}>{t("typeOrDrag")}</div>
                          </div>
                        ) : canQuickNumberSet && primaryOperation ? (
                          <div className={pageStyles.inlineQuickNumberControl}>
                            <div className={pageStyles.inlineQuickNumberRow}>
                              <Input
                                type="number"
                                value={inlineCommandValue}
                                onChange={(event) =>
                                  setInlineCommandValues((current) => ({
                                    ...current,
                                    [capability.id]: event.target.value,
                                  }))
                                }
                                min={minValue}
                                max={maxValue}
                                step={getNumericStepForRule(primaryRule)}
                                placeholder={t("enterValue")}
                                disabled={!canControlDevice || isQuickToggleBusy}
                              />
                              <Button
                                size="sm"
                                className={`${pageStyles.compactActionButton} ${pageStyles.inlineQuickSetButton}`}
                                onClick={() => {
                                  onInlineCommandSend(
                                    capability.capabilityId,
                                    capability.id,
                                    capability.endpointId,
                                    primaryOperation,
                                    primaryRule,
                                    inlineCommandValue,
                                    primaryValuePath
                                  );
                                }}
                                disabled={!canControlDevice || isQuickToggleBusy || Boolean(inlineValueValidation.errorKey)}
                              >
                                {t("set")}
                              </Button>
                            </div>
                          </div>
                        ) : canQuickScalarSet && primaryOperation ? (
                          <div className={pageStyles.inlineQuickNumberControl}>
                            <div className={pageStyles.inlineQuickNumberRow}>
                              {Array.isArray(primaryRule?.enumValues) && primaryRule.enumValues.length > 0 ? (
                                <select
                                  className={styles.select}
                                  value={inlineCommandValue}
                                  onChange={(event) =>
                                    setInlineCommandValues((current) => ({
                                      ...current,
                                      [capability.id]: event.target.value,
                                    }))
                                  }
                                  disabled={!canControlDevice || isQuickToggleBusy}
                                >
                                  {primaryRule.enumValues.map((enumValue, enumIndex) => (
                                    <option
                                      key={`${capability.id}:${String(enumValue)}:${enumIndex}`}
                                      value={String(enumValue)}
                                    >
                                      {String(enumValue)}
                                    </option>
                                  ))}
                                </select>
                              ) : (
                                <Input
                                  type="text"
                                  value={inlineCommandValue}
                                  onChange={(event) =>
                                    setInlineCommandValues((current) => ({
                                      ...current,
                                      [capability.id]: event.target.value,
                                    }))
                                  }
                                  placeholder={t("enterValue")}
                                  disabled={!canControlDevice || isQuickToggleBusy}
                                />
                              )}

                              <Button
                                size="sm"
                                className={`${pageStyles.compactActionButton} ${pageStyles.inlineQuickSetButton}`}
                                onClick={() => {
                                  onInlineCommandSend(
                                    capability.capabilityId,
                                    capability.id,
                                    capability.endpointId,
                                    primaryOperation,
                                    primaryRule,
                                    inlineCommandValue,
                                    primaryValuePath
                                  );
                                }}
                                disabled={!canControlDevice || isQuickToggleBusy || Boolean(inlineValueValidation.errorKey)}
                              >
                                {t("set")}
                              </Button>
                            </div>
                          </div>
                        ) : canQuickPrimaryMultiField && primaryOperation ? (
                          <div className={pageStyles.inlineNumberControl}>
                            {primaryFieldStates.map((field) => {
                              const inputId = `primary-field-${capability.id}-${field.path || "value"}`;
                              const normalizedFieldType = normalizeRuleType(field.rule);
                              const enumValues = Array.isArray(field.rule?.enumValues)
                                ? field.rule.enumValues
                                : [];
                              const inputType =
                                normalizedFieldType === "number" ||
                                  normalizedFieldType === "integer"
                                  ? "number"
                                  : "text";
                              const fieldLabel = localizeStatePathLabel(field.path);

                              return (
                                <div key={`${primaryContextKey}:${field.path}`} className={pageStyles.inlineNumberControl}>
                                  <label className={pageStyles.filterLabel} htmlFor={inputId}>
                                    {fieldLabel}
                                    {field.required ? " *" : ""}
                                  </label>

                                  {field.unsupported ? (
                                    <div className={pageStyles.capabilityHint}>
                                      {t("advancedFieldUnsupported", {
                                        field: fieldLabel,
                                        defaultValue: t("advancedFieldUnsupported", {
                                          field: fieldLabel,
                                        }),
                                      })}
                                    </div>
                                  ) : normalizedFieldType === "boolean" ? (
                                    <select
                                      id={inputId}
                                      className={styles.select}
                                      value={field.rawValue}
                                      disabled={!canControlDevice || isQuickToggleBusy}
                                      onChange={(event) => {
                                        const nextValue = event.target.value;

                                        setAdvancedFieldValuesByContext((current) => ({
                                          ...current,
                                          [primaryContextKey]: {
                                            ...(current[primaryContextKey] ?? {}),
                                            [field.path]: nextValue,
                                          },
                                        }));
                                      }}
                                    >
                                      {!field.required ? (
                                        <option value="">-</option>
                                      ) : null}
                                      <option value="true">{t("on")} (true)</option>
                                      <option value="false">{t("off")} (false)</option>
                                    </select>
                                  ) : enumValues.length > 0 ? (
                                    <select
                                      id={inputId}
                                      className={styles.select}
                                      value={field.rawValue}
                                      disabled={!canControlDevice || isQuickToggleBusy}
                                      onChange={(event) => {
                                        const nextValue = event.target.value;

                                        setAdvancedFieldValuesByContext((current) => ({
                                          ...current,
                                          [primaryContextKey]: {
                                            ...(current[primaryContextKey] ?? {}),
                                            [field.path]: nextValue,
                                          },
                                        }));
                                      }}
                                    >
                                      {!field.required ? (
                                        <option value="">-</option>
                                      ) : null}
                                      {enumValues.map((enumValue, enumIndex) => (
                                        <option
                                          key={`${primaryContextKey}:${field.path}:${String(enumValue)}:${enumIndex}`}
                                          value={String(enumValue)}
                                        >
                                          {String(enumValue)}
                                        </option>
                                      ))}
                                    </select>
                                  ) : (
                                    <Input
                                      id={inputId}
                                      type={inputType}
                                      value={field.rawValue}
                                      onChange={(event) => {
                                        const nextValue = event.target.value;

                                        setAdvancedFieldValuesByContext((current) => ({
                                          ...current,
                                          [primaryContextKey]: {
                                            ...(current[primaryContextKey] ?? {}),
                                            [field.path]: nextValue,
                                          },
                                        }));
                                      }}
                                      min={field.rule?.min ?? undefined}
                                      max={field.rule?.max ?? undefined}
                                      step={getNumericStepForRule(field.rule)}
                                      placeholder={t("enterValue")}
                                      disabled={!canControlDevice || isQuickToggleBusy}
                                    />
                                  )}

                                  {field.validation.errorKey ? (
                                    <div className={pageStyles.capabilityHint}>
                                      {t(field.validation.errorKey, {
                                        ...(field.validation.errorOptions ?? {}),
                                        defaultValue: field.validation.errorKey,
                                      })}
                                    </div>
                                  ) : null}
                                </div>
                              );
                            })}

                            {hasUnsupportedPrimaryField ? (
                              <div className={pageStyles.capabilityHint}>
                                {t("advancedControlSchemaUnsupported")}
                              </div>
                            ) : null}

                            <Button
                              size="sm"
                              className={pageStyles.compactActionButton}
                              onClick={() => {
                                const payload = primaryFieldStates.reduce<Record<string, unknown>>(
                                  (currentPayload, field) => {
                                    if (field.unsupported) {
                                      return currentPayload;
                                    }

                                    if (field.rawValue.trim() === "" && !field.required) {
                                      return currentPayload;
                                    }

                                    if (field.path === "") {
                                      return currentPayload;
                                    }

                                    const nextPayload = composeValueByPath(
                                      field.path,
                                      field.validation.value
                                    );

                                    if (!isPlainObject(nextPayload)) {
                                      return currentPayload;
                                    }

                                    return mergeRecords(currentPayload, nextPayload);
                                  },
                                  {}
                                );

                                const payloadValue = rootPrimaryField
                                  ? rootPrimaryField.validation.value
                                  : payload;

                                onInlineCommandSend(
                                  capability.capabilityId,
                                  capability.id,
                                  capability.endpointId,
                                  primaryOperation,
                                  primaryRule,
                                  "",
                                  null,
                                  payloadValue
                                );
                              }}
                              disabled={
                                !canControlDevice ||
                                isQuickToggleBusy ||
                                hasUnsupportedPrimaryField ||
                                hasPrimaryValidationError
                              }
                            >
                              {t("send")}
                            </Button>
                          </div>
                        ) : (
                          null
                        )}

                        {isAdvancedOpen && canShowAdvancedControls ? (
                          <div className={pageStyles.schemaBlock}>
                            <div className={pageStyles.inlineNumberControl}>
                              <label className={pageStyles.filterLabel} htmlFor={`advanced-operation-${capability.id}`}>
                                {t("operation")}
                              </label>
                              <select
                                id={`advanced-operation-${capability.id}`}
                                className={styles.select}
                                value={selectedAdvancedOperation}
                                onChange={(event) => {
                                  const nextOperation = event.target.value;

                                  setAdvancedOperationByCapability((current) => ({
                                    ...current,
                                    [capability.id]: nextOperation,
                                  }));
                                }}
                              >
                                {advancedOperations.map((operation) => (
                                  <option key={`${capability.id}-${operation}`} value={operation}>
                                    {localizeOperationLabel(operation)}
                                  </option>
                                ))}
                              </select>

                              {advancedFieldStates.map((field) => {
                                const inputId = `advanced-field-${capability.id}-${field.path || "value"}`;
                                const normalizedFieldType = normalizeRuleType(field.rule);
                                const enumValues = Array.isArray(field.rule?.enumValues)
                                  ? field.rule.enumValues
                                  : [];
                                const inputType =
                                  normalizedFieldType === "number" ||
                                    normalizedFieldType === "integer"
                                    ? "number"
                                    : "text";
                                const fieldLabel = localizeStatePathLabel(field.path);

                                return (
                                  <div key={`${advancedContextKey}:${field.path}`} className={pageStyles.inlineNumberControl}>
                                    <label className={pageStyles.filterLabel} htmlFor={inputId}>
                                      {fieldLabel}
                                      {field.required ? " *" : ""}
                                    </label>

                                    {field.unsupported ? (
                                      <div className={pageStyles.capabilityHint}>
                                        {t("advancedFieldUnsupported", {
                                          field: fieldLabel,
                                          defaultValue: t("advancedFieldUnsupported", {
                                            field: fieldLabel,
                                          }),
                                        })}
                                      </div>
                                    ) : normalizedFieldType === "boolean" ? (
                                      <select
                                        id={inputId}
                                        className={styles.select}
                                        value={field.rawValue}
                                        onChange={(event) => {
                                          const nextValue = event.target.value;

                                          setAdvancedFieldValuesByContext((current) => ({
                                            ...current,
                                            [advancedContextKey]: {
                                              ...(current[advancedContextKey] ?? {}),
                                              [field.path]: nextValue,
                                            },
                                          }));
                                        }}
                                      >
                                        {!field.required ? (
                                          <option value="">-</option>
                                        ) : null}
                                        <option value="true">{t("on")} (true)</option>
                                        <option value="false">{t("off")} (false)</option>
                                      </select>
                                    ) : enumValues.length > 0 ? (
                                      <select
                                        id={inputId}
                                        className={styles.select}
                                        value={field.rawValue}
                                        onChange={(event) => {
                                          const nextValue = event.target.value;

                                          setAdvancedFieldValuesByContext((current) => ({
                                            ...current,
                                            [advancedContextKey]: {
                                              ...(current[advancedContextKey] ?? {}),
                                              [field.path]: nextValue,
                                            },
                                          }));
                                        }}
                                      >
                                        {!field.required ? (
                                          <option value="">-</option>
                                        ) : null}
                                        {enumValues.map((enumValue, index) => (
                                          <option
                                            key={`${advancedContextKey}:${field.path}:${String(enumValue)}:${index}`}
                                            value={String(enumValue)}
                                          >
                                            {String(enumValue)}
                                          </option>
                                        ))}
                                      </select>
                                    ) : (
                                      <Input
                                        id={inputId}
                                        type={inputType}
                                        value={field.rawValue}
                                        onChange={(event) => {
                                          const nextValue = event.target.value;

                                          setAdvancedFieldValuesByContext((current) => ({
                                            ...current,
                                            [advancedContextKey]: {
                                              ...(current[advancedContextKey] ?? {}),
                                              [field.path]: nextValue,
                                            },
                                          }));
                                        }}
                                        min={field.rule?.min ?? undefined}
                                        max={field.rule?.max ?? undefined}
                                        step={getNumericStepForRule(field.rule)}
                                        placeholder={t("enterValue")}
                                        disabled={!canControlDevice || isQuickToggleBusy}
                                      />
                                    )}

                                    {field.validation.errorKey ? (
                                      <div className={pageStyles.capabilityHint}>
                                        {t(field.validation.errorKey, {
                                          ...(field.validation.errorOptions ?? {}),
                                          defaultValue: field.validation.errorKey,
                                        })}
                                      </div>
                                    ) : null}
                                  </div>
                                );
                              })}

                              {hasUnsupportedAdvancedField ? (
                                <div className={pageStyles.capabilityHint}>
                                  {t("advancedControlSchemaUnsupported")}
                                </div>
                              ) : null}

                              <Button
                                size="sm"
                                className={pageStyles.compactActionButton}
                                onClick={() => {
                                  const payload = advancedFieldStates.reduce<Record<string, unknown>>(
                                    (currentPayload, field) => {
                                      if (field.unsupported) {
                                        return currentPayload;
                                      }

                                      if (field.rawValue.trim() === "" && !field.required) {
                                        return currentPayload;
                                      }

                                      if (field.path === "") {
                                        return currentPayload;
                                      }

                                      const nextPayload = composeValueByPath(
                                        field.path,
                                        field.validation.value
                                      );

                                      if (!isPlainObject(nextPayload)) {
                                        return currentPayload;
                                      }

                                      return mergeRecords(currentPayload, nextPayload);
                                    },
                                    {}
                                  );

                                  const payloadValue = rootAdvancedField
                                    ? rootAdvancedField.validation.value
                                    : payload;

                                  onInlineCommandSend(
                                    capability.capabilityId,
                                    capability.id,
                                    capability.endpointId,
                                    selectedAdvancedOperation,
                                    advancedRule,
                                    "",
                                    null,
                                    payloadValue
                                  );
                                }}
                                disabled={
                                  !canControlDevice ||
                                  isQuickToggleBusy ||
                                  !selectedAdvancedOperation ||
                                  hasUnsupportedAdvancedField ||
                                  hasAdvancedValidationError
                                }
                              >
                                {t("send")}
                              </Button>
                            </div>
                          </div>
                        ) : null}
                      </div>

                      {canShowAdvancedControls ? (
                        <Button
                          size="sm"
                          variant={hasInlineQuickControl ? "secondary" : "primary"}
                          onClick={() => {
                            setExpandedCapabilityId((current) =>
                              current === capability.id ? null : capability.id
                            );

                            setAdvancedOperationByCapability((current) => {
                              if (current[capability.id]) {
                                return current;
                              }

                              return {
                                ...current,
                                [capability.id]: advancedOperations[0] ?? "",
                              };
                            });
                          }}
                          disabled={!canControlDevice}
                        >
                          {isAdvancedOpen ? t("hideAdvancedControl") : t("advancedControl")}
                        </Button>
                      ) : null}
                    </div>
                  </div>
                );
              })}
          </CellGrid>
        </div>
      ))}

      {quickActionError ? <div className={styles.emptyState}>{quickActionError}</div> : null}
    </section>
  );
}
