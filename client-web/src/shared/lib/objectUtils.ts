export function isPlainObject(value: unknown): value is Record<string, unknown> {
  return typeof value === "object" && value !== null && !Array.isArray(value);
}

export function mergeRecords(
  target: Record<string, unknown>,
  source: Record<string, unknown>
) {
  const merged: Record<string, unknown> = { ...target };

  Object.entries(source).forEach(([key, value]) => {
    const existing = merged[key];

    if (isPlainObject(existing) && isPlainObject(value)) {
      merged[key] = mergeRecords(existing, value);
      return;
    }

    merged[key] = value;
  });

  return merged;
}
