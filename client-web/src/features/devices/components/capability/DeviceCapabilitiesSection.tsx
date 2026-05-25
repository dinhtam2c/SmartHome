import { useState, type Dispatch, type SetStateAction } from "react";
import { useTranslation } from "react-i18next";
import { CellGrid } from "@/shared/ui/CellGrid";
import {
  getCapabilityDisplayLabel,
  getCapabilityDisplayOrder,
} from "@/features/capabilities";
import type { OperationRule } from "../../services/deviceCapabilityService";
import type { DeviceCapabilityDto } from "../../types/deviceTypes";
import { DeviceCapabilityCard } from "./DeviceCapabilityCard";
import styles from "@/shared/styles/featurePage.module.css";
import pageStyles from "./DeviceCapability.module.css";

type CapabilityGroup = {
  endpointKey: string;
  endpointLabel: string;
  capabilities: DeviceCapabilityDto[];
};

type Props = {
  capabilityGroups: CapabilityGroup[];
  canControlDevice: boolean;
  quickToggleBusyCapabilityId: string | null;
  optimisticToggleValues: Record<string, boolean | undefined>;
  inlineCommandValues: Record<string, string>;
  setInlineCommandValues: Dispatch<SetStateAction<Record<string, string>>>;
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
  quickActionError: string | null;
  formatCapabilityState: (state: unknown, capabilityId?: string | null) => string;
};

function sortCapabilities(
  t: ReturnType<typeof useTranslation<"devices">>["t"],
  capabilities: DeviceCapabilityDto[]
) {
  return capabilities
    .map((capability) => ({
      capability,
      displayLabel: getCapabilityDisplayLabel(
        t,
        capability.capabilityId,
        capability.metadata?.defaultName
      ),
    }))
    .sort((left, right) => {
      const orderDelta =
        getCapabilityDisplayOrder(left.capability.metadata) -
        getCapabilityDisplayOrder(right.capability.metadata);

      if (orderDelta !== 0) {
        return orderDelta;
      }

      const labelDelta = left.displayLabel.localeCompare(right.displayLabel);
      if (labelDelta !== 0) {
        return labelDelta;
      }

      return left.capability.capabilityId.localeCompare(
        right.capability.capabilityId
      );
    });
}

export function DeviceCapabilitiesSection({
  capabilityGroups,
  canControlDevice,
  quickToggleBusyCapabilityId,
  optimisticToggleValues,
  inlineCommandValues,
  setInlineCommandValues,
  onBooleanToggleSend,
  onScheduleInlineCommandSend,
  onInlineCommandSend,
  onLiveInlineCommandSend,
  quickActionError,
  formatCapabilityState,
}: Props) {
  const { t } = useTranslation("devices");
  const [expandedCapabilityId, setExpandedCapabilityId] = useState<string | null>(null);
  const [advancedOperationByCapability, setAdvancedOperationByCapability] = useState<
    Record<string, string>
  >({});
  const [advancedFieldValuesByContext, setAdvancedFieldValuesByContext] = useState<
    Record<string, Record<string, string>>
  >({});

  return (
    <section className={styles.section}>
      <div className={styles.sectionHeader}>
        <h2 className={styles.sectionTitle}>{t("capabilities")}</h2>
      </div>

      {capabilityGroups.map((group) => (
        <div key={group.endpointKey} className={pageStyles.endpointGroup}>
          <div className={pageStyles.endpointGroupHeader}>
            <h3 className={pageStyles.endpointGroupTitle}>{group.endpointLabel}</h3>
            <span className={pageStyles.endpointGroupCount}>
              {group.capabilities.length} {t("capabilities")}
            </span>
          </div>

          <CellGrid>
            {sortCapabilities(t, group.capabilities).map(({ capability, displayLabel }) => (
              <DeviceCapabilityCard
                key={capability.id}
                capability={capability}
                displayLabel={displayLabel}
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
                formatCapabilityState={formatCapabilityState}
              />
            ))}
          </CellGrid>
        </div>
      ))}

      {quickActionError ? <div className={styles.emptyState}>{quickActionError}</div> : null}
    </section>
  );
}
