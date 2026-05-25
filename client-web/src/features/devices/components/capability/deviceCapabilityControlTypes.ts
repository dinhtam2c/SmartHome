import type { Dispatch, SetStateAction } from "react";
import type { OperationRule } from "../../services/deviceCapabilityService";
import type { DeviceCapabilityDto } from "../../types/deviceTypes";

export type DeviceCapabilityControlsProps = {
  capability: DeviceCapabilityDto;
  canControlDevice: boolean;
  quickToggleBusyCapabilityId: string | null;
  optimisticToggleValues: Record<string, boolean | undefined>;
  inlineCommandValues: Record<string, string>;
  setInlineCommandValues: Dispatch<SetStateAction<Record<string, string>>>;
  expandedCapabilityId: string | null;
  setExpandedCapabilityId: Dispatch<SetStateAction<string | null>>;
  advancedOperationByCapability: Record<string, string>;
  setAdvancedOperationByCapability: Dispatch<SetStateAction<Record<string, string>>>;
  advancedFieldValuesByContext: Record<string, Record<string, string>>;
  setAdvancedFieldValuesByContext: Dispatch<
    SetStateAction<Record<string, Record<string, string>>>
  >;
  onBooleanToggleSend: (
    capabilityId: string,
    capabilityKey: string,
    endpointId: string,
    operation: string,
    nextValue: boolean,
    previousValue: boolean | null,
    valuePath?: string | null
  ) => void;
  onScheduleInlineCommandSend: (
    capabilityId: string,
    capabilityKey: string,
    endpointId: string,
    operation: string,
    rule: OperationRule | null,
    rawValue: string,
    valuePath?: string | null,
    delayMs?: number
  ) => void;
  onInlineCommandSend: (
    capabilityId: string,
    capabilityKey: string,
    endpointId: string,
    operation: string,
    rule: OperationRule | null,
    rawValue: string,
    valuePath?: string | null,
    valueOverride?: unknown
  ) => void;
  onLiveInlineCommandSend: (
    capabilityId: string,
    capabilityKey: string,
    endpointId: string,
    operation: string,
    rule: OperationRule | null,
    rawValue: string,
    valuePath?: string | null,
    valueOverride?: unknown
  ) => void;
};
