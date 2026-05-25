import type { ReactNode } from "react";
import { FormGroup } from "@/shared/ui/FormGroup";
import {
  CapabilityColorControl,
  CapabilityFieldControl,
} from "../controls/CapabilityFieldControl";
import {
  getRgbHex,
  parseRgbHex,
  type RgbChannel,
} from "../../services/rgbCapabilityService";
import {
  type CapabilityBooleanLabels,
} from "../../registry/capabilityRenderRegistry";
import styles from "./CapabilitySchemaFieldsEditor.module.css";

export type LightEffectFlashNumericField = {
  path: string;
  inputId: string;
  label: string;
  required: boolean;
  value: string;
  min?: number | null;
  max?: number | null;
  step?: number | null;
  sliderValue: number;
  validation?: ReactNode;
  onRawChange: (nextValue: string) => void;
  onSliderChange: (nextValue: number) => void;
};

type LightEffectFlashColorField = {
  inputId: string;
  label: string;
  required: boolean;
  value: Record<RgbChannel, number>;
  onChange: (nextRgb: Record<RgbChannel, number>) => void;
};

type LightEffectFlashLayout =
  | {
    variant: "schema";
    selectClassName?: string;
  }
  | {
    variant: "inline";
    fieldClassName: string;
    labelClassName: string;
    selectClassName?: string;
  };

type Props = {
  capabilityId: string;
  disabled?: boolean;
  fields: {
    count: LightEffectFlashNumericField;
    intervalMs: LightEffectFlashNumericField;
  };
  color: LightEffectFlashColorField;
  labels: CapabilityBooleanLabels;
  layout: LightEffectFlashLayout;
};

function getNumericFieldKind(field: LightEffectFlashNumericField) {
  return typeof field.min === "number" &&
    typeof field.max === "number" &&
    field.min <= field.max
    ? "numeric-slider"
    : "number";
}

export function LightEffectFlashFieldsEditor({
  capabilityId,
  disabled = false,
  fields,
  color,
  labels,
  layout,
}: Props) {
  const renderNumericField = (field: LightEffectFlashNumericField) => {
    const control = (
      <>
        <CapabilityFieldControl
          capabilityId={capabilityId}
          kind={getNumericFieldKind(field)}
          id={field.inputId}
          value={field.value}
          ariaLabel={field.label}
          allowEmpty={!field.required}
          min={field.min}
          max={field.max}
          step={field.step ?? undefined}
          sliderValue={field.sliderValue}
          selectClassName={layout.selectClassName}
          disabled={disabled}
          labels={labels}
          onSliderChange={field.onSliderChange}
          onRawChange={field.onRawChange}
        />

        {field.validation}
      </>
    );

    if (layout.variant === "schema") {
      return (
        <FormGroup
          key={field.path}
          label={field.label}
          htmlFor={field.inputId}
          required={field.required}
        >
          {control}
        </FormGroup>
      );
    }

    return (
      <div key={field.path} className={layout.fieldClassName}>
        <label className={layout.labelClassName} htmlFor={field.inputId}>
          {field.label}
          {field.required ? " *" : ""}
        </label>

        {control}
      </div>
    );
  };

  const renderColorField = () => {
    const colorValue = getRgbHex(color.value) ?? "#000000";
    const control = (
      <CapabilityColorControl
        id={color.inputId}
        className={layout.variant === "schema" ? styles.colorInput : undefined}
        value={colorValue}
        disabled={disabled}
        label={color.label}
        onChange={(nextValue) => {
          const nextRgb = parseRgbHex(nextValue);
          if (!nextRgb) {
            return;
          }

          color.onChange(nextRgb);
        }}
      />
    );

    if (layout.variant === "schema") {
      return (
        <FormGroup
          key="color"
          label={color.label}
          htmlFor={color.inputId}
          required={color.required}
        >
          <div className={styles.rgbEditor}>{control}</div>
        </FormGroup>
      );
    }

    return (
      <div key="color" className={layout.fieldClassName}>
        <label className={layout.labelClassName} htmlFor={color.inputId}>
          {color.label}
          {color.required ? " *" : ""}
        </label>

        {control}
      </div>
    );
  };

  const content = (
    <>
      {renderNumericField(fields.count)}
      {renderNumericField(fields.intervalMs)}
      {renderColorField()}
    </>
  );

  return layout.variant === "schema" ? (
    <div className={styles.fieldGrid}>{content}</div>
  ) : (
    content
  );
}
