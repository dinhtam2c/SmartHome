import type { ReactNode } from "react";
import styles from "./Modal.module.css";

interface Props {
  open: boolean;
  title: string;
  onClose: () => void;
  children: ReactNode;
}

export function Modal({ open, title, onClose, children }: Props) {
  if (!open) return null;

  return (
    <div className={styles.overlay} onClick={onClose}>
      <div className={styles.modal} onClick={(e) => e.stopPropagation()}>
        <div className={styles.header}>
          <strong>{title}</strong>
          <button className={styles.close} onClick={onClose} aria-label="Close">
            x
          </button>
        </div>

        <div className={styles.body}>{children}</div>
      </div>
    </div>
  );
}
