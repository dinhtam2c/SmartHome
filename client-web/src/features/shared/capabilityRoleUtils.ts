export type CapabilityRole = "Control" | "Sensor" | "Actuator" | "Unknown";

export const CAPABILITY_ROLE_ORDER: CapabilityRole[] = [
  "Sensor",
  "Control",
  "Actuator",
  "Unknown",
];

function toRoleRank(role: CapabilityRole) {
  if (role === "Sensor") return 0;
  if (role === "Control") return 1;
  if (role === "Actuator") return 2;
  return 3;
}

export function sortCapabilitiesByRole<
  T extends {
    role: CapabilityRole;
    displayLabel?: string;
    capabilityId?: string;
  },
>(capabilities: T[]) {
  return [...capabilities].sort((left, right) => {
    const roleDelta = toRoleRank(left.role) - toRoleRank(right.role);
    if (roleDelta !== 0) {
      return roleDelta;
    }

    const labelDelta = (left.displayLabel ?? left.capabilityId ?? "").localeCompare(
      right.displayLabel ?? right.capabilityId ?? ""
    );

    if (labelDelta !== 0) {
      return labelDelta;
    }

    return (left.capabilityId ?? "").localeCompare(right.capabilityId ?? "");
  });
}
