import type { ReactNode } from "react";
import { Cell } from "@/shared/ui/Cell";
import { CellGrid } from "@/shared/ui/CellGrid";
import { StatusChip } from "@/shared/ui/StatusChip";
import { LocalizedCapabilityStateValue } from "@/features/capabilities/components/CapabilityStateValue";
import {
  DeviceCategoryBadge, getDeviceCategoryLabel, resolveDeviceCategory, useDeviceCategoryRegistry, } from "@/features/device-categories";
import {
  CAPABILITY_LABEL_KEYS, DEVICE_LEVEL_ENDPOINT_KEY, getCapabilityDisplayOrder, getCapabilityDisplayLabel, getCapabilityPrimaryStateValue, getCapabilityUnit, isCapabilityOverviewVisible, } from "@/features/capabilities";
import type { HomeUnassignedDeviceOverviewDto } from "../types/homeTypes";
import styles from "@/shared/styles/featurePage.module.css";
import sectionStyles from "./HomeUnassignedDevicesSection.module.css";
import { useTranslation } from "react-i18next";

type Props = {
  devices: HomeUnassignedDeviceOverviewDto[];
  onOpenDevice: (deviceId: string) => void;
  formatCapabilityStatePreview: (state: unknown) => string;
};

export function HomeUnassignedDevicesSection({
  devices,
  onOpenDevice,
  formatCapabilityStatePreview,
}: Props) {
  const { t } = useTranslation("homes");
  const { t: tDeviceCategories } = useTranslation("deviceCategories");
  const { categories } = useDeviceCategoryRegistry();

  function getEndpointLabel(endpointId: string, endpointName: string | null) {
    return endpointId === DEVICE_LEVEL_ENDPOINT_KEY
      ? t("detail.deviceLevel")
      : endpointName?.trim() || endpointId;
  }

  if (devices.length === 0) {
    return null;
  }

  return (
    <section className={styles.section}>
      <div className={styles.sectionHeader}>
        <h2 className={styles.sectionTitle}>{t("detail.unassignedDevices")}</h2>
      </div>

      <CellGrid>
        {devices.map((device) => {
          const category = resolveDeviceCategory(categories, device.category);
          const categoryLabel = getDeviceCategoryLabel(category, (key, fallback) =>
            tDeviceCategories(key, { defaultValue: fallback })
          );
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
                  const canShowUnit =
                    capabilityUnit &&
                      capabilityValue !== t("notAvailable") &&
                      (typeof primaryStateValue === "number" ||
                        typeof primaryStateValue === "string");

                  return (
                    <LocalizedCapabilityStateValue
                      t={t}
                      labelKeys={CAPABILITY_LABEL_KEYS.scene}
                      key={`${capability.capabilityId}:${displayLabel}`}
                      capabilityId={capability.capabilityId}
                      label={displayLabel}
                      value={primaryStateValue}
                      fallbackText={capabilityValue}
                      metadata={capability.metadata}
                      unit={canShowUnit ? capabilityUnit : null}
                    />
                  );
                })
                .reduce<ReactNode[]>((nodes, node, index) => {
                  if (index > 0) {
                    nodes.push(" · ");
                  }

                  nodes.push(node);
                  return nodes;
                }, []);

              return { endpointLabel, renderedCapabilities };
            })
            .filter((line): line is {
              endpointLabel: string;
              renderedCapabilities: ReactNode[];
            } => line !== null);

          return (
            <Cell
              key={device.id}
              id={device.id}
              title={
                <div className={sectionStyles.deviceHeader}>
                  <div className={sectionStyles.deviceIdentity}>
                    <DeviceCategoryBadge category={category} label={categoryLabel} />
                    <span className={sectionStyles.deviceName}>{device.name}</span>
                  </div>
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
                        <span>{line.endpointLabel} - </span>
                        {line.renderedCapabilities}
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
    </section>
  );
}
