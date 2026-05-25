import styles from "./LockStateToggle.module.css";

type Props = {
  id?: string;
  checked: boolean;
  disabled?: boolean;
  label: string;
  onChange: (checked: boolean) => void;
};

export function LockStateToggle({
  id,
  checked,
  disabled = false,
  label,
  onChange,
}: Props) {
  return (
    <button
      id={id}
      type="button"
      className={[
        styles.toggle,
        checked ? styles.toggleLocked : styles.toggleUnlocked,
      ].join(" ")}
      aria-pressed={checked}
      disabled={disabled}
      onClick={() => onChange(!checked)}
    >
      <span
        className={[
          styles.icon,
          checked ? null : styles.iconUnlocked,
        ].filter(Boolean).join(" ")}
        aria-hidden="true"
      />
      <span className={styles.label}>{label}</span>
    </button>
  );
}
