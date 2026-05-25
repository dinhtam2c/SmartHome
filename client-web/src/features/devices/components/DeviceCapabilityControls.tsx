import type { Dispatch, SetStateAction } from "react";
import { useTranslation } from "react-i18next";
import { Button } from "@/shared/ui/Button";
import { Input } from "@/shared/ui/Input";
import { NumericSliderField } from "@/shared/ui/NumericSliderField";
import {
  CAPABILITY_LABEL_KEYS,
  getCapabilityBooleanLabel,
  getCapabilityBooleanLabels,
  getCapabilityDeviceControlDefinition,
  getValueByPath,
  localizeCapabilityOperation,
  parsePrimaryOperationReference,
  type CapabilityDeviceControlDefinition,
} from "@/features/capabilities";
import { CapabilityBooleanControl } from "@/features/capabilities/components/CapabilityBooleanControl";
import {
  type OperationRule,
  defaultValueForRule,
  getDefaultNumericValue,
  getNumericStepForRule,
  getOperationRule,
  getOperationRuleForPath,
  isNumericSliderRule,
  normalizeRuleType,
  resolveSupportedOperation,
  validateCommandValue,
} from "../services/deviceCapabilityService";
import {
  buildOperationFieldStates,
  getOperationFields,
  resolveOperationFieldRenderPlan,
} from "../services/deviceOperationFieldService";
import type { DeviceCapabilityDto } from "../types/deviceTypes";
import { DeviceOperationCommandForm } from "./DeviceOperationCommandForm";
import styles from "@/shared/styles/featurePage.module.css";
import pageStyles from "../pages/DeviceDetailPage.module.css";

type Props = {
  capability: DeviceCapabilityDto;
  canControlDevice: boolean;
  quickToggleBusyCapabilityId: string | null;
  optimisticToggleValues: Record<string, boolean | undefined>;
  inlineCommandValues: Record<string, string>;
  setInlineCommandValues: Dispatch<SetStateAction<Record<string, string>>>;
  expandedCapabilityId: string | null;
  setExpandedCapabilityId: Dispatch<SetStateAction<string | null>>;
  advancedOperationByCapability: Record<string, string>;
  setAdvancedOperationByCapability: Dispatch<SetStateAction<Record<string, string>>>;
  advancedFieldValuesByContext: Record<string, Record<string, string>>;
  setAdvancedFieldValuesByContext: Dispatch<
    SetStateAction<Record<string, Record<string, string>>>
  >;
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
  onLiveInlineCommandSend: (
    capabilityId: string,
    capabilityKey: string,
    endpointId: string,
    operation: string,
    rule: OperationRule | null,
    rawValue: string,
    valuePath?: string | null,
    valueOverride?: unknown
  ) => void;
};

type OperationRenderPlan = ReturnType<typeof resolveOperationFieldRenderPlan>;

function isOperationRenderPlanUsable(plan: OperationRenderPlan | null) {
  return Boolean(plan && plan.kind !== "unsupported" && plan.skippedFields.length === 0);
}

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
  return typeof value === "boolean" ? value : null;
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

function getSupportedOperation(
  capability: DeviceCapabilityDto,
  operation: string | null | undefined
) {
  return resolveSupportedOperation(capability.supportedOperations, operation);
}

function canSendFromCapability(capability: DeviceCapabilityDto) {
  return capability.hasRegistryMetadata && capability.supportedOperations.length > 0;
}

function getOperationFieldsForCapability(capability: DeviceCapabilityDto, operation: string) {
  return getOperationFields(
    capability.operations as Record<string, Record<string, unknown>> | null,
    operation
  );
}

function getOperationContextKey(capability: DeviceCapabilityDto, operation: string) {
  return `${capability.id}|${operation}`;
}

function createFieldValueUpdater({
  setAdvancedFieldValuesByContext,
}: Pick<Props, "setAdvancedFieldValuesByContext">) {
  return (contextKey: string, path: string, nextValue: string) => {
    setAdvancedFieldValuesByContext((current) => ({
      ...current,
      [contextKey]: {
        ...(current[contextKey] ?? {}),
        [path]: nextValue,
      },
    }));
  };
}

function updateInlineCommandValue(
  setInlineCommandValues: Props["setInlineCommandValues"],
  capabilityKey: string,
  nextValue: string
) {
  setInlineCommandValues((current) => ({
    ...current,
    [capabilityKey]: nextValue,
  }));
}

function SpecificCapabilityControl({
  definition,
  props,
}: {
  definition: CapabilityDeviceControlDefinition;
  props: Props;
}) {
  const {
    capability,
    canControlDevice,
    quickToggleBusyCapabilityId,
    optimisticToggleValues,
    inlineCommandValues,
    setInlineCommandValues,
    advancedFieldValuesByContext,
    setAdvancedFieldValuesByContext,
    onBooleanToggleSend,
    onScheduleInlineCommandSend,
    onInlineCommandSend,
    onLiveInlineCommandSend,
  } = props;
  const { t } = useTranslation("devices");
  const booleanLabels = getCapabilityBooleanLabels(t, CAPABILITY_LABEL_KEYS.device);
  const isBusy = quickToggleBusyCapabilityId === capability.id;
  const updateFieldValue = createFieldValueUpdater({ setAdvancedFieldValuesByContext });

  if (definition.kind === "readonly") {
    return null;
  }

  const operation = getSupportedOperation(capability, definition.operation);
  if (!operation || !canSendFromCapability(capability)) {
    return null;
  }

  if (definition.kind === "boolean") {
    const stateValue = getBooleanValue(getValueByPath(capability.state, definition.statePath));
    const displayedValue = getDisplayedToggleValue(
      optimisticToggleValues,
      capability.id,
      stateValue
    );
    const label = isBusy
      ? t("sending")
      : getCapabilityBooleanLabel(
        capability.capabilityId,
        Boolean(displayedValue),
        booleanLabels
      );

    return (
      <CapabilityBooleanControl
        capabilityId={capability.capabilityId}
        checked={Boolean(displayedValue)}
        disabled={!canControlDevice || isBusy || stateValue === null}
        labels={booleanLabels}
        label={label}
        onChange={(nextChecked) => {
          onBooleanToggleSend(
            capability.capabilityId,
            capability.id,
            capability.endpointId,
            operation,
            nextChecked,
            displayedValue,
            definition.valuePath
          );
        }}
      />
    );
  }

  if (definition.kind === "numericSlider") {
    const rule = getOperationRuleForPath(
      capability.operations,
      operation,
      definition.valuePath
    );
    if (!rule) {
      return null;
    }

    const stateValue = definition.statePath
      ? getValueByPath(capability.state, definition.statePath)
      : null;
    const numericStateValue = getNumericValue(stateValue);
    const inlineCommandValue =
      inlineCommandValues[capability.id] ??
      numericStateValue ??
      getDefaultNumericValue(rule);
    const min = typeof rule.min === "number" ? rule.min : 0;
    const max = typeof rule.max === "number" ? rule.max : 100;
    const parsedSliderValue = Number(inlineCommandValue);
    const sliderValue = Number.isFinite(parsedSliderValue) ? parsedSliderValue : min;

    return (
      <div className={pageStyles.inlineSliderControl}>
        <NumericSliderField
          inputValue={inlineCommandValue}
          sliderValue={sliderValue}
          min={min}
          max={max}
          step={getNumericStepForRule(rule)}
          commitPolicy={definition.commitPolicy}
          throttleMs={definition.throttleMs}
          placeholder={t("enterValue")}
          disabled={!canControlDevice || isBusy}
          onInputChange={(nextValue) => {
            updateInlineCommandValue(setInlineCommandValues, capability.id, nextValue);

            onScheduleInlineCommandSend(
              capability.capabilityId,
              capability.id,
              capability.endpointId,
              operation,
              rule,
              nextValue,
              definition.valuePath
            );
          }}
          onSliderChange={(nextValue) => {
            updateInlineCommandValue(
              setInlineCommandValues,
              capability.id,
              String(nextValue)
            );
          }}
          onSliderCommit={(nextValue) => {
            onLiveInlineCommandSend(
              capability.capabilityId,
              capability.id,
              capability.endpointId,
              operation,
              rule,
              String(nextValue),
              definition.valuePath
            );
          }}
        />
        <div className={pageStyles.capabilityHint}>{t("typeOrDrag")}</div>
      </div>
    );
  }

  if (definition.kind === "rgb" || definition.kind === "operationForm") {
    const fields = getOperationFieldsForCapability(capability, operation);
    const rule = getOperationRule(capability.operations, operation);
    const contextKey = getOperationContextKey(capability, operation);
    const fieldStates = buildOperationFieldStates(
      fields,
      advancedFieldValuesByContext[contextKey] ?? {},
      capability.state
    );

    return (
      <div className={pageStyles.inlineNumberControl}>
        <DeviceOperationCommandForm
          capability={capability}
          operation={operation}
          rule={rule}
          contextKey={contextKey}
          fields={fieldStates}
          disabled={!canControlDevice || isBusy}
          onChangeField={(path, nextValue) => {
            updateFieldValue(contextKey, path, nextValue);
          }}
          onInlineCommandSend={onInlineCommandSend}
        />
      </div>
    );
  }

  return null;
}

function FallbackCapabilityControls(props: Props) {
  const {
    capability,
    canControlDevice,
    quickToggleBusyCapabilityId,
    optimisticToggleValues,
    inlineCommandValues,
    setInlineCommandValues,
    expandedCapabilityId,
    setExpandedCapabilityId,
    advancedOperationByCapability,
    setAdvancedOperationByCapability,
    advancedFieldValuesByContext,
    setAdvancedFieldValuesByContext,
    onBooleanToggleSend,
    onScheduleInlineCommandSend,
    onInlineCommandSend,
    onLiveInlineCommandSend,
  } = props;
  const { t } = useTranslation("devices");
  const booleanLabels = getCapabilityBooleanLabels(t, CAPABILITY_LABEL_KEYS.device);
  const updateFieldValue = createFieldValueUpdater({ setAdvancedFieldValuesByContext });

  const primaryStatePath = capability.metadata?.primary?.state ?? null;
  const primaryOperationMetadata = capability.metadata?.primary?.operation ?? null;
  const { operation: primaryOperationReference, valuePath: primaryValuePathFromMetadata } =
    parsePrimaryOperationReference(primaryOperationMetadata);
  const primaryOperation = getSupportedOperation(capability, primaryOperationReference);
  const primaryOperationFields = primaryOperation
    ? getOperationFieldsForCapability(capability, primaryOperation)
    : [];
  const primaryRenderPlan = primaryOperation
    ? resolveOperationFieldRenderPlan(
      capability.capabilityId,
      primaryOperationFields,
      primaryOperation
    )
    : null;
  const canRenderPrimaryOperation = isOperationRenderPlanUsable(primaryRenderPlan);
  const explicitPrimaryField =
    primaryValuePathFromMetadata
      ? primaryOperationFields.find((field) => field.path === primaryValuePathFromMetadata)
      ?? null
      : null;
  const primaryFieldFromStatePath =
    !explicitPrimaryField && primaryStatePath
      ? primaryOperationFields.find((field) => field.path === primaryStatePath) ?? null
      : null;
  const inferredPrimaryField =
    !explicitPrimaryField &&
      !primaryFieldFromStatePath &&
      primaryOperationFields.length === 1
      ? primaryOperationFields[0]
      : null;
  const selectedPrimaryField =
    explicitPrimaryField ?? primaryFieldFromStatePath ?? inferredPrimaryField;
  const primaryValuePath =
    selectedPrimaryField?.path ?? primaryValuePathFromMetadata ?? null;
  const primaryRule = selectedPrimaryField?.rule ?? (primaryOperation
    ? getOperationRuleForPath(capability.operations, primaryOperation, primaryValuePath)
    : null);
  const primaryRuleType = normalizeRuleType(primaryRule);
  const canSend = canSendFromCapability(capability);
  const canQuickPrimaryMultiField =
    canSend &&
    Boolean(primaryOperation) &&
    canRenderPrimaryOperation &&
    primaryOperationFields.length >= 2;
  const canQuickToggle =
    canSend &&
    canRenderPrimaryOperation &&
    primaryRuleType === "boolean" &&
    !canQuickPrimaryMultiField;
  const canQuickNumberSet =
    canSend &&
    canRenderPrimaryOperation &&
    (primaryRuleType === "integer" || primaryRuleType === "number") &&
    !canQuickPrimaryMultiField;
  const canQuickScalarSet =
    canSend &&
    canRenderPrimaryOperation &&
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

  const primaryStateValue = getValueByPath(capability.state, primaryStatePath);
  const booleanStateValue = getBooleanValue(primaryStateValue);
  const numericStateValue = getNumericValue(primaryStateValue);
  const isBusy = quickToggleBusyCapabilityId === capability.id;
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
  const inlineValueValidation = validateCommandValue(primaryRule, inlineCommandValue);
  const primaryContextKey = `${capability.id}|__primary__|${primaryOperation ?? ""}`;
  const primaryFieldStates = canQuickPrimaryMultiField
    ? buildOperationFieldStates(
      primaryOperationFields,
      advancedFieldValuesByContext[primaryContextKey] ?? {},
      capability.state
    )
    : [];

  const normalizedPrimaryOperation = primaryOperation?.trim().toLowerCase();
  const advancedOperations = capability.supportedOperations
    .filter((operation) =>
      normalizedPrimaryOperation
        ? operation.trim().toLowerCase() !== normalizedPrimaryOperation
        : true
    )
    .filter((operation) => {
      const fields = getOperationFieldsForCapability(capability, operation);
      const renderPlan = resolveOperationFieldRenderPlan(
        capability.capabilityId,
        fields,
        operation
      );

      return isOperationRenderPlanUsable(renderPlan);
    });
  const canShowAdvancedControls = canSend && advancedOperations.length > 0;
  const isAdvancedOpen = expandedCapabilityId === capability.id;
  const selectedAdvancedOperationCandidate = advancedOperationByCapability[capability.id];
  const selectedAdvancedOperation =
    getSupportedOperation(capability, selectedAdvancedOperationCandidate)
    ?? advancedOperations[0]
    ?? "";
  const advancedRule = getOperationRule(capability.operations, selectedAdvancedOperation);
  const advancedFields = selectedAdvancedOperation
    ? getOperationFieldsForCapability(capability, selectedAdvancedOperation)
    : [];
  const advancedContextKey = `${capability.id}|${selectedAdvancedOperation}`;
  const advancedFieldStates = buildOperationFieldStates(
    advancedFields,
    advancedFieldValuesByContext[advancedContextKey] ?? {},
    capability.state
  );

  return (
    <>
      <div className={pageStyles.inlineControlArea}>
        {canQuickToggle && primaryOperation ? (
          <CapabilityBooleanControl
            capabilityId={capability.capabilityId}
            checked={Boolean(
              getDisplayedToggleValue(
                optimisticToggleValues,
                capability.id,
                booleanStateValue
              )
            )}
            disabled={!canControlDevice || isBusy || booleanStateValue === null}
            labels={booleanLabels}
            onChange={(nextChecked) => {
              const displayedValue = getDisplayedToggleValue(
                optimisticToggleValues,
                capability.id,
                booleanStateValue
              );

              onBooleanToggleSend(
                capability.capabilityId,
                capability.id,
                capability.endpointId,
                primaryOperation,
                nextChecked,
                displayedValue,
                primaryValuePath
              );
            }}
          />
        ) : useNumericSlider && primaryOperation ? (
          <div className={pageStyles.inlineSliderControl}>
            <NumericSliderField
              inputValue={inlineCommandValue}
              sliderValue={inlineSliderValue}
              min={numericMinValue}
              max={numericMaxValue}
              step={getNumericStepForRule(primaryRule)}
              placeholder={t("enterValue")}
              disabled={!canControlDevice || isBusy}
              onInputChange={(nextValue) => {
                updateInlineCommandValue(setInlineCommandValues, capability.id, nextValue);
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
                updateInlineCommandValue(
                  setInlineCommandValues,
                  capability.id,
                  String(nextValue)
                );
              }}
              onSliderCommit={(nextValue) => {
                onLiveInlineCommandSend(
                  capability.capabilityId,
                  capability.id,
                  capability.endpointId,
                  primaryOperation,
                  primaryRule,
                  String(nextValue),
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
                  updateInlineCommandValue(
                    setInlineCommandValues,
                    capability.id,
                    event.target.value
                  )
                }
                min={minValue}
                max={maxValue}
                step={getNumericStepForRule(primaryRule)}
                placeholder={t("enterValue")}
                disabled={!canControlDevice || isBusy}
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
                disabled={!canControlDevice || isBusy || Boolean(inlineValueValidation.errorKey)}
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
                    updateInlineCommandValue(
                      setInlineCommandValues,
                      capability.id,
                      event.target.value
                    )
                  }
                  disabled={!canControlDevice || isBusy}
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
                    updateInlineCommandValue(
                      setInlineCommandValues,
                      capability.id,
                      event.target.value
                    )
                  }
                  placeholder={t("enterValue")}
                  disabled={!canControlDevice || isBusy}
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
                disabled={!canControlDevice || isBusy || Boolean(inlineValueValidation.errorKey)}
              >
                {t("set")}
              </Button>
            </div>
          </div>
        ) : canQuickPrimaryMultiField && primaryOperation ? (
          <div className={pageStyles.inlineNumberControl}>
            <DeviceOperationCommandForm
              capability={capability}
              operation={primaryOperation}
              rule={primaryRule}
              contextKey={primaryContextKey}
              fields={primaryFieldStates}
              disabled={!canControlDevice || isBusy}
              onChangeField={(path, nextValue) => {
                updateFieldValue(primaryContextKey, path, nextValue);
              }}
              onInlineCommandSend={onInlineCommandSend}
            />
          </div>
        ) : null}

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
                  setAdvancedOperationByCapability((current) => ({
                    ...current,
                    [capability.id]: event.target.value,
                  }));
                }}
              >
                {advancedOperations.map((operation) => (
                  <option key={`${capability.id}-${operation}`} value={operation}>
                    {localizeCapabilityOperation(t, operation, CAPABILITY_LABEL_KEYS.device)}
                  </option>
                ))}
              </select>

              <DeviceOperationCommandForm
                capability={capability}
                operation={selectedAdvancedOperation}
                rule={advancedRule}
                contextKey={advancedContextKey}
                fields={advancedFieldStates}
                disabled={!canControlDevice || isBusy}
                requireOperation
                onChangeField={(path, nextValue) => {
                  updateFieldValue(advancedContextKey, path, nextValue);
                }}
                onInlineCommandSend={onInlineCommandSend}
              />
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
    </>
  );
}

export function DeviceCapabilityControls(props: Props) {
  const definition = getCapabilityDeviceControlDefinition(
    props.capability.capabilityId,
    props.capability.capabilityVersion
  );

  return (
    <div className={pageStyles.capabilityActions}>
      {definition ? (
        <div className={pageStyles.inlineControlArea}>
          <SpecificCapabilityControl definition={definition} props={props} />
        </div>
      ) : (
        <FallbackCapabilityControls {...props} />
      )}
    </div>
  );
}
