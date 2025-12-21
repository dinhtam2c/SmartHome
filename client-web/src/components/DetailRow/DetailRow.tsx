import type { ReactNode } from "react";
import styles from "./DetailRow.module.css";

interface Props {
  label: string;
  children: ReactNode;
}

export function DetailRow({ label, children }: Props) {
  return (
    <>
      <div className={styles.label}>{label}</div>
      <div className={styles.value}>{children}</div>
    </>
  );
}
