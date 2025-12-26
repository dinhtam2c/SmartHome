import type { ReactNode } from "react";
import styles from "./Cell.module.css";

interface Props {
  id: string;
  title: string;
  subtitle?: string | ReactNode;
  onClick: (id: string) => void;
  disabled: boolean;
}

export function Cell({
  id,
  title,
  subtitle,
  onClick,
  disabled = false,
}: Props) {
  return (
    <button
      className={styles.cell}
      disabled={disabled}
      onClick={() => onClick(id)}
    >
      <div className={styles.title}>{title}</div>
      {subtitle && <div className={styles.description}>{subtitle}</div>}
    </button>
  );
}
