import styles from "./BooleanSwitch.module.css";

type Props = {
  id?: string;
  checked: boolean;
  disabled?: boolean;
  label: string;
  onChange: (checked: boolean) => void;
};

export function BooleanSwitch({
  id,
  checked,
  disabled = false,
  label,
  onChange,
}: Props) {
  return (
    <label className={styles.switch}>
      <input
        id={id}
        type="checkbox"
        checked={checked}
        disabled={disabled}
        onChange={(event) => onChange(event.target.checked)}
      />
      <span className={styles.track}>
        <span className={styles.thumb} />
      </span>
      <span className={styles.text}>{label}</span>
    </label>
  );
}
