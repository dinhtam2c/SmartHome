import type { ReactNode } from "react";
import styles from "./Cell.module.css";

interface Props {
  id: string;
  title: ReactNode;
  subtitle?: string | ReactNode;
  onClick: (id: string) => void;
  disabled: boolean;
  onPointerDown?: (id: string) => void;
  onPointerUp?: () => void;
  onPointerCancel?: () => void;
  onPointerLeave?: () => void;
}

export function Cell({
  id,
  title,
  subtitle,
  onClick,
  disabled = false,
  onPointerDown,
  onPointerUp,
  onPointerCancel,
  onPointerLeave,
}: Props) {
  return (
    <button
      className={styles.cell}
      disabled={disabled}
      onClick={() => onClick(id)}
      onPointerDown={onPointerDown ? () => onPointerDown(id) : undefined}
      onPointerUp={onPointerUp}
      onPointerCancel={onPointerCancel}
      onPointerLeave={onPointerLeave}
    >
      <div className={styles.title}>{title}</div>
      {subtitle && <div className={styles.description}>{subtitle}</div>}
    </button>
  );
}
