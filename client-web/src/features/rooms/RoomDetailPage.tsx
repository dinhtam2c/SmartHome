import { useNavigate, useParams } from "react-router-dom";
import { Button } from "@/components/Button";
import { useTranslation } from "react-i18next";
import { DetailRow } from "@/components/DetailRow";
import { DetailsView } from "@/components/DetailsView";
import { PageHeader } from "@/components/PageHeader";
import { RoomDevicesSection } from "./components/RoomDevicesSection";
import { EditRoomModal } from "./components/EditRoomModal";
import { CreateDeviceModal } from "./components/CreateDeviceModal";
import { useRoomDetailPageController } from "./hooks/useRoomDetailPageController";
import styles from "@/features/shared/featurePage.module.css";
import pageStyles from "./RoomDetailPage.module.css";

export function RoomDetailPage() {
  const { homeId, roomId } = useParams();
  const navigate = useNavigate();
  const { t } = useTranslation("rooms");
  const vm = useRoomDetailPageController({
    homeId: homeId ?? null,
    roomId: roomId ?? null,
    navigateTo: (to) => navigate(to),
    confirmDelete: (message) => window.confirm(message),
    t,
  });

  if (vm.isLoading) {
    return <div className={styles.emptyState}>{t("loading")}</div>;
  }

  if (vm.error) {
    return <div className={styles.emptyState}>{t("failed")}</div>;
  }

  if (!vm.room) {
    return <div className={styles.emptyState}>{t("notFound")}</div>;
  }

  return (
    <div className={styles.pageStack}>
      <PageHeader
        title={(
          <span className={pageStyles.roomTitleRow}>
            <span>{vm.room.name}</span>
            <button
              type="button"
              className={pageStyles.renameIconButton}
              onClick={vm.openEditNameModal}
              aria-label={t("editNameOnly")}
              title={t("editNameOnly")}
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
            <Button variant="secondary" size="sm" onClick={() => navigate(`/homes/${homeId}`)}>{t("back")}</Button>
            <Button size="sm" onClick={vm.openCreateDeviceModal}>
              {t("addDevice")}
            </Button>
            <Button
              variant="danger"
              size="sm"
              disabled={vm.isDeleting || !vm.canDeleteRoom}
              title={!vm.canDeleteRoom ? t("cannotDelete") : t("delete")}
              onClick={() => void vm.handleDeleteRoom()}
            >
              {vm.isDeleting ? t("deleting") : t("delete")}
            </Button>
          </div>
        }
      />

      <DetailsView>
        <DetailRow label={t("homeParent")}>{vm.home?.name ?? t("unknownHome")}</DetailRow>
        <DetailRow
          label={t("description")}
          onLabelClick={vm.openEditDescriptionModal}
          labelActionTitle={t("editDescriptionOnly")}
        >
          {vm.room.description ?? t("noDescription")}
        </DetailRow>
        <DetailRow label={t("onlineDevices")}>{vm.room.onlineDeviceCount}/{vm.room.deviceCount}</DetailRow>
        {vm.room.temperature !== undefined && vm.room.temperature !== null && (
          <DetailRow label={t("temperature")}>{vm.room.temperature}°C</DetailRow>
        )}
        {vm.room.humidity !== undefined && vm.room.humidity !== null && (
          <DetailRow label={t("humidity")}>{vm.room.humidity}%</DetailRow>
        )}
      </DetailsView>

      <RoomDevicesSection
        room={vm.room}
        onOpenDevice={(deviceId) =>
          navigate(`/homes/${homeId}/rooms/${roomId}/devices/${deviceId}`)
        }
        formatCapabilityStatePreview={vm.formatCapabilityStatePreview}
      />

      <EditRoomModal
        open={vm.isEditOpen}
        onClose={vm.closeEditModal}
        onSubmit={vm.handleSaveRoom}
        title={vm.editMode === "name" ? t("editNameOnly") : t("editDescriptionOnly")}
        showName={vm.editMode === "name"}
        showDescription={vm.editMode === "description"}
        name={vm.editName}
        onNameChange={vm.setEditName}
        description={vm.editDescription}
        onDescriptionChange={vm.setEditDescription}
        isSaving={vm.isSaving}
        error={vm.editError}
      />

      <CreateDeviceModal
        open={vm.isCreateDeviceOpen}
        onClose={vm.closeCreateDeviceModal}
        onSubmit={vm.handleCreateDevice}
        provisionCode={vm.newDeviceProvisionCode}
        onProvisionCodeChange={vm.setNewDeviceProvisionCode}
        isSaving={vm.isCreatingDevice}
        error={vm.createDeviceError}
      />
    </div>
  );
}
