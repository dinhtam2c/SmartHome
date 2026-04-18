import type { ReactNode } from "react";
import { createPortal } from "react-dom";
import styles from "./Modal.module.css";
import { useTranslation } from "react-i18next";

interface Props {
  open: boolean;
  title: string;
  onClose: () => void;
  children: ReactNode;
}

export function Modal({ open, title, onClose, children }: Props) {
  const { t } = useTranslation();

  if (!open) return null;

  const content = (
    <div className={styles.overlay} onClick={onClose}>
      <div className={styles.modal} onClick={(e) => e.stopPropagation()}>
        <div className={styles.header}>
          <strong>{title}</strong>
          <button className={styles.close} onClick={onClose} aria-label={t("close")}>
            x
          </button>
        </div>

        <div className={styles.body}>{children}</div>
      </div>
    </div>
  );

  return createPortal(content, document.body);
}
