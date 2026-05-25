import type {
  CapabilityControlCommitPolicy,
  CapabilityRegistryMetadata,
} from "./capabilities.types";
import { normalizeCapabilityId } from "./capabilityRenderRegistry";

export type CapabilityDeviceControlDefinition =
  | {
    kind: "boolean";
    operation: string;
    statePath: string;
    valuePath: string;
  }
  | {
    kind: "numericSlider";
    operation: string;
    statePath: string | null;
    valuePath: string;
    commitPolicy: CapabilityControlCommitPolicy;
    throttleMs?: number;
  }
  | {
    kind: "operationForm";
    operation: string;
  }
  | {
    kind: "rgb";
    operation: string;
  }
  | {
    kind: "readonly";
  };

type CapabilityMetadataRegistryEntry = {
  id: string;
  version: number;
  metadata: CapabilityRegistryMetadata;
  deviceControl: CapabilityDeviceControlDefinition;
};

const CAPABILITY_METADATA_REGISTRY: Record<string, CapabilityMetadataRegistryEntry> = {
  "switch.power": {
    id: "switch.power",
    version: 1,
    metadata: {
      defaultName: "Power",
      primary: {
        state: "value",
        operation: "set",
      },
      visual: {
        icon: "power",
        tone: "green",
      },
      control: {
        commitPolicy: "immediate",
      },
      overviewVisible: true,
      order: 0,
    },
    deviceControl: {
      kind: "boolean",
      operation: "set",
      statePath: "value",
      valuePath: "value",
    },
  },
  "lock.state": {
    id: "lock.state",
    version: 1,
    metadata: {
      defaultName: "Lock State",
      primary: {
        state: "locked",
        operation: "set",
      },
      visual: {
        icon: "lock",
        tone: "neutral",
      },
      control: {
        commitPolicy: "immediate",
      },
      overviewVisible: true,
      order: 1,
    },
    deviceControl: {
      kind: "boolean",
      operation: "set",
      statePath: "locked",
      valuePath: "locked",
    },
  },
  "light.brightness": {
    id: "light.brightness",
    version: 1,
    metadata: {
      unit: "%",
      defaultName: "Brightness",
      primary: {
        state: "value",
        operation: "set",
      },
      visual: {
        icon: "brightness",
        tone: "amber",
      },
      control: {
        commitPolicy: "commitOnRelease",
      },
      overviewVisible: true,
      order: 1,
    },
    deviceControl: {
      kind: "numericSlider",
      operation: "set",
      statePath: "value",
      valuePath: "value",
      commitPolicy: "commitOnRelease",
    },
  },
  "light.rgb": {
    id: "light.rgb",
    version: 1,
    metadata: {
      defaultName: "RGB",
      primary: {
        state: "value",
        operation: "set",
      },
      visual: {
        icon: "palette",
        tone: "violet",
      },
      overviewVisible: false,
      order: 2,
    },
    deviceControl: {
      kind: "rgb",
      operation: "set",
    },
  },
  "light.effect": {
    id: "light.effect",
    version: 1,
    metadata: {
      defaultName: "Light Effect",
      primary: {
        operation: "flash",
      },
      control: {
        commitPolicy: "formOnly",
      },
      overviewVisible: false,
      order: 50,
    },
    deviceControl: {
      kind: "operationForm",
      operation: "flash",
    },
  },
  "fan.speed": {
    id: "fan.speed",
    version: 1,
    metadata: {
      unit: "%",
      defaultName: "Fan Speed",
      primary: {
        state: "value",
        operation: "set",
      },
      visual: {
        icon: "fan",
        tone: "blue",
      },
      control: {
        commitPolicy: "commitOnRelease",
      },
      overviewVisible: true,
      order: 2,
    },
    deviceControl: {
      kind: "numericSlider",
      operation: "set",
      statePath: "value",
      valuePath: "value",
      commitPolicy: "commitOnRelease",
    },
  },
  buzzer: {
    id: "buzzer",
    version: 1,
    metadata: {
      defaultName: "Buzzer",
      primary: {
        operation: "beep",
      },
      visual: {
        icon: "buzzer",
        tone: "neutral",
      },
      control: {
        commitPolicy: "formOnly",
      },
      overviewVisible: false,
      order: 6,
    },
    deviceControl: {
      kind: "operationForm",
      operation: "beep",
    },
  },
  "sensor.interval": {
    id: "sensor.interval",
    version: 1,
    metadata: {
      unit: "s",
      defaultName: "Reporting Interval",
      primary: {
        state: "value",
        operation: "set",
      },
      visual: {
        icon: "timer",
        tone: "neutral",
      },
      control: {
        commitPolicy: "commitOnRelease",
      },
      overviewVisible: false,
      order: 90,
    },
    deviceControl: {
      kind: "numericSlider",
      operation: "set",
      statePath: "value",
      valuePath: "value",
      commitPolicy: "commitOnRelease",
    },
  },
  "sensor.temperature": {
    id: "sensor.temperature",
    version: 1,
    metadata: {
      unit: "°C",
      defaultName: "Temperature",
      primary: {
        state: "value",
      },
      visual: {
        icon: "temperature",
        tone: "red",
        precision: 1,
      },
      overviewVisible: true,
      order: 2,
    },
    deviceControl: {
      kind: "readonly",
    },
  },
  "sensor.humidity": {
    id: "sensor.humidity",
    version: 1,
    metadata: {
      unit: "%",
      defaultName: "Humidity",
      primary: {
        state: "value",
      },
      visual: {
        icon: "humidity",
        tone: "blue",
        precision: 1,
      },
      overviewVisible: true,
      order: 3,
    },
    deviceControl: {
      kind: "readonly",
    },
  },
  "sensor.motion": {
    id: "sensor.motion",
    version: 1,
    metadata: {
      defaultName: "Motion",
      primary: {
        state: "value",
      },
      visual: {
        icon: "motion",
        tone: "green",
      },
      overviewVisible: true,
      order: 4,
    },
    deviceControl: {
      kind: "readonly",
    },
  },
  "sensor.illuminance": {
    id: "sensor.illuminance",
    version: 1,
    metadata: {
      unit: "ADC",
      defaultName: "Illuminance",
      primary: {
        state: "value",
      },
      visual: {
        icon: "illuminance",
        tone: "amber",
      },
      overviewVisible: true,
      order: 5,
    },
    deviceControl: {
      kind: "readonly",
    },
  },
  "sensor.light": {
    id: "sensor.light",
    version: 1,
    metadata: {
      defaultName: "Light Detection",
      primary: {
        state: "value",
      },
      visual: {
        icon: "light",
        tone: "amber",
      },
      overviewVisible: true,
      order: 6,
    },
    deviceControl: {
      kind: "readonly",
    },
  },
};

function getCapabilityMetadataRegistryEntry(
  capabilityId: string | null | undefined
) {
  return CAPABILITY_METADATA_REGISTRY[normalizeCapabilityId(capabilityId)] ?? null;
}

export function getCapabilityMetadata(
  capabilityId: string | null | undefined,
  version?: number | null
) {
  const entry = getCapabilityMetadataRegistryEntry(capabilityId);

  if (!entry) {
    return {};
  }

  if (typeof version === "number" && version > 0 && entry.version !== version) {
    return {};
  }

  return entry.metadata;
}

export function getCapabilityDeviceControlDefinition(
  capabilityId: string | null | undefined,
  version?: number | null
) {
  const entry = getCapabilityMetadataRegistryEntry(capabilityId);

  if (!entry) {
    return null;
  }

  if (typeof version === "number" && version > 0 && entry.version !== version) {
    return null;
  }

  return entry.deviceControl;
}
