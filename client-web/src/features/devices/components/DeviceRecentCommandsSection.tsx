import { Button } from "@/components/Button";
import { Cell } from "@/components/Cell";
import { CellGrid } from "@/components/CellGrid";
import { Input } from "@/components/Input";
import { StatusChip } from "@/components/StatusChip";
import { getCapabilityDisplayLabel } from "@/features/capabilities";
import { timestampToDateTime } from "@/utils/dateTimeUtils";
import type { DeviceCommandExecutionDto, DeviceCommandStatus } from "../devices.types";
import styles from "@/features/shared/featurePage.module.css";
import pageStyles from "../DeviceDetailPage.module.css";
import { useTranslation } from "react-i18next";

type Props = {
  commandFilterEndpoint: string;
  onCommandFilterEndpointChange: (value: string) => void;
  capabilityEndpointFilterOptions: Array<{ value: string; label: string; }>;
  commandFilterStatus: "all" | DeviceCommandStatus;
  onCommandFilterStatusChange: (value: "all" | DeviceCommandStatus) => void;
  commandFilterCapability: string;
  onCommandFilterCapabilityChange: (value: string) => void;
  capabilityIdFilterOptions: string[];
  commandLimit: number;
  onCommandLimitChange: (value: number) => void;
  commandFrom: string;
  onCommandFromChange: (value: string) => void;
  commandTo: string;
  onCommandToChange: (value: string) => void;
  commandError: string | null;
  commandTotalCount: number;
  commandPage: number;
  commandPageSize: number;
  filteredCommandHistory: DeviceCommandExecutionDto[];
  visibleCommandHistory: DeviceCommandExecutionDto[];
  collapsedItemCount: number;
  showAllCommands: boolean;
  onToggleShowAllCommands: () => void;
  getCommandStatusTone: (status: DeviceCommandStatus) => "pending" | "completed" | "failed" | "neutral";
  getCommandStatusLabel: (status: DeviceCommandStatus) => string;
  getCommandEndpointLabel: (command: DeviceCommandExecutionDto) => string;
  summarizePayload: (payload: string | null) => string;
};

export function DeviceRecentCommandsSection({
  commandFilterEndpoint,
  onCommandFilterEndpointChange,
  capabilityEndpointFilterOptions,
  commandFilterStatus,
  onCommandFilterStatusChange,
  commandFilterCapability,
  onCommandFilterCapabilityChange,
  capabilityIdFilterOptions,
  commandLimit,
  onCommandLimitChange,
  commandFrom,
  onCommandFromChange,
  commandTo,
  onCommandToChange,
  commandError,
  commandTotalCount,
  commandPage,
  commandPageSize,
  filteredCommandHistory,
  visibleCommandHistory,
  collapsedItemCount,
  showAllCommands,
  onToggleShowAllCommands,
  getCommandStatusTone,
  getCommandStatusLabel,
  getCommandEndpointLabel,
  summarizePayload,
}: Props) {
  const { t } = useTranslation("devices");

  const getOperationDisplayLabel = (operation: string) => {
    const normalized = operation.trim();

    if (!normalized) {
      return operation;
    }

    return t(`operationKeyLabels.${normalized.toLowerCase()}`, {
      defaultValue: normalized,
    });
  };

  return (
    <section className={`${styles.section} ${pageStyles.panel}`}>
      <div className={styles.sectionHeader}>
        <h2 className={styles.sectionTitle}>{t("commands")}</h2>
      </div>

      <div className={pageStyles.filterPanel}>
        <div className={pageStyles.filterRow}>
          <div className={pageStyles.filterField}>
            <label className={pageStyles.filterLabel} htmlFor="command-endpoint-filter">
              {t("endpoint")}
            </label>
            <select
              id="command-endpoint-filter"
              className={styles.select}
              value={commandFilterEndpoint}
              onChange={(event) => onCommandFilterEndpointChange(event.target.value)}
            >
              <option value="all">
                {t("allEndpoints")}
              </option>
              {capabilityEndpointFilterOptions.map((endpoint) => (
                <option key={endpoint.value} value={endpoint.value}>
                  {endpoint.label}
                </option>
              ))}
            </select>
          </div>

          <div className={pageStyles.filterField}>
            <label className={pageStyles.filterLabel} htmlFor="command-capability-filter">
              {t("capability")}
            </label>
            <select
              id="command-capability-filter"
              className={styles.select}
              value={commandFilterCapability}
              onChange={(event) => onCommandFilterCapabilityChange(event.target.value)}
            >
              <option value="all">
                {t("allCapabilities")}
              </option>
              {capabilityIdFilterOptions.map((capabilityId) => (
                <option key={capabilityId} value={capabilityId}>
                  {getCapabilityDisplayLabel(t, capabilityId)}
                </option>
              ))}
            </select>
          </div>

          <div className={pageStyles.filterField}>
            <label className={pageStyles.filterLabel} htmlFor="command-status-filter">
              {t("status")}
            </label>
            <select
              id="command-status-filter"
              className={styles.select}
              value={commandFilterStatus}
              onChange={(event) =>
                onCommandFilterStatusChange(event.target.value as "all" | DeviceCommandStatus)
              }
            >
              <option value="all">
                {t("allStatuses")}
              </option>
              <option value="Pending">
                {getCommandStatusLabel("Pending")}
              </option>
              <option value="Accepted">
                {getCommandStatusLabel("Accepted")}
              </option>
              <option value="Completed">
                {getCommandStatusLabel("Completed")}
              </option>
              <option value="Failed">
                {getCommandStatusLabel("Failed")}
              </option>
              <option value="TimedOut">
                {getCommandStatusLabel("TimedOut")}
              </option>
            </select>
          </div>

          <div className={pageStyles.filterFieldSmall}>
            <label className={pageStyles.filterLabel} htmlFor="command-limit-filter">
              {t("range")}
            </label>
            <select
              id="command-limit-filter"
              className={styles.select}
              value={String(commandLimit)}
              onChange={(event) => onCommandLimitChange(Number(event.target.value))}
            >
              <option value="20">
                {t("last20")}
              </option>
              <option value="50">
                {t("last50")}
              </option>
              <option value="100">
                {t("last100")}
              </option>
            </select>
          </div>
        </div>

        <div className={pageStyles.filterDateRow}>
          <div className={pageStyles.filterField}>
            <label className={pageStyles.filterLabel} htmlFor="command-from-filter">
              {t("from")}
            </label>
            <Input
              id="command-from-filter"
              type="date"
              value={commandFrom}
              onChange={(event) => onCommandFromChange(event.target.value)}
            />
          </div>

          <div className={pageStyles.filterField}>
            <label className={pageStyles.filterLabel} htmlFor="command-to-filter">
              {t("to")}
            </label>
            <Input
              id="command-to-filter"
              type="date"
              value={commandTo}
              onChange={(event) => onCommandToChange(event.target.value)}
            />
          </div>
        </div>
      </div>

      {commandError ? <div className={styles.emptyState}>{commandError}</div> : null}

      {!commandError ? (
        <div className={pageStyles.mutedText}>
          {t("page")} {commandPage} · {t("pageSize")} {commandPageSize} · {t("total")} {commandTotalCount}
        </div>
      ) : null}

      {filteredCommandHistory.length === 0 ? (
        <div className={styles.emptyState}>{t("noCommands")}</div>
      ) : (
        <>
          <CellGrid>
            {visibleCommandHistory.map((command) => (
              <Cell
                key={command.id}
                id={command.id}
                title={getOperationDisplayLabel(command.operation)}
                subtitle={
                  <>
                    <div>
                      <StatusChip label={getCommandStatusLabel(command.status)} tone={getCommandStatusTone(command.status)} />
                    </div>
                    <div>{t("endpoint")}: {getCommandEndpointLabel(command)}</div>
                    <div>
                      {t("capability")}: {getCapabilityDisplayLabel(t, command.capabilityId)}
                    </div>
                    <div>{t("requested")}: {timestampToDateTime(command.requestedAt)}</div>
                    {command.error && <div>{t("error")}: {command.error}</div>}

                    <details className={pageStyles.advancedDetails}>
                      <summary>{t("payloadDetails")}</summary>
                      <div className={pageStyles.capabilityLine}>{t("request")}: {summarizePayload(command.requestPayload)}</div>
                      <div className={pageStyles.capabilityLine}>{t("result")}: {summarizePayload(command.resultPayload)}</div>
                    </details>
                  </>
                }
                onClick={() => void 0}
                disabled={false}
              />
            ))}
          </CellGrid>

          {filteredCommandHistory.length > collapsedItemCount ? (
            <div className={pageStyles.expandRow}>
              <Button
                size="sm"
                variant="secondary"
                onClick={onToggleShowAllCommands}
              >
                {showAllCommands
                  ? t("showLess")
                  : t("showMoreCount", { count: filteredCommandHistory.length - visibleCommandHistory.length })}
              </Button>
            </div>
          ) : null}
        </>
      )}
    </section>
  );
}
