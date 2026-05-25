import type { ReactNode } from "react";
import type { TFunction } from "i18next";
import {
  getCapabilityBooleanLabels,
  getCapabilityRgbLabels,
  type CapabilityLabelKeys,
} from "../../services/capabilityI18nService";
import {
  getCapabilityUnit,
  getCapabilityVisualMetadata,
} from "../../services/capabilityMetadataService";
import {
  isLockStateCapabilityId,
  resolveCapabilityStateRender,
  type CapabilityBooleanLabels,
} from "../../registry/capabilityRenderRegistry";
import type {
  CapabilityRegistryMetadata,
  CapabilityVisualIcon,
  CapabilityVisualTone,
} from "../../types/capabilityTypes";
import type { RgbChannel } from "../../services/rgbCapabilityService";
import styles from "./CapabilityStateValue.module.css";

const ICON_CLASSES: Partial<Record<CapabilityVisualIcon, string>> = {
  brightness: styles.iconBrightness,
  buzzer: styles.iconBuzzer,
  effect: styles.iconEffect,
  fan: styles.iconFan,
  humidity: styles.iconHumidity,
  illuminance: styles.iconIlluminance,
  light: styles.iconLight,
  lock: styles.iconLock,
  motion: styles.iconMotion,
  palette: styles.iconPalette,
  power: styles.iconPower,
  temperature: styles.iconTemperature,
  timer: styles.iconTimer,
};

const TONE_CLASSES: Partial<Record<CapabilityVisualTone, string>> = {
  amber: styles.toneAmber,
  blue: styles.toneBlue,
  green: styles.toneGreen,
  neutral: styles.toneNeutral,
  red: styles.toneRed,
  violet: styles.toneViolet,
};

function getBooleanStateValue(value: unknown): boolean | null {
  if (typeof value === "boolean") {
    return value;
  }

  if (value && typeof value === "object" && !Array.isArray(value)) {
    const record = value as Record<string, unknown>;

    if (typeof record.locked === "boolean") {
      return record.locked;
    }

    if (typeof record.value === "boolean") {
      return record.value;
    }

    const values = Object.values(record);
    return values.length === 1 && typeof values[0] === "boolean"
      ? values[0]
      : null;
  }

  return null;
}

type Props = {
  capabilityId?: string | null;
  value: unknown;
  label?: ReactNode;
  fallbackText: string;
  metadata?: CapabilityRegistryMetadata | null;
  showVisualIcon?: boolean;
  unit?: string | null;
  rgbLabels?: Partial<Record<RgbChannel, string>>;
  booleanLabels?: CapabilityBooleanLabels;
  className?: string;
};

type LocalizedProps = Omit<Props, "rgbLabels" | "booleanLabels"> & {
  t: TFunction;
  labelKeys: CapabilityLabelKeys;
};

function CapabilityStateValue({
  capabilityId,
  value,
  label,
  fallbackText,
  metadata,
  showVisualIcon = true,
  unit,
  rgbLabels,
  booleanLabels,
  className,
}: Props) {
  const visual = getCapabilityVisualMetadata(capabilityId, metadata);
  const renderPlan = resolveCapabilityStateRender(capabilityId, value, {
    fallbackText,
    rgbLabels,
    booleanLabels,
    numberPrecision: visual?.precision,
  });
  const resolvedUnit = unit === undefined ? getCapabilityUnit(metadata) : unit;
  const lockStateValue =
    renderPlan?.kind === "lock"
      ? renderPlan.locked
      : isLockStateCapabilityId(capabilityId)
        ? getBooleanStateValue(value)
        : null;
  const visualIconClass =
    visual?.icon === "lock" && lockStateValue === false
      ? styles.iconLockOpen
      : visual?.icon
        ? ICON_CLASSES[visual.icon]
        : null;
  const visualToneClass = visual?.tone ? TONE_CLASSES[visual.tone] : null;
  const shouldRenderVisualIcon = showVisualIcon && (visual?.image || visualIconClass);

  return (
    <span className={`${styles.stateValue}${className ? ` ${className}` : ""}`}>
      {label ? <span className={styles.label}>{label}:</span> : null}
      {shouldRenderVisualIcon ? (
        <span
          className={[
            styles.visualIcon,
            visualIconClass,
            visualToneClass,
          ].filter(Boolean).join(" ")}
          aria-hidden="true"
        >
          {visual?.image ? (
            <img className={styles.visualImage} src={visual.image} alt="" />
          ) : null}
        </span>
      ) : null}
      {renderPlan?.kind === "rgb" ? (
        <span
          className={styles.swatch}
          style={{ backgroundColor: renderPlan.swatchHex }}
          title={renderPlan.title}
          aria-hidden="true"
        />
      ) : null}
      {renderPlan?.kind === "text" || renderPlan?.kind === "lock" ? (
        <span className={styles.valueText}>{renderPlan.text}</span>
      ) : null}
      {resolvedUnit ? <span className={styles.unit}>{resolvedUnit}</span> : null}
    </span>
  );
}

export function LocalizedCapabilityStateValue({
  t,
  labelKeys,
  ...props
}: LocalizedProps) {
  return (
    <CapabilityStateValue
      {...props}
      rgbLabels={getCapabilityRgbLabels(t, labelKeys)}
      booleanLabels={getCapabilityBooleanLabels(t, labelKeys)}
    />
  );
}
