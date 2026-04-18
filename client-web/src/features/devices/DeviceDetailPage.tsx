import { useNavigate, useParams } from "react-router-dom";
import { Button } from "@/components/Button";
import { DetailRow } from "@/components/DetailRow";
import { DetailsView } from "@/components/DetailsView";
import { Form } from "@/components/Form";
import { FormGroup } from "@/components/FormGroup";
import { Input } from "@/components/Input";
import { Modal } from "@/components/Modal";
import { PageHeader } from "@/components/PageHeader";
import { StatusChip } from "@/components/StatusChip";
import { formatDuration, timestampToDateTime } from "@/utils/dateTimeUtils";
import { DeviceRecentCommandsSection } from "./components/DeviceRecentCommandsSection";
import { DeviceCapabilityHistorySection } from "./components/DeviceCapabilityHistorySection";
import { DeviceCapabilitiesSection } from "./components/DeviceCapabilitiesSection";
import { useDeviceDetailPage } from "./hooks/useDeviceDetailPage";
import styles from "@/features/shared/featurePage.module.css";
import pageStyles from "./DeviceDetailPage.module.css";
import { useTranslation } from "react-i18next";

export function DeviceDetailPage() {
  const { t } = useTranslation("devices");

  const { homeId, roomId: routeRoomId, deviceId } = useParams();
  const navigate = useNavigate();

  const vm = useDeviceDetailPage({
    deviceId: deviceId ?? null,
    homeId: homeId ?? null,
    routeRoomId: routeRoomId ?? null,
    navigateTo: (to) => navigate(to),
    t,
  });

  const homeBackPath = homeId ? `/homes/${homeId}` : "/homes";
  const roomBackPath =
    homeId && routeRoomId
      ? `/homes/${homeId}/rooms/${routeRoomId}`
      : null;

  if (vm.isLoading) {
    return <div className={styles.emptyState}>{t("loading")}</div>;
  }

  if (vm.error) {
    return <div className={styles.emptyState}>{t("failed")}</div>;
  }

  if (!vm.device) {
    return <div className={styles.emptyState}>{t("notFound")}</div>;
  }

  return (
    <div className={styles.pageStack}>
      <PageHeader
        title={(
          <span className={pageStyles.deviceTitleRow}>
            <span>{vm.device.name}</span>
            <button
              type="button"
              className={pageStyles.renameIconButton}
              onClick={vm.openEditNameModal}
              aria-label={t("renameAction")}
              title={t("renameAction")}
            >
              <svg
                className={pageStyles.renameIcon}
                viewBox="0 0 24 24"
                fill="none"
                xmlns="http://www.w3.org/2000/svg"
                aria-hidden="true"
              >
                <path
                  d="M15.2 5.2L18.8 8.8M6 18L9.7 17.2L18.1 8.8C18.6 8.3 18.6 7.5 18.1 7L17 5.9C16.5 5.4 15.7 5.4 15.2 5.9L6.8 14.3L6 18Z"
                  stroke="currentColor"
                  strokeWidth="1.8"
                  strokeLinecap="round"
                  strokeLinejoin="round"
                />
              </svg>
            </button>
          </span>
        )}
        action={
          <div className={styles.metaRow}>
            {homeId ? (
              <Button
                variant="secondary"
                size="sm"
                onClick={() => navigate(homeBackPath)}
              >
                {t("backToHome")}
              </Button>
            ) : (
              <Button
                variant="secondary"
                size="sm"
                onClick={() => navigate(vm.backPath)}
              >
                {t("back")}
              </Button>
            )}

            {roomBackPath ? (
              <Button
                variant="secondary"
                size="sm"
                onClick={() => navigate(roomBackPath)}
              >
                {t("backToRoom")}
              </Button>
            ) : null}
            <Button
              variant="secondary"
              size="sm"
              onClick={vm.openAssignRoomModal}
              disabled={!vm.device.homeId}
            >
              {t("assignRoom")}
            </Button>
            <Button
              variant="danger"
              size="sm"
              disabled={vm.isDeleteBusy}
              onClick={() => void vm.handleDeleteDevice()}
            >
              {vm.isDeleteBusy ? t("deleting") : t("delete")}
            </Button>
          </div>
        }
      />

      <div className={pageStyles.columns}>
        <div className={`${pageStyles.leftColumn} ${pageStyles.fullWidthColumn}`}>
          <section className={pageStyles.panel}>
            <DetailsView>
              <DetailRow label={t("status")}>
                <div className={pageStyles.statusRow}>
                  <StatusChip
                    label={vm.device.isOnline ? t("online") : t("offline")}
                    tone={vm.device.isOnline ? "online" : "offline"}
                  />
                </div>
              </DetailRow>
              <DetailRow label={t("home")}>{vm.device.homeName}</DetailRow>
              <DetailRow label={t("room")}>{vm.device.roomName ?? t("unassigned")}</DetailRow>
              <DetailRow label={t("lastSeenAt")}>{timestampToDateTime(vm.device.lastSeenAt)}</DetailRow>
              {vm.device.isOnline ? (
                <DetailRow label={t("uptime")}>{formatDuration(vm.device.uptime)}</DetailRow>
              ) : null}
            </DetailsView>

            <details className={pageStyles.advancedDetails}>
              <summary>{t("moreDetails")}</summary>
              <DetailsView>
                <DetailRow label={t("deviceId")}>{vm.device.id}</DetailRow>
                <DetailRow label={t("firmware")}>{vm.device.firmwareVersion}</DetailRow>
                <DetailRow label={t("provisionedAt")}>{vm.device.provisionedAt ? timestampToDateTime(vm.device.provisionedAt) : t("notAvailable")}</DetailRow>
              </DetailsView>
            </details>
          </section>

          <DeviceCapabilitiesSection
            capabilityGroups={vm.capabilityGroups}
            canControlDevice={vm.canControlDevice}
            quickToggleBusyCapabilityId={vm.quickToggleBusyCapabilityId}
            optimisticToggleValues={vm.optimisticToggleValues}
            inlineCommandValues={vm.inlineCommandValues}
            setInlineCommandValues={vm.setInlineCommandValues}
            onBooleanToggleSend={(...args) => void vm.handleBooleanToggleSend(...args)}
            onScheduleInlineCommandSend={(...args) => vm.scheduleInlineCommandSend(...args)}
            onInlineCommandSend={(...args) => void vm.handleInlineCommandSend(...args)}
            quickActionError={vm.quickActionError ? t(vm.quickActionError, { defaultValue: vm.quickActionError }) : null}
            formatCapabilityState={vm.formatCapabilityState}
          />
        </div>
      </div>

      <div className={pageStyles.historyActions}>
        <Button
          size="sm"
          variant="secondary"
          onClick={vm.openCommandHistory}
        >
          {t("viewCommandHistory")}
        </Button>

        <Button
          size="sm"
          variant="secondary"
          onClick={vm.openCapabilityHistory}
        >
          {t("viewCapabilityHistory")}
        </Button>
      </div>

      <Modal
        open={vm.isCommandHistoryVisible}
        title={t("commands")}
        onClose={vm.closeCommandHistory}
      >
        <DeviceRecentCommandsSection
          commandFilterEndpoint={vm.commandFilterEndpoint}
          onCommandFilterEndpointChange={vm.setCommandFilterEndpoint}
          capabilityEndpointFilterOptions={vm.capabilityEndpointFilterOptions}
          commandFilterStatus={vm.commandFilterStatus}
          onCommandFilterStatusChange={vm.setCommandFilterStatus}
          commandFilterCapability={vm.commandFilterCapability}
          onCommandFilterCapabilityChange={vm.setCommandFilterCapability}
          capabilityIdFilterOptions={vm.capabilityIdFilterOptions}
          commandLimit={vm.commandLimit}
          onCommandLimitChange={vm.setCommandLimit}
          commandFrom={vm.commandFrom}
          onCommandFromChange={vm.setCommandFrom}
          commandTo={vm.commandTo}
          onCommandToChange={vm.setCommandTo}
          commandError={vm.commandError ? t(vm.commandError, { defaultValue: vm.commandError }) : null}
          commandTotalCount={vm.commandTotalCount}
          commandPage={vm.commandPage}
          commandPageSize={vm.commandPageSize}
          filteredCommandHistory={vm.filteredCommandHistory}
          visibleCommandHistory={vm.visibleCommandHistory}
          collapsedItemCount={vm.collapsedItemCount}
          showAllCommands={vm.showAllCommands}
          onToggleShowAllCommands={vm.toggleShowAllCommands}
          getCommandStatusTone={vm.getCommandStatusTone}
          getCommandStatusLabel={vm.getCommandStatusLabel}
          getCommandEndpointLabel={vm.getCommandEndpointLabel}
          summarizePayload={vm.summarizePayload}
        />
      </Modal>

      <Modal
        open={vm.isCapabilityHistoryVisible}
        title={t("history")}
        onClose={vm.closeCapabilityHistory}
      >
        <DeviceCapabilityHistorySection
          historyFilterEndpoint={vm.historyFilterEndpoint}
          onHistoryFilterEndpointChange={vm.setHistoryFilterEndpoint}
          capabilityEndpointFilterOptions={vm.capabilityEndpointFilterOptions}
          selectedCapabilityForHistory={vm.selectedCapabilityForHistory}
          onSelectedCapabilityForHistoryChange={vm.setSelectedCapabilityForHistory}
          historyCapabilityOptions={vm.historyCapabilityOptions}
          historyFrom={vm.historyFrom}
          onHistoryFromChange={vm.setHistoryFrom}
          historyTo={vm.historyTo}
          onHistoryToChange={vm.setHistoryTo}
          isCapabilityHistoryLoading={vm.isCapabilityHistoryLoading}
          capabilityHistoryError={vm.capabilityHistoryError ? t(vm.capabilityHistoryError, { defaultValue: vm.capabilityHistoryError }) : null}
          capabilityHistoryTotalCount={vm.capabilityHistoryTotalCount}
          capabilityHistoryPage={vm.capabilityHistoryPage}
          capabilityHistoryPageSize={vm.capabilityHistoryPageSize}
          capabilityHistory={vm.capabilityHistory}
          visibleCapabilityHistory={vm.visibleCapabilityHistory}
          collapsedItemCount={vm.collapsedItemCount}
          showAllCapabilityHistory={vm.showAllCapabilityHistory}
          onToggleShowAllCapabilityHistory={vm.toggleShowAllCapabilityHistory}
          formatCapabilityState={vm.formatCapabilityState}
        />
      </Modal>

      <Modal open={vm.isEditNameOpen} title={t("editNameTitle")} onClose={vm.closeEditNameModal}>
        <Form onSubmit={vm.handleSaveName}>
          <FormGroup label={t("nameInputLabel")}
            htmlFor="edit-device-name">
            <Input
              id="edit-device-name"
              value={vm.editName}
              onChange={(event) => vm.setEditName(event.target.value)}
              required
            />
          </FormGroup>

          {vm.actionError ? <p className={styles.helperText}>{t(vm.actionError, { defaultValue: vm.actionError })}</p> : null}

          <div className={styles.metaRow}>
            <Button type="submit" disabled={vm.isSaving}>{vm.isSaving ? t("saving") : t("save")}</Button>
            <Button type="button" variant="secondary" onClick={vm.closeEditNameModal}>{t("cancel")}</Button>
          </div>
        </Form>
      </Modal>

      <Modal open={vm.isAssignRoomOpen} title={t("assignRoomTitle")} onClose={vm.closeAssignRoomModal}>
        <Form onSubmit={vm.handleAssignRoom}>
          <FormGroup label={t("room")}
            htmlFor="assign-device-room">
            <select
              id="assign-device-room"
              className={styles.select}
              value={vm.roomId}
              onChange={(event) => vm.setRoomId(event.target.value)}
              required
            >
              <option value="" disabled>{t("selectRoom")}</option>
              {vm.homeRooms.map((room) => (
                <option key={room.id} value={room.id}>{room.name}</option>
              ))}
            </select>
          </FormGroup>

          {vm.actionError ? <p className={styles.helperText}>{t(vm.actionError, { defaultValue: vm.actionError })}</p> : null}

          <div className={styles.metaRow}>
            <Button type="submit" disabled={vm.isSaving}>{vm.isSaving ? t("assigning") : t("assign")}</Button>
            <Button type="button" variant="secondary" onClick={vm.closeAssignRoomModal}>{t("cancel")}</Button>
          </div>
        </Form>
      </Modal>
    </div>
  );
}
