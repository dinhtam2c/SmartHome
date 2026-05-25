import type { TFunction } from "i18next";
import type { RgbChannel } from "./rgbCapabilityUtils";
import {
  LIGHT_SENSOR_CAPABILITY_ID,
  localizeSchemaPath,
  LOCK_STATE_CAPABILITY_ID,
  MOTION_SENSOR_CAPABILITY_ID,
  type CapabilityBooleanLabels,
} from "./capabilityRenderRegistry";

export type CapabilityLabelKeys = {
  rootValue?: string;
  stateKeyPrefix?: string;
  commandKeyPrefix?: string;
  operationKeyPrefix?: string;
  on?: string;
  off?: string;
  locked?: string;
  unlocked?: string;
  lightDetected?: string;
  lightClear?: string;
  motionDetected?: string;
  motionClear?: string;
  color?: string;
};

const DEVICE_LABEL_KEYS = {
  rootValue: "value",
  stateKeyPrefix: "stateKeyLabels",
  commandKeyPrefix: "commandKeyLabels",
  operationKeyPrefix: "operationKeyLabels",
  on: "on",
  off: "off",
  locked: "stateLocked",
  unlocked: "stateUnlocked",
  lightDetected: "stateLightDetected",
  lightClear: "stateLightClear",
  motionDetected: "stateMotionDetected",
  motionClear: "stateMotionClear",
  color: "stateKeyLabels.color",
} satisfies Required<CapabilityLabelKeys>;

const SCENE_LABEL_KEYS = {
  rootValue: "scenes.stateValue",
  stateKeyPrefix: "scenes.stateKeyLabels",
  commandKeyPrefix: "scenes.commandKeyLabels",
  operationKeyPrefix: "scenes.operationKeyLabels",
  on: "scenes.stateOn",
  off: "scenes.stateOff",
  locked: "scenes.stateLocked",
  unlocked: "scenes.stateUnlocked",
  lightDetected: "scenes.stateLightDetected",
  lightClear: "scenes.stateLightClear",
  motionDetected: "scenes.stateMotionDetected",
  motionClear: "scenes.stateMotionClear",
  color: "scenes.stateKeyLabels.color",
} satisfies Required<CapabilityLabelKeys>;

const ROOM_LABEL_KEYS = {
  ...DEVICE_LABEL_KEYS,
  on: "on",
  off: "off",
  locked: "stateLocked",
  unlocked: "stateUnlocked",
  lightDetected: "stateLightDetected",
  lightClear: "stateLightClear",
  motionDetected: "stateMotionDetected",
  motionClear: "stateMotionClear",
} satisfies Required<CapabilityLabelKeys>;

export const CAPABILITY_LABEL_KEYS = {
  device: DEVICE_LABEL_KEYS,
  scene: SCENE_LABEL_KEYS,
  room: ROOM_LABEL_KEYS,
} as const;

function translate(
  t: TFunction,
  key: string,
  defaultValue?: string
) {
  const value = t(key, {
    defaultValue: defaultValue ?? key,
  });

  return typeof value === "string" ? value : String(value);
}

function translateOptional(t: TFunction, key: string) {
  const value = t(key, { defaultValue: "" });
  const text = typeof value === "string" ? value : String(value);

  return text === key ? "" : text;
}

function joinKey(prefix: string | undefined, segment: string) {
  return prefix ? `${prefix}.${segment}` : segment;
}

export function getCapabilityBooleanLabels(
  t: TFunction,
  keys: CapabilityLabelKeys = DEVICE_LABEL_KEYS
): CapabilityBooleanLabels {
  const on = translate(t, keys.on ?? DEVICE_LABEL_KEYS.on, "On");
  const off = translate(t, keys.off ?? DEVICE_LABEL_KEYS.off, "Off");
  const locked = translate(t, keys.locked ?? DEVICE_LABEL_KEYS.locked, "Locked");
  const unlocked = translate(t, keys.unlocked ?? DEVICE_LABEL_KEYS.unlocked, "Unlocked");
  const lightDetected = translate(
    t,
    keys.lightDetected ?? DEVICE_LABEL_KEYS.lightDetected,
    "Light detected"
  );
  const lightClear = translate(
    t,
    keys.lightClear ?? DEVICE_LABEL_KEYS.lightClear,
    "No light"
  );
  const motionDetected = translate(
    t,
    keys.motionDetected ?? DEVICE_LABEL_KEYS.motionDetected,
    "Motion detected"
  );
  const motionClear = translate(
    t,
    keys.motionClear ?? DEVICE_LABEL_KEYS.motionClear,
    "No motion"
  );

  return {
    on,
    off,
    locked,
    unlocked,
    byCapability: {
      [LOCK_STATE_CAPABILITY_ID]: {
        trueLabel: locked,
        falseLabel: unlocked,
      },
      [LIGHT_SENSOR_CAPABILITY_ID]: {
        trueLabel: lightDetected,
        falseLabel: lightClear,
      },
      [MOTION_SENSOR_CAPABILITY_ID]: {
        trueLabel: motionDetected,
        falseLabel: motionClear,
      },
    },
  };
}

export function getCapabilityRgbLabels(
  t: TFunction,
  keys: CapabilityLabelKeys = DEVICE_LABEL_KEYS,
  defaults: Partial<Record<RgbChannel, string>> = {}
) {
  const stateKeyPrefix = keys.stateKeyPrefix ?? DEVICE_LABEL_KEYS.stateKeyPrefix;

  return {
    red: translate(t, joinKey(stateKeyPrefix, "red"), defaults.red ?? "R"),
    green: translate(t, joinKey(stateKeyPrefix, "green"), defaults.green ?? "G"),
    blue: translate(t, joinKey(stateKeyPrefix, "blue"), defaults.blue ?? "B"),
  } satisfies Record<RgbChannel, string>;
}

export function getCapabilityColorLabel(
  t: TFunction,
  keys: CapabilityLabelKeys = DEVICE_LABEL_KEYS
) {
  return translate(t, keys.color ?? DEVICE_LABEL_KEYS.color, "Color");
}

export function localizeCapabilityStatePath(
  t: TFunction,
  path: string,
  keys: CapabilityLabelKeys = DEVICE_LABEL_KEYS
) {
  const rootValueKey = keys.rootValue ?? DEVICE_LABEL_KEYS.rootValue;
  const stateKeyPrefix = keys.stateKeyPrefix ?? DEVICE_LABEL_KEYS.stateKeyPrefix;

  return localizeSchemaPath(
    path,
    translate(t, rootValueKey, "Value"),
    (segment) => translate(t, joinKey(stateKeyPrefix, segment), segment)
  );
}

export function localizeCapabilityCommandPath(
  t: TFunction,
  path: string,
  keys: CapabilityLabelKeys = DEVICE_LABEL_KEYS
) {
  const rootValueKey = keys.rootValue ?? DEVICE_LABEL_KEYS.rootValue;
  const commandKeyPrefix = keys.commandKeyPrefix ?? DEVICE_LABEL_KEYS.commandKeyPrefix;
  const stateKeyPrefix = keys.stateKeyPrefix ?? DEVICE_LABEL_KEYS.stateKeyPrefix;

  return localizeSchemaPath(
    path,
    translate(t, rootValueKey, "Value"),
    (segment) => {
      const commandLabel = translateOptional(t, joinKey(commandKeyPrefix, segment));

      if (commandLabel.trim()) {
        return commandLabel;
      }

      return translate(t, joinKey(stateKeyPrefix, segment), segment);
    }
  );
}

export function localizeCapabilityOperation(
  t: TFunction,
  operation: string,
  keys: CapabilityLabelKeys = DEVICE_LABEL_KEYS
) {
  const normalized = operation.trim();

  if (!normalized) {
    return operation;
  }

  const operationKeyPrefix =
    keys.operationKeyPrefix ?? DEVICE_LABEL_KEYS.operationKeyPrefix;

  return translate(
    t,
    joinKey(operationKeyPrefix, normalized.toLowerCase()),
    normalized
  );
}
