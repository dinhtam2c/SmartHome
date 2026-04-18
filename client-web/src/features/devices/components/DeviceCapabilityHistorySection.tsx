import { useMemo } from "react";
import { Button } from "@/components/Button";
import { Cell } from "@/components/Cell";
import { CellGrid } from "@/components/CellGrid";
import { Input } from "@/components/Input";
import {
  DEVICE_LEVEL_ENDPOINT_KEY,
  getCapabilityDisplayLabel,
  toEndpointKey,
} from "@/features/capabilities";
import { timestampToDateTime } from "@/utils/dateTimeUtils";
import type { DeviceCapabilityDto, DeviceCapabilityHistoryPointDto } from "../devices.types";
import styles from "@/features/shared/featurePage.module.css";
import pageStyles from "../DeviceDetailPage.module.css";
import { useTranslation } from "react-i18next";

type Props = {
  historyFilterEndpoint: string;
  onHistoryFilterEndpointChange: (value: string) => void;
  capabilityEndpointFilterOptions: Array<{ value: string; label: string; }>;
  selectedCapabilityForHistory: string;
  onSelectedCapabilityForHistoryChange: (value: string) => void;
  historyCapabilityOptions: DeviceCapabilityDto[];
  historyFrom: string;
  onHistoryFromChange: (value: string) => void;
  historyTo: string;
  onHistoryToChange: (value: string) => void;
  isCapabilityHistoryLoading: boolean;
  capabilityHistoryError: string | null;
  capabilityHistoryTotalCount: number;
  capabilityHistoryPage: number;
  capabilityHistoryPageSize: number;
  capabilityHistory: DeviceCapabilityHistoryPointDto[];
  visibleCapabilityHistory: DeviceCapabilityHistoryPointDto[];
  collapsedItemCount: number;
  showAllCapabilityHistory: boolean;
  onToggleShowAllCapabilityHistory: () => void;
  formatCapabilityState: (state: unknown) => string;
};

export function DeviceCapabilityHistorySection({
  historyFilterEndpoint,
  onHistoryFilterEndpointChange,
  capabilityEndpointFilterOptions,
  selectedCapabilityForHistory,
  onSelectedCapabilityForHistoryChange,
  historyCapabilityOptions,
  historyFrom,
  onHistoryFromChange,
  historyTo,
  onHistoryToChange,
  isCapabilityHistoryLoading,
  capabilityHistoryError,
  capabilityHistoryTotalCount,
  capabilityHistoryPage,
  capabilityHistoryPageSize,
  capabilityHistory,
  visibleCapabilityHistory,
  collapsedItemCount,
  showAllCapabilityHistory,
  onToggleShowAllCapabilityHistory,
  formatCapabilityState,
}: Props) {
  const { t } = useTranslation("devices");

  const endpointLabelByKey = useMemo(
    () => new Map(capabilityEndpointFilterOptions.map((option) => [option.value, option.label])),
    [capabilityEndpointFilterOptions]
  );

  function getCapabilityScopeLabel(endpointId: string) {
    const endpointKey = toEndpointKey(endpointId);

    return endpointKey === DEVICE_LEVEL_ENDPOINT_KEY
      ? t("deviceLevel")
      : endpointLabelByKey.get(endpointKey) ?? endpointKey;
  }

  return (
    <section className={`${styles.section} ${pageStyles.panel}`}>
      <div className={styles.sectionHeader}>
        <h2 className={styles.sectionTitle}>{t("history")}</h2>
      </div>

      <div className={pageStyles.filterPanel}>
        <div className={pageStyles.filterRow}>
          <div className={pageStyles.filterField}>
            <label className={pageStyles.filterLabel} htmlFor="history-endpoint-filter">{t("endpoint")}</label>
            <select
              id="history-endpoint-filter"
              className={styles.select}
              value={historyFilterEndpoint}
              onChange={(event) => onHistoryFilterEndpointChange(event.target.value)}
            >
              <option value="all">{t("allEndpoints")}</option>
              {capabilityEndpointFilterOptions.map((endpoint) => (
                <option key={endpoint.value} value={endpoint.value}>{endpoint.label}</option>
              ))}
            </select>
          </div>

          <div className={pageStyles.filterField}>
            <label className={pageStyles.filterLabel} htmlFor="history-capability-filter">{t("capability")}</label>
            <select
              id="history-capability-filter"
              className={styles.select}
              value={selectedCapabilityForHistory}
              onChange={(event) => onSelectedCapabilityForHistoryChange(event.target.value)}
            >
              {historyCapabilityOptions.length === 0 ? (
                <option value="all">{t("noCapabilityAvailable")}</option>
              ) : (
                historyCapabilityOptions.map((capability) => (
                  <option key={capability.id} value={capability.id}>
                    {getCapabilityScopeLabel(capability.endpointId)} · {getCapabilityDisplayLabel(
                      t,
                      capability.capabilityId,
                      capability.metadata?.defaultName
                    )}
                  </option>
                ))
              )}
            </select>
          </div>
        </div>

        <div className={pageStyles.filterDateRow}>
          <div className={pageStyles.filterField}>
            <label className={pageStyles.filterLabel} htmlFor="history-from-filter">{t("from")}</label>
            <Input
              id="history-from-filter"
              type="date"
              value={historyFrom}
              onChange={(event) => onHistoryFromChange(event.target.value)}
            />
          </div>

          <div className={pageStyles.filterField}>
            <label className={pageStyles.filterLabel} htmlFor="history-to-filter">{t("to")}</label>
            <Input
              id="history-to-filter"
              type="date"
              value={historyTo}
              onChange={(event) => onHistoryToChange(event.target.value)}
            />
          </div>
        </div>
      </div>

      {isCapabilityHistoryLoading ? (
        <div className={styles.emptyState}>{t("loadingHistory")}</div>
      ) : capabilityHistoryError ? (
        <div className={styles.emptyState}>{capabilityHistoryError}</div>
      ) : capabilityHistory.length === 0 ? (
        <div className={styles.emptyState}>{t("noHistoryInRange")}</div>
      ) : (
        <>
          <div className={pageStyles.mutedText}>
            {t("page")} {capabilityHistoryPage} · {t("pageSize")} {capabilityHistoryPageSize} · {t("total")} {capabilityHistoryTotalCount}
          </div>

          <CellGrid>
            {visibleCapabilityHistory.map((point, index) => (
              <Cell
                key={`${point.reportedAt}-${index}`}
                id={`${point.reportedAt}-${index}`}
                title={timestampToDateTime(point.reportedAt)}
                subtitle={
                  <>
                    <div>{t("state")}: {formatCapabilityState(point.state)}</div>
                    <div>{t("capability")}: {getCapabilityDisplayLabel(t, point.capabilityId)}</div>
                    <div>{getCapabilityScopeLabel(point.endpointId)}</div>
                    <div>{t("historyId")}: {point.id}</div>
                  </>
                }
                onClick={() => void 0}
                disabled={true}
              />
            ))}
          </CellGrid>

          {capabilityHistory.length > collapsedItemCount ? (
            <div className={pageStyles.expandRow}>
              <Button
                size="sm"
                variant="secondary"
                onClick={onToggleShowAllCapabilityHistory}
              >
                {showAllCapabilityHistory
                  ? t("showLess")
                  : t("showMoreCount", { count: capabilityHistory.length - visibleCapabilityHistory.length })}
              </Button>
            </div>
          ) : null}
        </>
      )}
    </section>
  );
}
