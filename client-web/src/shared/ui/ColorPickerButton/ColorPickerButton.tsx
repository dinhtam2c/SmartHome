import styles from "./ColorPickerButton.module.css";

type Props = {
  id: string;
  value: string;
  disabled?: boolean;
  ariaLabel?: string;
  onChange: (nextValue: string) => void;
  className?: string;
};

export function ColorPickerButton({
  id,
  value,
  disabled = false,
  ariaLabel,
  onChange,
  className,
}: Props) {
  return (
    <input
      id={id}
      className={`${styles.button}${className ? ` ${className}` : ""}`}
      type="color"
      value={value}
      disabled={disabled}
      aria-label={ariaLabel ?? "Select color"}
      title={value}
      onChange={(event) => onChange(event.target.value)}
    />
  );
}
