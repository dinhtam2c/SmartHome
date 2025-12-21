import type { ReactNode } from "react";
import styles from "./CellGrid.module.css";

interface Props {
  children: ReactNode;
}

export function CellGrid({ children }: Props) {
  return (
    <div className={styles.grid}>
      {children}
    </div>
  );
}
