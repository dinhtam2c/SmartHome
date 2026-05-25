import {
  CapabilityColorControl,
  CapabilityFieldControl,
} from "@/features/capabilities/components/CapabilityFieldControl";
import {
  getRgbHex,
  getSchemaFieldRenderKind,
  parseRgbHex,
  RGB_CHANNELS,
  type CapabilityBooleanLabels,
  type RgbChannel,
} from "@/features/capabilities";
import styles from "@/shared/styles/featurePage.module.css";
import pageStyles from "../pages/DeviceDetailPage.module.css";
import {
  getNumericStepForRule,
  normalizeRuleType,
} from "../services/deviceCapabilityService";
import {
  resolveOperationFieldRenderPlan,
  type OperationFieldState,
} from "../services/deviceOperationFieldService";

type Props = {
  capabilityId: string;
  operation?: string | null;
  contextKey: string;
  fields: OperationFieldState[];
  disabled?: boolean;
  getFieldLabel: (path: string) => string;
  onChangeField: (path: string, rawValue: string) => void;
  labels: {
    enterValue: string;
    rgbColor: string;
    validationError: (errorKey: string, options?: Record<string, unknown>) => string;
  } & CapabilityBooleanLabels;
  onRgbChange?: (nextRgb: Record<RgbChannel, number>) => void;
};

function getFieldSliderBounds(field: OperationFieldState) {
  return {
    min: typeof field.rule?.min === "number" ? field.rule.min : 0,
    max: typeof field.rule?.max === "number" ? field.rule.max : 100,
  };
}

function getFieldSliderValue(field: OperationFieldState) {
  const { min } = getFieldSliderBounds(field);
  const parsedValue = Number(field.rawValue);

  return Number.isFinite(parsedValue) ? parsedValue : min;
}

export function DeviceOperationFieldsEditor({
  capabilityId,
  operation,
  contextKey,
  fields,
  disabled = false,
  getFieldLabel,
  onChangeField,
  labels,
  onRgbChange,
}: Props) {
  const renderPlan = resolveOperationFieldRenderPlan(
    capabilityId,
    fields,
    operation
  );

  if (renderPlan.kind === "unsupported") {
    return null;
  }

  if (renderPlan.kind === "rgb") {
    const { channelPaths } = renderPlan;
    const channelValues = Object.fromEntries(
      RGB_CHANNELS.map((channel) => {
        const field = fields.find((candidate) => candidate.path === channelPaths[channel]);
        return [channel, field ? getFieldSliderValue(field) : 0];
      })
    ) as Record<RgbChannel, number>;
    const colorValue = getRgbHex(channelValues) ?? "#000000";

    return (
      <div className={pageStyles.inlineNumberControl}>
        <label className={pageStyles.filterLabel} htmlFor={`${contextKey}-rgb-color`}>
          {labels.rgbColor}
        </label>
        <CapabilityColorControl
          id={`${contextKey}-rgb-color`}
          value={colorValue}
          disabled={disabled}
          label={labels.rgbColor}
          onChange={(nextValue) => {
            const nextRgb = parseRgbHex(nextValue);
            if (!nextRgb) {
              return;
            }

            RGB_CHANNELS.forEach((channel) => {
              onChangeField(channelPaths[channel], String(nextRgb[channel]));
            });

            onRgbChange?.(nextRgb);
          }}
        />
      </div>
    );
  }

  const fieldsToRender = renderPlan.kind === "lock"
    ? [renderPlan.field]
    : renderPlan.fields;

  return (
    <>
      {fieldsToRender.map((field) => {
        const inputId = `${contextKey}-${field.path || "value"}`;
        const normalizedFieldType = normalizeRuleType(field.rule);
        const enumValues = Array.isArray(field.rule?.enumValues)
          ? field.rule.enumValues
          : [];
        const fieldLabel = getFieldLabel(field.path);
        const fieldKind = getSchemaFieldRenderKind({
          path: field.path,
          type: normalizedFieldType,
          enumValues,
          min: field.rule?.min,
          max: field.rule?.max,
        });

        return (
          <div key={`${contextKey}:${field.path}`} className={pageStyles.inlineNumberControl}>
            <label className={pageStyles.filterLabel} htmlFor={inputId}>
              {fieldLabel}
              {field.required ? " *" : ""}
            </label>

            {fieldKind !== "unsupported" ? (
              <CapabilityFieldControl
                capabilityId={capabilityId}
                kind={fieldKind}
                id={inputId}
                value={field.rawValue}
                ariaLabel={fieldLabel}
                disabled={disabled}
                enumValues={enumValues}
                allowEmpty={!field.required}
                min={field.rule?.min}
                max={field.rule?.max}
                step={getNumericStepForRule(field.rule)}
                sliderValue={getFieldSliderValue(field)}
                selectClassName={styles.select}
                placeholder={labels.enterValue}
                onSliderChange={(nextValue) => {
                  const nextRawValue =
                    normalizedFieldType === "integer"
                      ? String(Math.trunc(nextValue))
                      : String(nextValue);

                  onChangeField(field.path, nextRawValue);
                }}
                labels={labels}
                onRawChange={(nextValue) => onChangeField(field.path, nextValue)}
              />
            ) : null}

            {field.validation.errorKey ? (
              <div className={pageStyles.capabilityHint}>
                {labels.validationError(field.validation.errorKey, field.validation.errorOptions)}
              </div>
            ) : null}
          </div>
        );
      })}
    </>
  );
}
