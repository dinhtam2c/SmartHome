export const DEVICE_LEVEL_ENDPOINT_KEY = "__device_level__";

export function toEndpointKey(endpointId: string | null | undefined) {
  if (typeof endpointId !== "string") {
    return DEVICE_LEVEL_ENDPOINT_KEY;
  }

  const trimmed = endpointId.trim();
  return trimmed === "" ? DEVICE_LEVEL_ENDPOINT_KEY : trimmed;
}
