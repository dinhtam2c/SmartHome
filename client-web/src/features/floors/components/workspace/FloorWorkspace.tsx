import { useTranslation } from "react-i18next";
import { CanvasToolbar } from "../canvas/CanvasToolbar";
import { FloorCanvas } from "../canvas/FloorCanvas";
import { InspectorPanel } from "../panels/InspectorPanel";
import {
  UnplacedFloorDevicesPanel,
  type FloorDeviceGroup,
} from "../panels/UnplacedFloorDevicesPanel";
import type { useFloorEditor } from "../../hooks/useFloorEditor";
import type {
  CanvasDevice,
  CanvasRoom,
  Floor,
  FloorDevicePlacement,
  Point,
} from "../../types/floorTypes";
import styles from "./FloorWorkspace.module.css";

type EditorState = ReturnType<typeof useFloorEditor>;

type Props = {
  floor: Floor;
  rooms: CanvasRoom[];
  devices: CanvasDevice[];
  editor: EditorState;
  canDrawRoom: boolean;
  unplacedDeviceGroups: FloorDeviceGroup[];
  unplacedDeviceCount: number;
  pendingPlacementDeviceId: string | null;
  selectedRoom: CanvasRoom | null;
  selectedPlacement: FloorDevicePlacement | null;
  selectedCanvasDevice: CanvasDevice | null;
  isRemovingPlacement: boolean;
  devicesError: Error | null;
  onCreateRoom: (polygon: Point[]) => void;
  onFinishDrawingRoom: () => void;
  onCancelDrawingRoom: () => void;
  onSelectDeviceForPlacement: (deviceId: string) => void;
  onPlaceDevice: (deviceId: string, point: Point) => void;
  onRoomClick: (roomId: string) => void;
  onDeviceClick: (placementId: string) => void;
  onDeviceDragEnd: (placementId: string, point: Point) => void;
  onEditRoom: (roomId: string) => void;
  onOpenDeviceDetails: (deviceId: string) => void;
  onRemovePlacement: (placementId: string) => void;
};

export function FloorWorkspace({
  floor,
  rooms,
  devices,
  editor,
  canDrawRoom,
  unplacedDeviceGroups,
  unplacedDeviceCount,
  pendingPlacementDeviceId,
  selectedRoom,
  selectedPlacement,
  selectedCanvasDevice,
  isRemovingPlacement,
  devicesError,
  onCreateRoom,
  onFinishDrawingRoom,
  onCancelDrawingRoom,
  onSelectDeviceForPlacement,
  onPlaceDevice,
  onRoomClick,
  onDeviceClick,
  onDeviceDragEnd,
  onEditRoom,
  onOpenDeviceDetails,
  onRemovePlacement,
}: Props) {
  const { t } = useTranslation("floors");

  return (
    <>
      <section className={styles.floorWorkbar}>
        <CanvasToolbar
          mode={editor.mode}
          drawingPointCount={editor.drawing.points.length}
          canDrawRoom={canDrawRoom}
          onEnterViewMode={editor.enterViewMode}
          onEnterPlaceDeviceMode={editor.enterPlaceDeviceMode}
          onStartDrawingRoom={editor.startDrawingRoom}
          onUndoDrawingPoint={editor.removeLastDrawingPoint}
          onCancelDrawingRoom={onCancelDrawingRoom}
          viewLabel={t("toolbar.view")}
          editLabel={t("toolbar.edit")}
          drawRoomLabel={t("toolbar.drawRoom")}
          undoPointLabel={t("toolbar.undoPoint")}
          cancelDrawingLabel={t("toolbar.cancelDrawing")}
          drawingPointCountLabel={t("toolbar.pointCount", {
            count: editor.drawing.points.length,
          })}
          drawRoomHint={t("canvas.drawHint")}
          idleHint={t("toolbar.idleHint")}
        />

        <div className={styles.planMeta}>
          <span className={styles.metaChip}>
            {t("stats.rooms", { count: floor.floorPlanRooms.length })}
          </span>
          <span className={styles.metaChip}>
            {t("stats.devicePlacements", { count: floor.devicePlacements.length })}
          </span>
          <span className={styles.metaChip}>
            {t("stats.unplacedDevices", { count: unplacedDeviceCount })}
          </span>
        </div>
      </section>

      <div
        className={`${styles.boardLayout} ${editor.isEditMode ? styles.boardLayoutWithSidebar : ""}`}
      >
        <section className={styles.canvasColumn}>
          <FloorCanvas
            floor={floor}
            rooms={rooms}
            devices={devices}
            editor={editor}
            pendingPlacementDeviceId={pendingPlacementDeviceId}
            onBlankClick={editor.clearSelection}
            onCreateRoom={onCreateRoom}
            onFinishDrawingRoom={onFinishDrawingRoom}
            onCancelDrawingRoom={onCancelDrawingRoom}
            onUndoDrawingPoint={editor.removeLastDrawingPoint}
            onPlaceDevice={onPlaceDevice}
            onRoomClick={onRoomClick}
            onDeviceClick={onDeviceClick}
            onDeviceDragEnd={onDeviceDragEnd}
          />
        </section>

        {editor.isEditMode ? (
          <aside className={styles.sideColumn}>
            {editor.mode === "place-device" ? (
              <UnplacedFloorDevicesPanel
                groups={unplacedDeviceGroups}
                selectedDeviceId={pendingPlacementDeviceId}
                title={t("panels.unplacedTitle")}
                helperText={t("panels.unplacedHelper")}
                emptyText={t("panels.unplacedEmpty")}
                roomFallbackLabel={t("panels.unassignedRoom")}
                onSelectDevice={onSelectDeviceForPlacement}
              />
            ) : null}

            <InspectorPanel
              mode={editor.mode}
              floor={floor}
              selectedRoom={selectedRoom}
              selectedPlacement={selectedPlacement}
              selectedCanvasDevice={selectedCanvasDevice}
              unplacedDeviceCount={unplacedDeviceCount}
              isRemovingPlacement={isRemovingPlacement}
              onEditRoom={onEditRoom}
              onOpenDeviceDetails={onOpenDeviceDetails}
              onRemovePlacement={onRemovePlacement}
              drawModeTitle={t("panels.drawTitle")}
              drawModeDescription={t("panels.drawDescription")}
              emptyTitle={t("panels.selectionTitle")}
              emptyDescription={t("panels.selectionDescription")}
              deviceOpenDetailsLabel={t("panels.openDeviceDetails")}
              deviceRemoveLabel={t("panels.removePlacement")}
              deviceRemovingLabel={t("panels.removingPlacement")}
              deviceRoomLabel={t("panels.deviceRoom")}
              roomFallbackLabel={t("panels.unassignedRoom")}
              deviceStatusLabel={t("panels.deviceStatus")}
              deviceOnlineLabel={t("device.online")}
              deviceOfflineLabel={t("device.offline")}
              statsTitle={t("panels.statsTitle")}
              statsRoomsLabel={t("panels.statsRooms")}
              statsPlacementsLabel={t("panels.statsDevicePlacements")}
              statsUnplacedLabel={t("panels.statsUnplacedDevices")}
              roomEditLabel={t("panels.editRoom")}
              roomPolygonHint={t("panels.roomPoints")}
            />

            {devicesError ? (
              <div className={styles.inlineError}>
                {devicesError.message || t("errors.loadDevicesFailed")}
              </div>
            ) : null}
          </aside>
        ) : null}
      </div>
    </>
  );
}
