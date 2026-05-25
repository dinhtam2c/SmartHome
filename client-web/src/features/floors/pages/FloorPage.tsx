import { useCallback, useEffect, useMemo } from "react";
import { useNavigate, useParams } from "react-router-dom";
import { useTranslation } from "react-i18next";
import { Button } from "@/shared/ui/Button";
import { PageHeader } from "@/shared/ui/PageHeader";
import { Spinner } from "@/shared/ui/Spinner";
import { useHomeDetail } from "@/features/homes";
import sharedStyles from "@/shared/styles/featurePage.module.css";
import { ApiError } from "@/shared/api/http";
import {
  MIN_CANVAS_HEIGHT,
  MIN_CANVAS_WIDTH,
} from "../services/floorConstants";
import { DeviceControlPanel } from "../components/panels/DeviceControlPanel";
import { FloorInfoModal } from "../components/panels/FloorInfoModal";
import { RoomFormModal } from "../components/panels/RoomFormModal";
import { FloorTabs } from "../components/navigation/FloorTabs";
import { FloorSetupPrompt } from "../components/setup/FloorSetupPrompt";
import { FloorWorkspace } from "../components/workspace/FloorWorkspace";
import { useFloor } from "../hooks/useFloor";
import { useFloorDetailsActions } from "../hooks/useFloorDetailsActions";
import { useFloorDevices } from "../hooks/useFloorDevices";
import { useFloorEditor } from "../hooks/useFloorEditor";
import { useFloorLocalEvents } from "../hooks/useFloorLocalEvents";
import { useFloorPlacementActions } from "../hooks/useFloorPlacementActions";
import { useFloorPlanViewModel } from "../hooks/useFloorPlanViewModel";
import { useFloorRealtime } from "../hooks/useFloorRealtime";
import { useFloorRoomActions } from "../hooks/useFloorRoomActions";
import { useFloors } from "../hooks/useFloors";
import pageStyles from "./FloorPage.module.css";

export function FloorPage() {
  const { homeId, floorId } = useParams();
  const navigate = useNavigate();
  const { t } = useTranslation("floors");
  const editor = useFloorEditor();
  const { consumeExpectedLocalUpdate, markExpectedLocalUpdate } =
    useFloorLocalEvents();

  const {
    home,
    isLoading: isHomeLoading,
    error: homeError,
    reload: reloadHome,
  } = useHomeDetail(homeId ?? null);
  const {
    floors,
    isLoading: isFloorsLoading,
    error: floorsError,
    reload: reloadFloors,
    setFloors,
  } = useFloors(homeId ?? null);
  const {
    floor,
    isLoading: isFloorLoading,
    error: floorError,
    reload: reloadFloor,
    setFloor,
  } = useFloor(homeId ?? null, floorId ?? null);
  const {
    devices,
    devicesById,
    error: devicesError,
    reload: reloadDevices,
    applyDeviceRealtimeDelta,
  } = useFloorDevices(homeId ?? null);

  const selectedFloorSummary = useMemo(
    () =>
      floorId
        ? floors.find((floorSummary) => floorSummary.id === floorId) ?? null
        : floors[0] ?? null,
    [floorId, floors]
  );
  const currentFloor =
    floor && (!floorId || floor.id === floorId) ? floor : null;

  const {
    availableRooms,
    canvasDevices,
    canvasRooms,
    deviceGroups: unplacedDeviceGroups,
    roomNamesById,
    selectedCanvasDevice,
    selectedPlacement,
    selectedRoom,
    unplacedDevices: unplacedFloorDevices,
  } = useFloorPlanViewModel({
    floor: currentFloor,
    floors,
    rooms: home?.rooms ?? [],
    devices,
    devicesById,
    selectedRoomId: editor.selectedRoomId,
    selectedPlacementId: editor.selectedPlacementId,
  });

  useEffect(() => {
    if (!homeId || isFloorsLoading) {
      return;
    }

    if (!floorId && floors.length > 0) {
      navigate(`/homes/${homeId}/floors/${floors[0].id}`, { replace: true });
      return;
    }

    if (
      floorId &&
      floors.length > 0 &&
      !floors.some((floorSummary) => floorSummary.id === floorId)
    ) {
      navigate(`/homes/${homeId}/floors/${floors[0].id}`, { replace: true });
    }
  }, [floorId, floors, homeId, isFloorsLoading, navigate]);

  const pendingPlacementDeviceId =
    editor.mode === "place-device" &&
    unplacedFloorDevices.some((device) => device.id === editor.pendingPlacementDeviceId)
      ? editor.pendingPlacementDeviceId
      : null;

  const {
    closeRoomModal,
    cancelDrawingRoom: handleCancelDrawingRoom,
    createRoom: handleCreateRoom,
    deleteRoom: handleDeleteRoom,
    editRoom: handleEditRoom,
    finishDrawingRoom: handleFinishDrawingRoom,
    isDeletingRoom,
    isRoomModalOpen,
    isSavingRoom,
    roomDraft,
    roomError,
    roomModalMode,
    saveRoom: handleSaveRoom,
    setRoomDraft,
  } = useFloorRoomActions({
    homeId: homeId ?? null,
    floor: currentFloor,
    availableRooms,
    canvasRooms,
    editor,
    markExpectedLocalUpdate,
    reloadFloor,
    reloadFloors,
  });
  const {
    closeControlPanel,
    controlPanelDeviceId,
    handleDeviceClick,
    handleRoomClick,
    isRemovingPlacement,
    movePlacement: handleMovePlacement,
    placeDevice: handlePlaceDevice,
    removePlacement: handleRemovePlacement,
    selectDeviceForPlacement: handleSelectDeviceForPlacement,
  } = useFloorPlacementActions({
    homeId: homeId ?? null,
    floor: currentFloor,
    editor,
    markExpectedLocalUpdate,
    reloadFloor,
    reloadFloors,
    reloadDevices,
  });
  const {
    closeCreateFloorModal,
    closeInfoModal,
    createFloor: handleCreateFloor,
    deleteFloor: handleDeleteFloor,
    openCreateFloorModal: handleOpenCreateFloorModal,
    openInfoModal: handleOpenInfoModal,
    reorderFloor: handleFloorDrop,
    saveFloorInfo: handleSaveFloorInfo,
    infoCanvasHeight,
    infoCanvasWidth,
    infoError,
    infoName,
    isCreateFloorModalOpen,
    isCreatingFloor,
    isDeletingFloor,
    isInfoModalOpen,
    isReorderingFloors,
    isSavingInfo,
    setDraggedFloorId,
    setInfoCanvasHeight,
    setInfoCanvasWidth,
    setInfoName,
    setSetupCanvasHeight,
    setSetupCanvasWidth,
    setSetupName,
    setupCanvasHeight,
    setupCanvasWidth,
    setupError,
    setupName,
  } = useFloorDetailsActions({
    homeId: homeId ?? null,
    home,
    floor: currentFloor,
    floors,
    editor,
    setFloor,
    setFloors,
    closeControlPanel,
    markExpectedLocalUpdate,
    reloadFloor,
    reloadFloors,
  });

  const editingSessionActive =
    editor.mode !== "view" || isRoomModalOpen || isInfoModalOpen;
  const { pendingExternalUpdate, reloadAll, setPendingExternalUpdate } =
    useFloorRealtime({
      homeId: homeId ?? null,
      floorId: floorId ?? null,
      editingSessionActive,
      controlPanelDeviceId,
      reloadHome,
      reloadFloors,
      reloadFloor,
      reloadDevices,
      applyDeviceRealtimeDelta,
      closeControlPanel,
      consumeExpectedLocalUpdate,
    });

  const defaultSetupName = home
    ? t("setup.defaultName", {
      homeName: home.name,
      number: floors.length + 1,
    })
    : "";
  const displayedSetupName = setupName || defaultSetupName;

  const handleReloadExternalChanges = useCallback(async () => {
    setPendingExternalUpdate(null);
    closeRoomModal();
    closeInfoModal();
    closeControlPanel();
    editor.enterViewMode();
    await reloadAll(true);
  }, [
    closeControlPanel,
    closeInfoModal,
    closeRoomModal,
    editor,
    reloadAll,
    setPendingExternalUpdate,
  ]);

  const isLoading = isHomeLoading || isFloorsLoading;
  const isFloorContentLoading = Boolean(floorId) && isFloorLoading && !currentFloor;
  const shouldShowFloorPlaceholder = !currentFloor && floors.length > 0;
  const pageTitle =
    currentFloor?.name ?? selectedFloorSummary?.name ?? t("pageTitleFallback");
  const isHomeNotFound =
    homeError instanceof ApiError && homeError.status === 404;

  if (!homeId) {
    return <div className={sharedStyles.emptyState}>{t("notFound")}</div>;
  }

  if (isLoading) {
    return (
      <div className={pageStyles.loadingState}>
        <Spinner />
      </div>
    );
  }

  if (homeError) {
    return (
      <div className={sharedStyles.emptyState}>
        {isHomeNotFound ? t("notFound") : t("failed")}
      </div>
    );
  }

  if (!home) {
    return <div className={sharedStyles.emptyState}>{t("notFound")}</div>;
  }

  if (floorsError && floors.length === 0) {
    return <div className={sharedStyles.emptyState}>{t("failed")}</div>;
  }

  if (floorId && floorError && !currentFloor) {
    return <div className={sharedStyles.emptyState}>{t("failed")}</div>;
  }

  return (
    <div className={sharedStyles.pageStack}>
      <PageHeader
        title={
          <span className={pageStyles.pageTitle}>
            <span>{pageTitle}</span>
            <span className={pageStyles.pageSubtitle}>{home.name}</span>
          </span>
        }
        action={
          <div className={pageStyles.headerActions}>
            <Button
              variant="secondary"
              size="sm"
              onClick={() => navigate(`/homes/${home.id}`)}
            >
              {t("actions.backToHome")}
            </Button>
            <Button
              size="sm"
              onClick={handleOpenCreateFloorModal}
            >
              {t("actions.createFloor")}
            </Button>
            {currentFloor ? (
              <>
                <Button
                  variant="secondary"
                  size="sm"
                  onClick={handleOpenInfoModal}
                >
                  {t("actions.editInfo")}
                </Button>
                <Button
                  variant="danger"
                  size="sm"
                  disabled={isDeletingFloor}
                  onClick={() => void handleDeleteFloor()}
                >
                  {isDeletingFloor
                    ? t("actions.deletingFloor")
                    : t("actions.deleteFloor")}
                </Button>
              </>
            ) : null}
          </div>
        }
      />

      {pendingExternalUpdate ? (
        <section className={pageStyles.noticeBar}>
          <div className={pageStyles.noticeCopy}>
            <strong>{t("events.externalUpdateTitle")}</strong>
            <span>
              {t(`events.reasons.${pendingExternalUpdate}`, {
                defaultValue: t("events.reasons.Unknown"),
              })}
            </span>
          </div>
          <div className={pageStyles.noticeActions}>
            <Button size="sm" onClick={() => void handleReloadExternalChanges()}>
              {t("events.reloadNow")}
            </Button>
            <Button
              size="sm"
              variant="secondary"
              onClick={() => setPendingExternalUpdate(null)}
            >
              {t("events.keepEditing")}
            </Button>
          </div>
        </section>
      ) : null}

      <FloorTabs
        floors={floors}
        activeFloorId={floorId ?? currentFloor?.id ?? null}
        isReordering={isReorderingFloors}
        onSelect={(selectedFloorId) =>
          navigate(`/homes/${home.id}/floors/${selectedFloorId}`)
        }
        onDragStart={setDraggedFloorId}
        onDragEnd={() => setDraggedFloorId(null)}
        onDrop={(targetFloorId) => void handleFloorDrop(targetFloorId)}
      />

      {isFloorContentLoading || shouldShowFloorPlaceholder ? (
        <section className={pageStyles.floorContentLoading}>
          <Spinner />
          <span>{t("loading")}</span>
        </section>
      ) : !currentFloor ? (
        <FloorSetupPrompt
          homeName={home.name}
          name={displayedSetupName}
          canvasWidth={setupCanvasWidth}
          canvasHeight={setupCanvasHeight}
          isCreating={isCreatingFloor}
          error={setupError}
          title={t("setup.title")}
          saveLabel={t("setup.create")}
          savingLabel={t("setup.creating")}
          nameLabel={t("fields.name")}
          widthLabel={t("fields.canvasWidth")}
          heightLabel={t("fields.canvasHeight")}
          minCanvasWidth={MIN_CANVAS_WIDTH}
          minCanvasHeight={MIN_CANVAS_HEIGHT}
          onSubmit={handleCreateFloor}
          onNameChange={setSetupName}
          onCanvasWidthChange={setSetupCanvasWidth}
          onCanvasHeightChange={setSetupCanvasHeight}
        />
      ) : (
        <FloorWorkspace
          floor={currentFloor}
          rooms={canvasRooms}
          devices={canvasDevices}
          editor={editor}
          canDrawRoom={availableRooms.length > 0}
          unplacedDeviceGroups={unplacedDeviceGroups}
          unplacedDeviceCount={unplacedFloorDevices.length}
          pendingPlacementDeviceId={pendingPlacementDeviceId}
          selectedRoom={selectedRoom}
          selectedPlacement={selectedPlacement}
          selectedCanvasDevice={selectedCanvasDevice}
          isRemovingPlacement={isRemovingPlacement}
          devicesError={devicesError}
          onCreateRoom={handleCreateRoom}
          onFinishDrawingRoom={handleFinishDrawingRoom}
          onCancelDrawingRoom={handleCancelDrawingRoom}
          onSelectDeviceForPlacement={handleSelectDeviceForPlacement}
          onPlaceDevice={(deviceId, point) => void handlePlaceDevice(deviceId, point)}
          onRoomClick={handleRoomClick}
          onDeviceClick={handleDeviceClick}
          onDeviceDragEnd={(placementId, point) =>
            void handleMovePlacement(placementId, point)
          }
          onEditRoom={handleEditRoom}
          onOpenDeviceDetails={(deviceId) =>
            navigate(`/homes/${home.id}/devices/${deviceId}`)
          }
          onRemovePlacement={(placementId) => void handleRemovePlacement(placementId)}
        />
      )}

      <RoomFormModal
        open={isRoomModalOpen}
        title={
          roomModalMode === "create"
            ? t("roomForm.createTitle")
            : t("roomForm.editTitle")
        }
        isEditing={roomModalMode === "edit"}
        roomId={roomDraft.roomId}
        roomName={roomNamesById.get(roomDraft.roomId) ?? ""}
        fillColor={roomDraft.fillColor}
        polygonPointCount={roomDraft.polygon.length}
        availableRooms={availableRooms}
        isSaving={isSavingRoom}
        isDeleting={isDeletingRoom}
        error={roomError}
        deleteLabel={t("roomForm.delete")}
        deletingLabel={t("roomForm.deleting")}
        saveLabel={t("roomForm.save")}
        savingLabel={t("roomForm.saving")}
        cancelLabel={t("common.cancel")}
        roomLabel={t("roomForm.room")}
        selectRoomLabel={t("roomForm.selectRoom", { defaultValue: "Chọn phòng" })}
        fillColorLabel={t("roomForm.fillColor")}
        polygonLabel={t("roomForm.polygon")}
        polygonHint={t("roomForm.polygonHint")}
        onClose={closeRoomModal}
        onSubmit={handleSaveRoom}
        onDelete={roomModalMode === "edit" ? () => void handleDeleteRoom() : undefined}
        onRoomIdChange={(value) =>
          setRoomDraft((current) => ({ ...current, roomId: value }))
        }
        onFillColorChange={(value) =>
          setRoomDraft((current) => ({ ...current, fillColor: value }))
        }
      />

      <FloorInfoModal
        open={isCreateFloorModalOpen}
        title={t("setup.modalTitle")}
        name={displayedSetupName}
        canvasWidth={setupCanvasWidth}
        canvasHeight={setupCanvasHeight}
        isSaving={isCreatingFloor}
        error={setupError}
        saveLabel={t("setup.create")}
        savingLabel={t("setup.creating")}
        cancelLabel={t("common.cancel")}
        nameLabel={t("fields.name")}
        widthLabel={t("fields.canvasWidth")}
        heightLabel={t("fields.canvasHeight")}
        minCanvasWidth={MIN_CANVAS_WIDTH}
        minCanvasHeight={MIN_CANVAS_HEIGHT}
        helperText={t("setup.helper")}
        onClose={closeCreateFloorModal}
        onSubmit={handleCreateFloor}
        onNameChange={setSetupName}
        onCanvasWidthChange={setSetupCanvasWidth}
        onCanvasHeightChange={setSetupCanvasHeight}
      />

      <FloorInfoModal
        open={isInfoModalOpen}
        title={t("infoModal.title")}
        name={infoName}
        canvasWidth={infoCanvasWidth}
        canvasHeight={infoCanvasHeight}
        isSaving={isSavingInfo}
        error={infoError}
        saveLabel={t("infoModal.save")}
        savingLabel={t("infoModal.saving")}
        cancelLabel={t("common.cancel")}
        nameLabel={t("fields.name")}
        widthLabel={t("fields.canvasWidth")}
        heightLabel={t("fields.canvasHeight")}
        minCanvasWidth={MIN_CANVAS_WIDTH}
        minCanvasHeight={MIN_CANVAS_HEIGHT}
        helperText={t("infoModal.helper")}
        onClose={closeInfoModal}
        onSubmit={handleSaveFloorInfo}
        onNameChange={setInfoName}
        onCanvasWidthChange={setInfoCanvasWidth}
        onCanvasHeightChange={setInfoCanvasHeight}
      />

      {controlPanelDeviceId ? (
        <DeviceControlPanel
          homeId={home.id}
          deviceId={controlPanelDeviceId}
          onClose={closeControlPanel}
          openDetailsLabel={t("device.openDetails")}
          notFoundLabel={t("device.notFound")}
        />
      ) : null}
    </div>
  );
}
