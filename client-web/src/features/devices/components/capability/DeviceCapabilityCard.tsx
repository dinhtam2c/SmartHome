import type { Dispatch, SetStateAction } from "react";
import { useTranslation } from "react-i18next";
import { Cell } from "@/shared/ui/Cell";
import { LocalizedCapabilityStateValue } from "@/features/capabilities";
import {
  CAPABILITY_LABEL_KEYS,
  getCapabilityBooleanLabel,
  getCapabilityBooleanLabels,
  getCapabilityPrimaryStateValue,
  getCapabilityUnit,
  type CapabilityBooleanLabels,
} from "@/features/capabilities";
import type { OperationRule } from "../../services/deviceCapabilityService";
import type { DeviceCapabilityDto } from "../../types/deviceTypes";
import { DeviceCapabilityControls } from "./DeviceCapabilityControls";
import pageStyles from "./DeviceCapability.module.css";

type Props = {
  capability: DeviceCapabilityDto;
  displayLabel: string;
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
  formatCapabilityState: (state: unknown, capabilityId?: string | null) => string;
};

function getDisplayValue(
  capabilityId: string,
  primaryStateValue: unknown,
  booleanLabels: CapabilityBooleanLabels,
  t: (key: string) => string,
  formatCapabilityState: (state: unknown, capabilityId?: string | null) => string,
  isDeviceOnline: boolean
) {
  if (primaryStateValue === undefined) {
    return t("notAvailable");
  }

  if (primaryStateValue === null) {
    return isDeviceOnline ? t("notAvailable") : t("offline");
  }

  if (
    typeof primaryStateValue === "string" ||
    typeof primaryStateValue === "number"
  ) {
    return String(primaryStateValue);
  }

  if (typeof primaryStateValue === "boolean") {
    return getCapabilityBooleanLabel(capabilityId, primaryStateValue, booleanLabels);
  }

  return formatCapabilityState(primaryStateValue, capabilityId);
}

export function DeviceCapabilityCard({
  capability,
  displayLabel,
  canControlDevice,
  quickToggleBusyCapabilityId,
  optimisticToggleValues,
  inlineCommandValues,
  setInlineCommandValues,
  expandedCapabilityId,
  setExpandedCapabilityId,
  advancedOperationByCapability,
  setAdvancedOperationByCapability,
  advancedFieldValuesByContext,
  setAdvancedFieldValuesByContext,
  onBooleanToggleSend,
  onScheduleInlineCommandSend,
  onInlineCommandSend,
  onLiveInlineCommandSend,
  formatCapabilityState,
}: Props) {
  const { t } = useTranslation("devices");
  const booleanLabels = getCapabilityBooleanLabels(t, CAPABILITY_LABEL_KEYS.device);
  const primaryStatePath = capability.metadata?.primary?.state ?? null;
  const primaryOperation = capability.metadata?.primary?.operation ?? null;
  const primaryStateValue = getCapabilityPrimaryStateValue(
    capability.state,
    primaryStatePath
  );
  const shouldHideStatePlaceholder =
    !primaryStatePath &&
    typeof primaryOperation === "string" &&
    primaryOperation.trim() !== "" &&
    primaryStateValue === undefined;
  const displayValue = shouldHideStatePlaceholder
    ? ""
    : getDisplayValue(
      capability.capabilityId,
      primaryStateValue,
      booleanLabels,
      t,
      formatCapabilityState,
      canControlDevice
    );
  const capabilityUnit = getCapabilityUnit(capability.metadata);
  const canShowUnit =
    capabilityUnit !== null &&
    displayValue !== "" &&
    displayValue !== t("notAvailable") &&
    (typeof primaryStateValue === "number" || typeof primaryStateValue === "string");

  return (
    <div className={pageStyles.capabilityCardWrap}>
      <Cell
        id={capability.id}
        title={
          <LocalizedCapabilityStateValue
            t={t}
            labelKeys={CAPABILITY_LABEL_KEYS.device}
            capabilityId={capability.capabilityId}
            label={displayLabel}
            value={primaryStateValue}
            fallbackText={displayValue}
            metadata={capability.metadata}
            unit={canShowUnit ? capabilityUnit : null}
          />
        }
        onClick={() => void 0}
        disabled={false}
      />

      <DeviceCapabilityControls
        capability={capability}
        canControlDevice={canControlDevice}
        quickToggleBusyCapabilityId={quickToggleBusyCapabilityId}
        optimisticToggleValues={optimisticToggleValues}
        inlineCommandValues={inlineCommandValues}
        setInlineCommandValues={setInlineCommandValues}
        expandedCapabilityId={expandedCapabilityId}
        setExpandedCapabilityId={setExpandedCapabilityId}
        advancedOperationByCapability={advancedOperationByCapability}
        setAdvancedOperationByCapability={setAdvancedOperationByCapability}
        advancedFieldValuesByContext={advancedFieldValuesByContext}
        setAdvancedFieldValuesByContext={setAdvancedFieldValuesByContext}
        onBooleanToggleSend={onBooleanToggleSend}
        onScheduleInlineCommandSend={onScheduleInlineCommandSend}
        onInlineCommandSend={onInlineCommandSend}
        onLiveInlineCommandSend={onLiveInlineCommandSend}
      />
    </div>
  );
}
