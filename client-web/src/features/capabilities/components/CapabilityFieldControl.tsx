import { ColorPickerButton } from "@/shared/ui/ColorPickerButton";
import { Input } from "@/shared/ui/Input";
import { NumericSliderField } from "@/shared/ui/NumericSliderField";
import {
  type CapabilityBooleanLabels,
  type FieldRenderKind,
} from "@/features/capabilities";
import { CapabilityBooleanControl } from "./CapabilityBooleanControl";

type BaseControlProps = {
  id: string;
  disabled?: boolean;
};

type CapabilityColorControlProps = BaseControlProps & {
  value: string;
  label: string;
  className?: string;
  onChange: (nextValue: string) => void;
};

type CapabilityFieldControlProps = BaseControlProps & {
  capabilityId: string;
  kind: Exclude<FieldRenderKind, "unsupported">;
  value: string;
  ariaLabel?: string;
  enumValues?: unknown[];
  allowEmpty?: boolean;
  min?: number | null;
  max?: number | null;
  step?: number;
  sliderValue?: number;
  selectClassName?: string;
  placeholder?: string;
  labels: CapabilityBooleanLabels;
  onRawChange: (nextValue: string) => void;
  onBooleanChange?: (nextValue: boolean) => void;
  onSliderChange?: (nextValue: number) => void;
};

const COLOR_INPUT_FALLBACK = "#000000";

function toColorInputValue(value: string) {
  const normalizedValue = value.trim();
  return /^#[0-9a-fA-F]{6}$/.test(normalizedValue)
    ? normalizedValue
    : COLOR_INPUT_FALLBACK;
}

export function CapabilityColorControl({
  id,
  value,
  label,
  disabled = false,
  className,
  onChange,
}: CapabilityColorControlProps) {
  return (
    <ColorPickerButton
      id={id}
      value={value}
      disabled={disabled}
      ariaLabel={label}
      className={className}
      onChange={onChange}
    />
  );
}

export function CapabilityFieldControl({
  capabilityId,
  kind,
  id,
  value,
  ariaLabel,
  enumValues = [],
  allowEmpty = false,
  min,
  max,
  step,
  sliderValue,
  selectClassName,
  placeholder,
  labels,
  disabled = false,
  onRawChange,
  onBooleanChange,
  onSliderChange,
}: CapabilityFieldControlProps) {
  if (kind === "boolean") {
    const checked = value.trim().toLowerCase() === "true";
    const handleChange = (nextValue: boolean) => {
      if (onBooleanChange) {
        onBooleanChange(nextValue);
        return;
      }

      onRawChange(String(nextValue));
    };

    return (
      <CapabilityBooleanControl
        capabilityId={capabilityId}
        id={id}
        checked={checked}
        disabled={disabled}
        labels={labels}
        onChange={handleChange}
      />
    );
  }

  if (kind === "color") {
    return (
      <CapabilityColorControl
        id={id}
        value={toColorInputValue(value)}
        label={ariaLabel ?? id}
        disabled={disabled}
        onChange={onRawChange}
      />
    );
  }

  if (kind === "enum") {
    return (
      <select
        id={id}
        className={selectClassName}
        value={value}
        disabled={disabled}
        onChange={(event) => onRawChange(event.target.value)}
      >
        {allowEmpty ? <option value="">-</option> : null}
        {enumValues.map((enumValue, enumIndex) => (
          <option
            key={`${id}:${String(enumValue)}:${enumIndex}`}
            value={String(enumValue)}
          >
            {String(enumValue)}
          </option>
        ))}
      </select>
    );
  }

  if (kind === "numeric-slider") {
    const numericMin = typeof min === "number" ? min : 0;
    const numericMax = typeof max === "number" ? max : 100;
    const parsedValue = Number(value);

    return (
      <NumericSliderField
        id={id}
        inputValue={value}
        sliderValue={
          typeof sliderValue === "number" && Number.isFinite(sliderValue)
            ? sliderValue
            : Number.isFinite(parsedValue)
              ? parsedValue
              : numericMin
        }
        min={numericMin}
        max={numericMax}
        step={step}
        placeholder={placeholder}
        disabled={disabled}
        onInputChange={onRawChange}
        onSliderChange={(nextValue) => {
          if (onSliderChange) {
            onSliderChange(nextValue);
            return;
          }

          onRawChange(String(nextValue));
        }}
      />
    );
  }

  return (
    <Input
      id={id}
      type={kind === "number" ? "number" : "text"}
      value={value}
      min={min ?? undefined}
      max={max ?? undefined}
      step={kind === "number" ? step : undefined}
      placeholder={placeholder}
      disabled={disabled}
      onChange={(event) => onRawChange(event.target.value)}
    />
  );
}
