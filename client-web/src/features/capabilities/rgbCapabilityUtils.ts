export type RgbChannel = "red" | "green" | "blue";

export type RgbValue = Record<RgbChannel, number>;

const RGB_CHANNELS: RgbChannel[] = ["red", "green", "blue"];

function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === "object" && value !== null && !Array.isArray(value);
}

function toRgbChannelValue(value: unknown) {
  if (typeof value === "number" && Number.isFinite(value)) {
    return value;
  }

  if (typeof value === "string") {
    const trimmed = value.trim();
    if (trimmed !== "") {
      const parsed = Number(trimmed);
      if (Number.isFinite(parsed)) {
        return parsed;
      }
    }
  }

  return null;
}

function clampRgbChannel(value: number) {
  return Math.max(0, Math.min(255, Math.round(value)));
}

function toHexChannel(value: number) {
  return clampRgbChannel(value).toString(16).padStart(2, "0");
}

export function getRgbValue(value: unknown): RgbValue | null {
  if (!isRecord(value)) {
    return null;
  }

  const red = toRgbChannelValue(value.red);
  const green = toRgbChannelValue(value.green);
  const blue = toRgbChannelValue(value.blue);

  if (red !== null && green !== null && blue !== null) {
    return { red, green, blue };
  }

  if ("value" in value) {
    const nestedValue = getRgbValue(value.value);
    if (nestedValue) {
      return nestedValue;
    }
  }

  if ("color" in value) {
    return getRgbValue(value.color);
  }

  return null;
}

export function getRgbHex(value: unknown) {
  const rgb = getRgbValue(value);
  if (!rgb) {
    return null;
  }

  return `#${toHexChannel(rgb.red)}${toHexChannel(rgb.green)}${toHexChannel(rgb.blue)}`;
}

export function parseRgbHex(value: string): RgbValue | null {
  const normalized = value.trim().replace(/^#/, "");

  if (!/^[0-9a-fA-F]{6}$/.test(normalized)) {
    return null;
  }

  return {
    red: Number.parseInt(normalized.slice(0, 2), 16),
    green: Number.parseInt(normalized.slice(2, 4), 16),
    blue: Number.parseInt(normalized.slice(4, 6), 16),
  } satisfies RgbValue;
}

export function formatRgbValue(
  value: unknown,
  labels: Partial<Record<RgbChannel, string>> = {}
) {
  const rgb = getRgbValue(value);
  if (!rgb) {
    return null;
  }

  const channelText = RGB_CHANNELS
    .map((channel) => `${labels[channel] ?? channel}: ${rgb[channel]}`)
    .join(" · ");
  const hex = getRgbHex(rgb);

  return hex ? `${channelText} (${hex})` : channelText;
}
