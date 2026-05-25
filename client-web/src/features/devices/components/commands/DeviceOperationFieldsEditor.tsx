import { useEffect, useRef } from "react";
import {
  CapabilityColorControl,
  CapabilityFieldControl,
  LightEffectFlashFieldsEditor,
} from "@/features/capabilities";
import {
  getRgbHex,
  getSchemaFieldRenderKind,
  parseRgbHex,
  RGB_CHANNELS,
  type CapabilityControlCommitPolicy,
  type CapabilityBooleanLabels,
  type RgbChannel,
} from "@/features/capabilities";
import styles from "@/shared/styles/featurePage.module.css";
import pageStyles from "../capability/DeviceCapability.module.css";
import {
  getNumericStepForRule,
  normalizeRuleType,
} from "../../services/deviceCapabilityService";
import {
  resolveOperationFieldRenderPlan,
  type OperationFieldState,
} from "../../services/deviceOperationFieldService";

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
  rgbCommitPolicy?: CapabilityControlCommitPolicy;
  rgbThrottleMs?: number;
  onRgbChange?: (nextRgb: Record<RgbChannel, number>) => void;
  onRgbCommit?: (nextRgb: Record<RgbChannel, number>) => void;
};

const DEFAULT_RGB_THROTTLE_MS = 100;

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

function getRgbCommitKey(value: Record<RgbChannel, number>) {
  return RGB_CHANNELS.map((channel) => value[channel]).join(":");
}

function getFieldByPath(fields: OperationFieldState[], path: string) {
  const normalizedPath = path.trim().toLowerCase();
  return fields.find((field) => field.path.trim().toLowerCase() === normalizedPath);
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
  rgbCommitPolicy = "immediate",
  rgbThrottleMs,
  onRgbChange,
  onRgbCommit,
}: Props) {
  const renderPlan = resolveOperationFieldRenderPlan(
    capabilityId,
    fields,
    operation
  );
  const latestRgbCommitValueRef = useRef<Record<RgbChannel, number> | null>(null);
  const lastCommittedRgbKeyRef = useRef<string | null>(null);
  const rgbCommitTimerRef = useRef<ReturnType<typeof setTimeout> | null>(null);

  const clearRgbCommitTimer = () => {
    if (rgbCommitTimerRef.current) {
      clearTimeout(rgbCommitTimerRef.current);
      rgbCommitTimerRef.current = null;
    }
  };

  const commitRgbValue = (nextRgb: Record<RgbChannel, number>) => {
    if (!onRgbCommit) {
      return;
    }

    const commitKey = getRgbCommitKey(nextRgb);
    if (lastCommittedRgbKeyRef.current === commitKey) {
      return;
    }

    lastCommittedRgbKeyRef.current = commitKey;
    onRgbCommit(nextRgb);
  };

  const scheduleRgbCommit = (nextRgb: Record<RgbChannel, number>) => {
    if (!onRgbCommit || rgbCommitPolicy === "formOnly") {
      return;
    }

    latestRgbCommitValueRef.current = nextRgb;

    if (rgbCommitPolicy === "commitOnRelease") {
      return;
    }

    if (rgbCommitPolicy === "immediate") {
      clearRgbCommitTimer();
      commitRgbValue(nextRgb);
      return;
    }

    clearRgbCommitTimer();
    rgbCommitTimerRef.current = setTimeout(() => {
      rgbCommitTimerRef.current = null;
      const latestRgbValue = latestRgbCommitValueRef.current;

      if (latestRgbValue) {
        commitRgbValue(latestRgbValue);
      }
    }, Math.max(0, rgbThrottleMs ?? DEFAULT_RGB_THROTTLE_MS));
  };

  const flushRgbCommit = (nextRgb?: Record<RgbChannel, number>) => {
    if (!onRgbCommit || rgbCommitPolicy === "formOnly") {
      return;
    }

    clearRgbCommitTimer();

    const rgbValue = nextRgb ?? latestRgbCommitValueRef.current;
    if (rgbValue) {
      latestRgbCommitValueRef.current = rgbValue;
      commitRgbValue(rgbValue);
    }
  };

  const applyRgbChange = (
    channelPaths: Record<RgbChannel, string>,
    nextValue: string,
    shouldCommit: boolean
  ) => {
    const nextRgb = parseRgbHex(nextValue);
    if (!nextRgb) {
      return;
    }

    RGB_CHANNELS.forEach((channel) => {
      onChangeField(channelPaths[channel], String(nextRgb[channel]));
    });

    onRgbChange?.(nextRgb);

    if (shouldCommit) {
      flushRgbCommit(nextRgb);
      return;
    }

    scheduleRgbCommit(nextRgb);
  };

  useEffect(() => {
    return () => {
      if (rgbCommitTimerRef.current) {
        clearTimeout(rgbCommitTimerRef.current);
      }
    };
  }, []);

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
          onChange={(nextValue) => applyRgbChange(channelPaths, nextValue, false)}
          onCommit={(nextValue) => applyRgbChange(channelPaths, nextValue, true)}
        />
      </div>
    );
  }

  if (renderPlan.kind === "lightEffectFlash") {
    const { fieldPaths } = renderPlan;
    const buildNumericField = (path: string) => {
      const field = getFieldByPath(fields, path) ?? fields[0];
      const normalizedFieldType = normalizeRuleType(field?.rule ?? null);
      const inputId = `${contextKey}-${path}`;

      return {
        path,
        inputId,
        label: getFieldLabel(path),
        required: field?.required ?? false,
        value: field?.rawValue ?? "",
        min: field?.rule?.min,
        max: field?.rule?.max,
        step: getNumericStepForRule(field?.rule ?? null),
        sliderValue: field ? getFieldSliderValue(field) : 0,
        validation: field?.validation.errorKey ? (
          <div className={pageStyles.capabilityHint}>
            {labels.validationError(field.validation.errorKey, field.validation.errorOptions)}
          </div>
        ) : null,
        onSliderChange: (nextValue: number) => {
          const nextRawValue =
            normalizedFieldType === "integer"
              ? String(Math.trunc(nextValue))
              : String(nextValue);

          onChangeField(path, nextRawValue);
        },
        onRawChange: (nextValue: string) => onChangeField(path, nextValue),
      };
    };
    const colorValue = Object.fromEntries(
      RGB_CHANNELS.map((channel) => {
        const field = getFieldByPath(fields, fieldPaths.color[channel]);

        return [channel, field ? getFieldSliderValue(field) : 0];
      })
    ) as Record<RgbChannel, number>;
    const colorRequired = RGB_CHANNELS.some((channel) =>
      Boolean(getFieldByPath(fields, fieldPaths.color[channel])?.required)
    );

    return (
      <LightEffectFlashFieldsEditor
        capabilityId={capabilityId}
        disabled={disabled}
        fields={{
          count: buildNumericField(fieldPaths.count),
          intervalMs: buildNumericField(fieldPaths.intervalMs),
        }}
        color={{
          inputId: `${contextKey}-color`,
          label: labels.rgbColor,
          required: colorRequired,
          value: colorValue,
          onChange: (nextRgb) => {
            RGB_CHANNELS.forEach((channel) => {
              onChangeField(fieldPaths.color[channel], String(nextRgb[channel]));
            });
          },
        }}
        labels={labels}
        layout={{
          variant: "inline",
          fieldClassName: pageStyles.inlineNumberControl,
          labelClassName: pageStyles.filterLabel,
          selectClassName: styles.select,
        }}
      />
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
