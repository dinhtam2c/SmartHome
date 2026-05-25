import { FormGroup } from "@/shared/ui/FormGroup";
import { CapabilityBooleanControl } from "./CapabilityBooleanControl";
import {
  CapabilityColorControl,
  CapabilityFieldControl,
} from "./CapabilityFieldControl";
import {
  getRgbHex,
  getSchemaFieldRenderKind,
  parseRgbHex,
  RGB_CHANNELS,
  resolveCapabilityFieldEditorRender,
  type CapabilityBooleanLabels,
  type RgbChannel,
  type SchemaField,
} from "@/features/capabilities";
import styles from "./CapabilitySchemaFieldsEditor.module.css";

type Props = {
  capabilityId: string;
  operation?: string | null;
  idPrefix?: string;
  fields: SchemaField[];
  disabled?: boolean;
  getValue: (path: string) => unknown;
  setValue: (path: string, value: unknown) => void;
  clearValue: (path: string) => void;
  getFieldLabel: (path: string) => string;
  labels: {
    clear: string;
    rgbColor: string;
  } & CapabilityBooleanLabels;
};

function getRgbRootPath(paths: Record<RgbChannel, string>) {
  const rootPaths = RGB_CHANNELS.map((channel) => {
    const path = paths[channel].trim();
    const suffix = `.${channel}`;

    if (path === channel) {
      return "";
    }

    return path.endsWith(suffix) ? path.slice(0, -suffix.length) : path;
  });

  const uniquePaths = new Set(rootPaths);
  return uniquePaths.size === 1 ? rootPaths[0] : null;
}

function mergeRgbRootValue(currentValue: unknown, nextRgb: Record<RgbChannel, number>) {
  if (typeof currentValue === "object" && currentValue !== null && !Array.isArray(currentValue)) {
    return { ...(currentValue as Record<string, unknown>), ...nextRgb };
  }

  return nextRgb;
}

function getNumericFieldValue(field: SchemaField | undefined, value: unknown) {
  if (typeof value === "number" && Number.isFinite(value)) {
    return value;
  }

  if (typeof value === "string") {
    const parsed = Number(value.trim());
    if (Number.isFinite(parsed)) {
      return parsed;
    }
  }

  return field?.min ?? 0;
}

export function CapabilitySchemaFieldsEditor({
  capabilityId,
  operation,
  idPrefix,
  fields,
  disabled = false,
  getValue,
  setValue,
  clearValue,
  getFieldLabel,
  labels,
}: Props) {
  const controlIdPrefix = idPrefix ?? capabilityId;
  const renderPlan = resolveCapabilityFieldEditorRender(
    capabilityId,
    fields,
    operation
  );

  if (renderPlan.kind === "unsupported") {
    return null;
  }

  if (renderPlan.kind === "lock") {
    const { field } = renderPlan;
    const fieldId = `schema-field-${controlIdPrefix}-${field.path || "value"}`;
    const fieldValue = getValue(field.path);
    const checked =
      typeof fieldValue === "boolean"
        ? fieldValue
        : String(fieldValue).trim().toLowerCase() === "true";

    return (
      <div className={styles.fieldGrid}>
        <FormGroup
          label={getFieldLabel(field.path)}
          htmlFor={fieldId}
          required={field.required}
        >
          <div className={styles.booleanToggle}>
            <CapabilityBooleanControl
              capabilityId={capabilityId}
              id={fieldId}
              checked={checked}
              disabled={disabled}
              labels={labels}
              onChange={(nextValue) => setValue(field.path, nextValue)}
            />

            {!field.required ? (
              <button
                type="button"
                className={styles.clearFieldButton}
                onClick={(event) => {
                  event.preventDefault();
                  clearValue(field.path);
                }}
                disabled={disabled}
              >
                {labels.clear}
              </button>
            ) : null}
          </div>
        </FormGroup>
      </div>
    );
  }

  if (renderPlan.kind === "rgb") {
    const { channelPaths } = renderPlan;
    const rgbRootPath = getRgbRootPath(channelPaths);
    const channelValues = Object.fromEntries(
      RGB_CHANNELS.map((channel) => {
        const path = channelPaths[channel];
        const field = fields.find((candidate) => candidate.path === path);
        const value = getNumericFieldValue(field ?? fields[0], getValue(path));

        return [channel, value];
      })
    ) as Record<RgbChannel, number>;
    const colorValue = getRgbHex(channelValues) ?? "#000000";

    return (
      <div className={styles.fieldGrid}>
        <div className={styles.rgbField}>
          <FormGroup
            label={labels.rgbColor}
            htmlFor={`rgb-color-${controlIdPrefix}`}
          >
            <div className={styles.rgbEditor}>
              <CapabilityColorControl
                id={`rgb-color-${controlIdPrefix}`}
                className={styles.colorInput}
                value={colorValue}
                disabled={disabled}
                label={labels.rgbColor}
                onChange={(event) => {
                  const nextRgb = parseRgbHex(event);
                  if (!nextRgb) {
                    return;
                  }

                  if (rgbRootPath !== null) {
                    const currentValue = getValue(rgbRootPath);
                    setValue(rgbRootPath, mergeRgbRootValue(currentValue, nextRgb));
                    return;
                  }

                  RGB_CHANNELS.forEach((channel) => {
                    setValue(channelPaths[channel], nextRgb[channel]);
                  });
                }}
              />
            </div>
          </FormGroup>
        </div>
      </div>
    );
  }

  return (
    <div className={styles.fieldGrid}>
      {renderPlan.fields.map((field) => {
        const fieldId = `schema-field-${controlIdPrefix}-${field.path || "value"}`;
        const fieldValue = getValue(field.path);
        const fieldLabel = getFieldLabel(field.path);
        const renderKind = getSchemaFieldRenderKind(field);

        if (renderKind === "unsupported") {
          return null;
        }

        return (
          <FormGroup
            key={fieldId}
            label={fieldLabel}
            htmlFor={fieldId}
            required={field.required}
          >
            <div className={renderKind === "boolean" ? styles.booleanToggle : undefined}>
              <CapabilityFieldControl
                capabilityId={capabilityId}
                kind={renderKind}
                id={fieldId}
                value={fieldValue === undefined ? "" : String(fieldValue)}
                ariaLabel={fieldLabel}
                enumValues={field.enumValues}
                allowEmpty
                min={field.min}
                max={field.max}
                step={field.step ?? (field.type === "integer" ? 1 : undefined)}
                sliderValue={getNumericFieldValue(field, fieldValue)}
                selectClassName={styles.select}
                disabled={disabled}
                labels={labels}
                onBooleanChange={(nextValue) => setValue(field.path, nextValue)}
                onSliderChange={(nextValue) => {
                  setValue(
                    field.path,
                    field.type === "integer" ? Math.trunc(nextValue) : nextValue
                  );
                }}
                onRawChange={(nextRawValue) => {
                  if (nextRawValue.trim() === "") {
                    clearValue(field.path);
                    return;
                  }

                  if (renderKind === "number" || renderKind === "numeric-slider") {
                    const parsed = Number(nextRawValue);
                    if (!Number.isFinite(parsed)) {
                      return;
                    }

                    setValue(
                      field.path,
                      field.type === "integer" ? Math.trunc(parsed) : parsed
                    );
                    return;
                  }

                  setValue(field.path, nextRawValue);
                }}
              />

              {renderKind === "boolean" && !field.required ? (
                <button
                  type="button"
                  className={styles.clearFieldButton}
                  onClick={(event) => {
                    event.preventDefault();
                    clearValue(field.path);
                  }}
                  disabled={disabled}
                >
                  {labels.clear}
                </button>
              ) : null}
            </div>
          </FormGroup>
        );
      })}
    </div>
  );
}
