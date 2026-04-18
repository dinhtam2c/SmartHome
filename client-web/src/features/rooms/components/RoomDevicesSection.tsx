import { Cell } from "@/components/Cell";
import { CellGrid } from "@/components/CellGrid";
import { StatusChip } from "@/components/StatusChip";
import {
  DEVICE_LEVEL_ENDPOINT_KEY,
  getCapabilityDisplayOrder,
  getCapabilityDisplayLabel,
  getCapabilityPrimaryStateValue,
  getCapabilityUnit,
  isCapabilityOverviewVisible,
} from "@/features/capabilities";
import type { RoomDetailDto } from "../rooms.types";
import styles from "@/features/shared/featurePage.module.css";
import sectionStyles from "./RoomDevicesSection.module.css";
import { useTranslation } from "react-i18next";

type Props = {
  room: RoomDetailDto;
  onOpenDevice: (deviceId: string) => void;
  formatCapabilityStatePreview: (state: unknown) => string;
};

export function RoomDevicesSection({
  room,
  onOpenDevice,
  formatCapabilityStatePreview,
}: Props) {
  const { t } = useTranslation("rooms");

  function getEndpointLabel(endpointId: string, endpointName: string | null) {
    return endpointId === DEVICE_LEVEL_ENDPOINT_KEY
      ? t("deviceLevel")
      : endpointName?.trim() || endpointId;
  }

  return (
    <section className={styles.section}>
      <div className={styles.sectionHeader}>
        <h2 className={styles.sectionTitle}>{t("devices")}</h2>
      </div>

      {room.devices.length === 0 ? (
        <div className={styles.emptyState}>{t("noDevices")}</div>
      ) : (
        <CellGrid>
          {room.devices.map((device) => {
            const endpointCapabilityLines = device.endpoints
              .map((endpoint) => {
                const endpointLabel = getEndpointLabel(endpoint.endpointId, endpoint.name);
                const visibleCapabilities = (endpoint.capabilities ?? [])
                  .filter((capability) => isCapabilityOverviewVisible(capability.metadata))
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

                    return left.displayLabel.localeCompare(right.displayLabel);
                  });

                if (visibleCapabilities.length === 0) {
                  return null;
                }

                const renderedCapabilities = visibleCapabilities
                  .map(({ capability, displayLabel }) => {
                    const primaryStateValue = getCapabilityPrimaryStateValue(
                      capability.state,
                      capability.metadata?.primary?.state
                    );
                    const capabilityValue =
                      primaryStateValue === undefined || primaryStateValue === null
                        ? t("notAvailable")
                        : formatCapabilityStatePreview(primaryStateValue);
                    const capabilityUnit = getCapabilityUnit(capability.metadata);
                    const unitSuffix =
                      capabilityUnit &&
                        capabilityValue !== t("notAvailable") &&
                        (typeof primaryStateValue === "number" ||
                          typeof primaryStateValue === "string")
                        ? ` ${capabilityUnit}`
                        : "";

                    return `${displayLabel}: ${capabilityValue}${unitSuffix}`;
                  })
                  .join(" · ");

                return `${endpointLabel} - ${renderedCapabilities}`;
              })
              .filter((line): line is string => line !== null);

            return (
              <Cell
                key={device.id}
                id={device.id}
                title={
                  <div className={sectionStyles.deviceHeader}>
                    <span className={sectionStyles.deviceName}>{device.name}</span>
                    <StatusChip
                      label={device.isOnline ? t("online") : t("offline")}
                      tone={device.isOnline ? "online" : "offline"}
                      className={sectionStyles.statusChip}
                    />
                  </div>
                }
                subtitle={
                  <div className={sectionStyles.deviceSubtitle}>
                    {endpointCapabilityLines.length > 0 ? (
                      endpointCapabilityLines.map((line, index) => (
                        <div key={`${device.id}-capability-group-${index}`} className={sectionStyles.capabilityLine}>
                          {line}
                        </div>
                      ))
                    ) : (
                      <div className={sectionStyles.capabilityLine}>{t("notAvailable")}</div>
                    )}
                  </div>
                }
                onClick={(id) => onOpenDevice(id)}
                disabled={false}
              />
            );
          })}
        </CellGrid>
      )}
    </section>
  );
}
