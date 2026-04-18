import { useTranslation } from "react-i18next";
import { useNavigate, useParams } from "react-router-dom";
import { Button } from "@/components/Button";
import { DetailRow } from "@/components/DetailRow";
import { DetailsView } from "@/components/DetailsView";
import { PageHeader } from "@/components/PageHeader";
import { SceneUpsertModal } from "@/features/scenes/components/SceneUpsertModal";
import { HomeRoomsSection } from "./components/HomeRoomsSection";
import { HomeQuickActionsSection } from "./components/HomeQuickActionsSection";
import { HomeUnassignedDevicesSection } from "./components/HomeUnassignedDevicesSection";
import { EditHomeModal } from "./components/EditHomeModal";
import { EditRoomModal } from "./components/EditRoomModal";
import { CreateRoomModal } from "./components/CreateRoomModal";
import { CreateDeviceModal } from "./components/CreateDeviceModal";
import { useHomeDetailPageController } from "./hooks/useHomeDetailPageController";
import styles from "@/features/shared/featurePage.module.css";
import pageStyles from "./HomeDetailPage.module.css";

export function HomeDetailPage() {
  const { t } = useTranslation("homes");
  const { homeId } = useParams();
  const navigate = useNavigate();
  const vm = useHomeDetailPageController({
    homeId: homeId ?? null,
    navigateTo: (to) => navigate(to),
    confirmDelete: (message) => window.confirm(message),
    t,
  });

  if (vm.isLoading) {
    return <div className={styles.emptyState}>{t('detail.loading')}</div>;
  }

  if (vm.error) {
    return <div className={styles.emptyState}>{t('detail.failed')}</div>;
  }

  if (!vm.home) {
    return (
      <div className={styles.emptyState}>
        {t('detail.notFound')}
      </div>
    );
  }

  const home = vm.home;

  return (
    <div className={styles.pageStack}>
      <PageHeader
        title={(
          <span className={pageStyles.homeTitleRow}>
            <span>{home.name}</span>
            <button
              type="button"
              className={pageStyles.renameIconButton}
              onClick={vm.openEditNameModal}
              aria-label={t("detail.editName")}
              title={t("detail.editName")}
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
            <Button
              variant="secondary"
              size="sm"
              onClick={() =>
                navigate("/homes", {
                  state: { skipAutoEnter: true },
                })
              }
            >
              {t('detail.back')}
            </Button>
            <Button size="sm" onClick={vm.openCreateDeviceModal}>
              {t('detail.addDevice')}
            </Button>
            <Button
              variant="danger"
              size="sm"
              disabled={vm.isDeleteBusy || !vm.canDeleteHome}
              onClick={() => void vm.handleDeleteHome()}
              title={!vm.canDeleteHome ? t('detail.cannotDelete') : t('detail.delete')}
            >
              {vm.isDeleteBusy ? t('detail.deleting') : t('detail.delete')}
            </Button>
          </div>
        }
      />

      <DetailsView>
        <DetailRow
          label={t('detail.description')}
          onLabelClick={vm.openEditDescriptionModal}
          labelActionTitle={t('detail.editDescription')}
        >
          {home.description ?? t('noDescription')}
        </DetailRow>
        <DetailRow label={t('detail.rooms')}>{home.roomCount}</DetailRow>
        <DetailRow label={t('detail.onlineDevices')}>{home.onlineDeviceCount}/{home.deviceCount}</DetailRow>
      </DetailsView>

      <HomeQuickActionsSection
        quickActions={vm.quickActions}
        executingQuickActionId={vm.executingQuickActionId}
        deletingQuickActionId={vm.deletingQuickActionId}
        onAddQuickAction={() => void vm.openCreateQuickActionModal()}
        onExecuteQuickAction={(sceneId) => void vm.handleExecuteQuickAction(sceneId)}
        onOpenQuickActionEdit={(sceneId) => void vm.openEditQuickActionModal(sceneId)}
      />

      <HomeRoomsSection
        home={home}
        onAddRoom={vm.openCreateRoomModal}
        onOpenRoom={(selectedRoomId) =>
          navigate(`/homes/${home.id}/rooms/${selectedRoomId}`)
        }
        onOpenRoomEdit={vm.openEditRoomModal}
      />

      {home.unassignedDevices.length > 0 ? (
        <HomeUnassignedDevicesSection
          devices={home.unassignedDevices}
          onOpenDevice={(deviceId) => navigate(`/homes/${home.id}/devices/${deviceId}`)}
          formatCapabilityStatePreview={vm.formatCapabilityStatePreview}
        />
      ) : null}

      <EditHomeModal
        open={vm.isEditOpen}
        onClose={vm.closeEditModal}
        onSubmit={vm.handleSaveHome}
        title={vm.editHomeMode === "name" ? t("detail.editName") : t("detail.editDescription")}
        showName={vm.editHomeMode === "name"}
        showDescription={vm.editHomeMode === "description"}
        name={vm.editName}
        onNameChange={vm.setEditName}
        description={vm.editDescription}
        onDescriptionChange={vm.setEditDescription}
        isSaving={vm.isSaving}
        error={vm.editError}
      />

      <EditRoomModal
        open={vm.isEditRoomOpen}
        onClose={vm.closeEditRoomModal}
        onSubmit={vm.handleSaveEditedRoom}
        name={vm.editRoomName}
        onNameChange={vm.setEditRoomName}
        description={vm.editRoomDescription}
        onDescriptionChange={vm.setEditRoomDescription}
        isSaving={vm.isSaving}
        error={vm.editRoomError}
      />

      <CreateRoomModal
        open={vm.isCreateRoomOpen}
        onClose={vm.closeCreateRoomModal}
        onSubmit={vm.handleCreateRoom}
        name={vm.newRoomName}
        onNameChange={vm.setNewRoomName}
        description={vm.newRoomDescription}
        onDescriptionChange={vm.setNewRoomDescription}
        isSaving={vm.isSaving}
        error={vm.roomError}
      />

      <CreateDeviceModal
        open={vm.isCreateDeviceOpen}
        onClose={vm.closeCreateDeviceModal}
        onSubmit={vm.handleCreateDevice}
        roomId={vm.newDeviceRoomId}
        onRoomIdChange={vm.setNewDeviceRoomId}
        rooms={home.rooms}
        provisionCode={vm.newDeviceProvisionCode}
        onProvisionCodeChange={vm.setNewDeviceProvisionCode}
        isSaving={vm.isCreatingDevice}
        error={vm.deviceError}
      />

      <SceneUpsertModal
        open={vm.isSceneModalOpen}
        mode={vm.sceneModalMode}
        title={vm.sceneModalMode === "create" ? t("scenes.create") : t("scenes.edit")}
        submitLabel={t("scenes.saveShort")}
        submittingLabel={t("scenes.saving")}
        name={vm.sceneName}
        description={vm.sceneDescription}
        isEnabled={vm.sceneIsEnabled}
        targets={vm.sceneTargets}
        sideEffects={vm.sceneSideEffects}
        rooms={vm.sceneRooms}
        availableDevices={vm.sceneBuilderDevices}
        availableDevicesByRoom={vm.sceneBuilderDevicesByRoom}
        registryMap={vm.sceneRegistryMap}
        isSaving={vm.isSceneSaving}
        isDeleting={vm.isSceneDeleting}
        error={
          vm.sceneModalError ||
          vm.sceneBuilderDevicesError ||
          vm.sceneRegistryError
        }
        onClose={vm.closeSceneModal}
        onSubmit={vm.handleSaveQuickAction}
        onDelete={vm.handleDeleteSceneFromModal}
        deleteLabel={t("scenes.deleteShort")}
        deletingLabel={t("scenes.deleting")}
        onNameChange={vm.setSceneName}
        onDescriptionChange={vm.setSceneDescription}
        onEnabledChange={vm.setSceneIsEnabled}
        onChangeTarget={vm.handleChangeSceneTarget}
        onAddTarget={vm.handleAddSceneTarget}
        onRemoveTarget={vm.handleRemoveSceneTarget}
        onChangeSideEffect={vm.handleChangeSceneSideEffect}
        onAddSideEffect={vm.handleAddSceneSideEffect}
        onRemoveSideEffect={vm.handleRemoveSceneSideEffect}
      />
    </div>
  );
}
