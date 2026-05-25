import { useRef } from "react";
import styles from "./ColorPickerButton.module.css";

type Props = {
  id: string;
  value: string;
  disabled?: boolean;
  ariaLabel?: string;
  onChange: (nextValue: string) => void;
  onCommit?: (nextValue: string) => void;
  className?: string;
};

export function ColorPickerButton({
  id,
  value,
  disabled = false,
  ariaLabel,
  onChange,
  onCommit,
  className,
}: Props) {
  const pendingValueRef = useRef<string | null>(null);

  const notifyChange = (nextValue: string) => {
    if (pendingValueRef.current === nextValue) {
      return;
    }

    pendingValueRef.current = nextValue;
    onChange(nextValue);
  };

  const commitValue = (nextValue: string) => {
    if (pendingValueRef.current !== nextValue) {
      return;
    }

    pendingValueRef.current = null;
    onCommit?.(nextValue);
  };

  return (
    <input
      id={id}
      className={`${styles.button}${className ? ` ${className}` : ""}`}
      type="color"
      value={value}
      disabled={disabled}
      aria-label={ariaLabel ?? "Select color"}
      title={value}
      onInput={(event) => notifyChange(event.currentTarget.value)}
      onChange={(event) => notifyChange(event.currentTarget.value)}
      onBlur={(event) => commitValue(event.currentTarget.value)}
      onKeyUp={(event) => commitValue(event.currentTarget.value)}
      onPointerCancel={(event) => commitValue(event.currentTarget.value)}
      onPointerUp={(event) => commitValue(event.currentTarget.value)}
    />
  );
}
