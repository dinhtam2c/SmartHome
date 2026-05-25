import type { CSSProperties } from "react";
import type { DeviceCategoryDefinition } from "../../types/deviceCategoryTypes";
import { DeviceCategoryIcon } from "../DeviceCategoryIcon/DeviceCategoryIcon";
import styles from "./DeviceCategoryBadge.module.css";

type Props = {
  category: DeviceCategoryDefinition;
  label: string;
  className?: string;
};

export function DeviceCategoryBadge({ category, label, className }: Props) {
  const style = {
    "--device-category-color": category.color,
  } as CSSProperties;

  return (
    <span className={`${styles.badge} ${className ?? ""}`} style={style}>
      <DeviceCategoryIcon category={category} label={label} size="sm" />
      <span className={styles.label}>{label}</span>
    </span>
  );
}
