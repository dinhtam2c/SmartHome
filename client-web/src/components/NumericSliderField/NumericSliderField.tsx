import { Input } from "@/components/Input";
import styles from "./NumericSliderField.module.css";

type Props = {
  id?: string;
  inputValue: string;
  sliderValue: number;
  min: number;
  max: number;
  step?: number;
  disabled?: boolean;
  placeholder?: string;
  onInputChange: (value: string) => void;
  onSliderChange: (value: number) => void;
};

export function NumericSliderField({
  id,
  inputValue,
  sliderValue,
  min,
  max,
  step,
  disabled = false,
  placeholder,
  onInputChange,
  onSliderChange,
}: Props) {
  return (
    <div className={styles.field}>
      <Input
        id={id}
        type="number"
        value={inputValue}
        min={min}
        max={max}
        step={step}
        disabled={disabled}
        placeholder={placeholder}
        onChange={(event) => onInputChange(event.target.value)}
      />

      <input
        className={styles.range}
        type="range"
        min={min}
        max={max}
        step={step}
        value={sliderValue}
        disabled={disabled}
        onChange={(event) => {
          const nextValue = Number(event.target.value);
          if (!Number.isFinite(nextValue)) {
            return;
          }

          onSliderChange(nextValue);
        }}
      />
    </div>
  );
}
