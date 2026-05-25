import { useTranslation } from "react-i18next";
import { NumericSliderField } from "@/shared/ui/NumericSliderField";
import {
  CAPABILITY_LABEL_KEYS,
  getCapabilityBooleanLabel,
  getCapabilityBooleanLabels,
  getValueByPath,
  type CapabilityDeviceControlDefinition,
} from "@/features/capabilities";
import { CapabilityBooleanControl } from "@/features/capabilities";
import {
  getDefaultNumericValue,
  getNumericStepForRule,
  getOperationRule,
  getOperationRuleForPath,
} from "../../services/deviceCapabilityService";
import { buildOperationFieldStates } from "../../services/deviceOperationFieldService";
import { DeviceOperationCommandForm } from "../commands/DeviceOperationCommandForm";
import type { DeviceCapabilityControlsProps } from "./deviceCapabilityControlTypes";
import {
  canSendFromCapability,
  createFieldValueUpdater,
  getBooleanValue,
  getDisplayedToggleValue,
  getNumericValue,
  getOperationContextKey,
  getOperationFieldsForCapability,
  getSupportedOperation,
  updateInlineCommandValue,
} from "./deviceCapabilityControlUtils";
import pageStyles from "./DeviceCapability.module.css";

export function SpecificCapabilityControl({
  definition,
  props,
}: {
  definition: CapabilityDeviceControlDefinition;
  props: DeviceCapabilityControlsProps;
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
          rgbCommitPolicy={definition.kind === "rgb" ? definition.commitPolicy : undefined}
          rgbThrottleMs={definition.kind === "rgb" ? definition.throttleMs : undefined}
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
