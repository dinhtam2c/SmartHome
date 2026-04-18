import type { TFunction } from "i18next";

export function getCapabilityDisplayLabel(
  t: TFunction,
  capabilityId: string,
  fallbackDefaultName?: string | null
) {
  const translated = t(capabilityId, {
    ns: "capabilities",
    defaultValue: "",
    keySeparator: false,
  });

  if (typeof translated === "string" && translated.trim() !== "") {
    return translated;
  }

  if (
    typeof fallbackDefaultName === "string" &&
    fallbackDefaultName.trim() !== ""
  ) {
    return fallbackDefaultName.trim();
  }

  return capabilityId;
}
