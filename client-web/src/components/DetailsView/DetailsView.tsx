import type { ReactNode } from "react";
import styles from "./DetailsView.module.css";

interface Props {
  children: ReactNode;
  actions?: ReactNode;
}

export function DetailsView({ children, actions }: Props) {
  return (
    <div className={styles.container}>
      <div className={styles.grid}>{children}</div>

      {actions && <div className={styles.actions}>{actions}</div>}
    </div>
  );
}
