import React from "react";
import styles from "./PageHeader.module.css";

interface Props {
  title: string;
  action?: React.ReactNode;
}

export function PageHeader({ title, action }: Props) {
  return (
    <div className={styles.header}>
      <h1 className={styles.title}>{title}</h1>
      {action && <div className={styles.action}>{action}</div>}
    </div>
  );
}
