import type { CSSProperties } from "react";
import type { DeviceCategoryDefinition } from "../types/deviceCategoryTypes";
import { DEVICE_CATEGORY_ICON_PATHS } from "../services/deviceCategoryPresentationService";
import styles from "./DeviceCategoryIcon.module.css";

type Props = {
  category: DeviceCategoryDefinition;
  label: string;
  size?: "sm" | "md";
  className?: string;
};

export function DeviceCategoryIcon({
  category,
  label,
  size = "md",
  className,
}: Props) {
  const paths =
    DEVICE_CATEGORY_ICON_PATHS[category.iconKey] ?? DEVICE_CATEGORY_ICON_PATHS.box;
  const style = {
    "--device-category-color": category.color,
  } as CSSProperties;

  return (
    <span
      className={`${styles.iconWrap} ${styles[`iconWrap--${size}`]} ${className ?? ""}`}
      style={style}
      title={label}
      aria-label={label}
      role="img"
    >
      <svg
        className={styles.icon}
        viewBox="0 0 24 24"
        fill="none"
        xmlns="http://www.w3.org/2000/svg"
        aria-hidden="true"
      >
        {paths.map((path) => (
          <path
            key={path.d}
            d={path.d}
            fill={path.fill ? "currentColor" : "none"}
            stroke="currentColor"
            strokeWidth="1.8"
            strokeLinecap="round"
            strokeLinejoin="round"
          />
        ))}
      </svg>
    </span>
  );
}
