import styles from "./StatusChip.module.css";

type StatusTone =
  | "online"
  | "offline"
  | "pending"
  | "failed"
  | "completed"
  | "neutral";

type Props = {
  label: string;
  tone: StatusTone;
  className?: string;
};

export function StatusChip({ label, tone, className }: Props) {
  return (
    <span className={[styles.chip, styles[tone], className].filter(Boolean).join(" ")}>
      {label}
    </span>
  );
}
