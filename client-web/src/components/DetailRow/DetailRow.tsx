import type { ReactNode } from "react";
import styles from "./DetailRow.module.css";

interface Props {
  label: string;
  children: ReactNode;
  onLabelClick?: () => void;
  labelActionTitle?: string;
}

export function DetailRow({
  label,
  children,
  onLabelClick,
  labelActionTitle,
}: Props) {
  return (
    <>
      <div className={styles.label}>
        {onLabelClick ? (
          <button
            type="button"
            className={styles.labelButton}
            onClick={onLabelClick}
            title={labelActionTitle}
          >
            {label}
          </button>
        ) : (
          label
        )}
      </div>
      <div className={styles.value}>{children}</div>
    </>
  );
}
