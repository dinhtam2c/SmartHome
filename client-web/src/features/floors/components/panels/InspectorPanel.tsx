import { Button } from "@/shared/ui/Button";
import type {
  CanvasDevice,
  EditorMode,
  Floor,
  FloorRoom,
  PlacedFloorDevice,
} from "../../types/floorTypes";
import styles from "./InspectorPanel.module.css";

type Props = {
  mode: EditorMode;
  floor: Floor;
  selectedRoom: FloorRoom | null;
  selectedPlacedFloorDevice: PlacedFloorDevice | null;
  selectedCanvasDevice: CanvasDevice | null;
  unplacedFloorDeviceCount: number;
  isRemovingPlacedFloorDevice: boolean;
  onEditRoom: (roomId: string) => void;
  onOpenDeviceDetails: (deviceId: string) => void;
  onRemovePlacedFloorDevice: (placedFloorDeviceId: string) => void;
  drawModeTitle: string;
  drawModeDescription: string;
  emptyTitle: string;
  emptyDescription: string;
  roomLinkedLabel: string;
  roomNoLinkedLabel: string;
  deviceDeletedLabel: string;
  deviceOpenDetailsLabel: string;
  deviceRemoveLabel: string;
  deviceRemovingLabel: string;
  deviceRoomLabel: string;
  deviceHomeRoomLabel: string;
  deviceStatusLabel: string;
  deviceOnlineLabel: string;
  deviceOfflineLabel: string;
  statsTitle: string;
  statsRoomsLabel: string;
  statsPlacedFloorDevicesLabel: string;
  statsUnplacedLabel: string;
  roomEditLabel: string;
  roomPolygonHint: string;
};

export function InspectorPanel({
  mode,
  floor,
  selectedRoom,
  selectedPlacedFloorDevice,
  selectedCanvasDevice,
  unplacedFloorDeviceCount,
  isRemovingPlacedFloorDevice,
  onEditRoom,
  onOpenDeviceDetails,
  onRemovePlacedFloorDevice,
  drawModeTitle,
  drawModeDescription,
  emptyTitle,
  emptyDescription,
  roomLinkedLabel,
  roomNoLinkedLabel,
  deviceDeletedLabel,
  deviceOpenDetailsLabel,
  deviceRemoveLabel,
  deviceRemovingLabel,
  deviceRoomLabel,
  deviceHomeRoomLabel,
  deviceStatusLabel,
  deviceOnlineLabel,
  deviceOfflineLabel,
  statsTitle,
  statsRoomsLabel,
  statsPlacedFloorDevicesLabel,
  statsUnplacedLabel,
  roomEditLabel,
  roomPolygonHint,
}: Props) {
  const selectedDeviceRoom = selectedPlacedFloorDevice?.floorRoomId
    ? floor.rooms.find((room) => room.id === selectedPlacedFloorDevice.floorRoomId)
    : null;
  const selectedDeviceStatus = selectedCanvasDevice?.isDeleted
    ? deviceDeletedLabel
    : selectedCanvasDevice?.isOnline
      ? deviceOnlineLabel
      : deviceOfflineLabel;

  return (
    <section className={styles.panel}>
      <div className={styles.header}>
        <h2 className={styles.title}>
          {mode === "draw-room"
            ? drawModeTitle
            : selectedRoom
              ? selectedRoom.label
              : selectedCanvasDevice
                ? selectedCanvasDevice.displayName
                : emptyTitle}
        </h2>
        <p className={styles.description}>
          {mode === "draw-room"
            ? drawModeDescription
            : selectedRoom
              ? `${roomLinkedLabel}: ${selectedRoom.linkedRoomName ?? roomNoLinkedLabel}`
              : selectedCanvasDevice
                ? selectedCanvasDevice.isDeleted
                  ? deviceDeletedLabel
                  : selectedCanvasDevice.deviceSnapshot?.roomName ?? roomNoLinkedLabel
                : emptyDescription}
        </p>
      </div>

      {selectedRoom ? (
        <>
          <dl className={styles.definitionList}>
            <div className={styles.definitionRow}>
              <dt>{roomLinkedLabel}</dt>
              <dd>{selectedRoom.linkedRoomName ?? roomNoLinkedLabel}</dd>
            </div>
            <div className={styles.definitionRow}>
              <dt>{roomPolygonHint}</dt>
              <dd>{selectedRoom.polygon.length}</dd>
            </div>
          </dl>
          <Button size="sm" onClick={() => onEditRoom(selectedRoom.id)}>
            {roomEditLabel}
          </Button>
        </>
      ) : null}

      {selectedPlacedFloorDevice && selectedCanvasDevice ? (
        <>
          <dl className={styles.definitionList}>
            <div className={styles.definitionRow}>
              <dt>{deviceStatusLabel}</dt>
              <dd
                className={
                  selectedCanvasDevice.isOnline && !selectedCanvasDevice.isDeleted
                    ? styles.statusOnline
                    : styles.statusOffline
                }
              >
                {selectedDeviceStatus}
              </dd>
            </div>
            <div className={styles.definitionRow}>
              <dt>{deviceHomeRoomLabel}</dt>
              <dd>
                {selectedCanvasDevice.deviceSnapshot?.roomName ?? roomNoLinkedLabel}
              </dd>
            </div>
            <div className={styles.definitionRow}>
              <dt>{deviceRoomLabel}</dt>
              <dd>{selectedDeviceRoom?.label ?? roomNoLinkedLabel}</dd>
            </div>
          </dl>

          <div className={styles.actions}>
            <Button
              size="sm"
              variant="secondary"
              disabled={selectedCanvasDevice.isDeleted}
              onClick={() => onOpenDeviceDetails(selectedPlacedFloorDevice.deviceId)}
            >
              {deviceOpenDetailsLabel}
            </Button>
            <Button
              size="sm"
              variant="danger"
              disabled={isRemovingPlacedFloorDevice}
              onClick={() => onRemovePlacedFloorDevice(selectedPlacedFloorDevice.id)}
            >
              {isRemovingPlacedFloorDevice ? deviceRemovingLabel : deviceRemoveLabel}
            </Button>
          </div>
        </>
      ) : null}

      {!selectedRoom && !selectedPlacedFloorDevice ? (
        <>
          <h3 className={styles.statsTitle}>{statsTitle}</h3>
          <dl className={styles.definitionList}>
            <div className={styles.definitionRow}>
              <dt>{statsRoomsLabel}</dt>
              <dd>{floor.rooms.length}</dd>
            </div>
            <div className={styles.definitionRow}>
              <dt>{statsPlacedFloorDevicesLabel}</dt>
              <dd>{floor.placedFloorDevices.length}</dd>
            </div>
            <div className={styles.definitionRow}>
              <dt>{statsUnplacedLabel}</dt>
              <dd>{unplacedFloorDeviceCount}</dd>
            </div>
          </dl>
        </>
      ) : null}
    </section>
  );
}
