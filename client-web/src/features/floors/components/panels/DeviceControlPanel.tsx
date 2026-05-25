import { useEffect } from "react";
import { useNavigate } from "react-router-dom";
import { Button } from "@/shared/ui/Button";
import { Spinner } from "@/shared/ui/Spinner";
import { StatusChip } from "@/shared/ui/StatusChip";
import { DeviceCapabilitiesSection } from "@/features/devices";
import { useDeviceDetailPage } from "@/features/devices";
import { useTranslation } from "react-i18next";
import sharedStyles from "@/shared/styles/featurePage.module.css";
import styles from "./DeviceControlPanel.module.css";

type Props = {
  homeId: string;
  deviceId: string;
  onClose: () => void;
  openDetailsLabel: string;
  notFoundLabel: string;
};

export function DeviceControlPanel({
  homeId,
  deviceId,
  onClose,
  openDetailsLabel,
  notFoundLabel,
}: Props) {
  const navigate = useNavigate();
  const { t } = useTranslation("devices");
  const vm = useDeviceDetailPage({
    deviceId,
    homeId,
    routeRoomId: null,
    navigateTo: (to) => navigate(to),
    t,
  });

  useEffect(() => {
    const handleKeyDown = (event: KeyboardEvent) => {
      if (event.key === "Escape") {
        onClose();
      }
    };

    window.addEventListener("keydown", handleKeyDown);

    return () => {
      window.removeEventListener("keydown", handleKeyDown);
    };
  }, [onClose]);

  return (
    <div className={styles.overlay} onClick={onClose}>
      <div className={styles.panel} onClick={(event) => event.stopPropagation()}>
        <div className={styles.header}>
          <div className={styles.titleGroup}>
            <h2 className={styles.title}>{vm.device?.name ?? t("loading")}</h2>
            {vm.device ? (
              <div className={styles.metaRow}>
                <StatusChip
                  label={vm.device.isOnline ? t("online") : t("offline")}
                  tone={vm.device.isOnline ? "online" : "offline"}
                />
                <span className={styles.metaText}>
                  {vm.device.roomName ?? t("unassigned")}
                </span>
              </div>
            ) : null}
          </div>

          <button
            type="button"
            className={styles.closeButton}
            onClick={onClose}
            aria-label={t("close", { ns: "translation" })}
          >
            x
          </button>
        </div>

        {vm.isLoading ? (
          <div className={styles.loadingState}>
            <Spinner />
          </div>
        ) : vm.device ? (
          <>
            <div className={styles.capabilities}>
              <DeviceCapabilitiesSection
                capabilityGroups={vm.capabilityGroups}
                canControlDevice={vm.canControlDevice}
                quickToggleBusyCapabilityId={vm.quickToggleBusyCapabilityId}
                optimisticToggleValues={vm.optimisticToggleValues}
                inlineCommandValues={vm.inlineCommandValues}
                setInlineCommandValues={vm.setInlineCommandValues}
                onBooleanToggleSend={(...args) => void vm.handleBooleanToggleSend(...args)}
                onScheduleInlineCommandSend={(...args) =>
                  vm.scheduleInlineCommandSend(...args)
                }
                onInlineCommandSend={(...args) => void vm.handleInlineCommandSend(...args)}
                onLiveInlineCommandSend={(...args) =>
                  void vm.handleLiveInlineCommandSend(...args)
                }
                quickActionError={
                  vm.quickActionError
                    ? t(vm.quickActionError, { defaultValue: vm.quickActionError })
                    : null
                }
                formatCapabilityState={vm.formatCapabilityState}
              />
            </div>

            <div className={styles.footer}>
              <Button
                variant="secondary"
                size="sm"
                onClick={() => {
                  onClose();
                  navigate(`/homes/${homeId}/devices/${deviceId}`);
                }}
              >
                {openDetailsLabel}
              </Button>
            </div>
          </>
        ) : (
          <div className={sharedStyles.emptyState}>{notFoundLabel}</div>
        )}
      </div>
    </div>
  );
}
