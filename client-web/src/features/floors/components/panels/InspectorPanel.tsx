import { Button } from "@/shared/ui/Button";
import type {
  CanvasDevice,
  CanvasRoom,
  EditorMode,
  Floor,
  FloorDevicePlacement,
} from "../../types/floorTypes";
import styles from "./InspectorPanel.module.css";

type Props = {
  mode: EditorMode;
  floor: Floor;
  selectedRoom: CanvasRoom | null;
  selectedPlacement: FloorDevicePlacement | null;
  selectedCanvasDevice: CanvasDevice | null;
  unplacedDeviceCount: number;
  isRemovingPlacement: boolean;
  onEditRoom: (roomId: string) => void;
  onOpenDeviceDetails: (deviceId: string) => void;
  onRemovePlacement: (placementId: string) => void;
  drawModeTitle: string;
  drawModeDescription: string;
  emptyTitle: string;
  emptyDescription: string;
  deviceOpenDetailsLabel: string;
  deviceRemoveLabel: string;
  deviceRemovingLabel: string;
  deviceRoomLabel: string;
  roomFallbackLabel: string;
  deviceStatusLabel: string;
  deviceOnlineLabel: string;
  deviceOfflineLabel: string;
  statsTitle: string;
  statsRoomsLabel: string;
  statsPlacementsLabel: string;
  statsUnplacedLabel: string;
  roomEditLabel: string;
  roomPolygonHint: string;
};

export function InspectorPanel(props: Props) {
  const {
    mode,
    floor,
    selectedRoom,
    selectedPlacement,
    selectedCanvasDevice,
    unplacedDeviceCount,
    isRemovingPlacement,
    onEditRoom,
    onOpenDeviceDetails,
    onRemovePlacement,
  } = props;
  const device = selectedCanvasDevice?.deviceSnapshot;

  return (
    <section className={styles.panel}>
      <div className={styles.header}>
        <h2 className={styles.title}>
          {mode === "draw-room"
            ? props.drawModeTitle
            : selectedRoom?.name ?? selectedCanvasDevice?.displayName ?? props.emptyTitle}
        </h2>
        <p className={styles.description}>
          {mode === "draw-room"
            ? props.drawModeDescription
            : selectedRoom?.name ?? device?.roomName ?? props.emptyDescription}
        </p>
      </div>

      {selectedRoom ? (
        <>
          <dl className={styles.definitionList}>
            <div className={styles.definitionRow}>
              <dt>{props.roomPolygonHint}</dt>
              <dd>{selectedRoom.polygon.length}</dd>
            </div>
          </dl>
          <Button size="sm" onClick={() => onEditRoom(selectedRoom.id)}>
            {props.roomEditLabel}
          </Button>
        </>
      ) : null}

      {selectedPlacement && selectedCanvasDevice && device ? (
        <>
          <dl className={styles.definitionList}>
            <div className={styles.definitionRow}>
              <dt>{props.deviceStatusLabel}</dt>
              <dd className={device.isOnline ? styles.statusOnline : styles.statusOffline}>
                {device.isOnline ? props.deviceOnlineLabel : props.deviceOfflineLabel}
              </dd>
            </div>
            <div className={styles.definitionRow}>
              <dt>{props.deviceRoomLabel}</dt>
              <dd>{device.roomName ?? props.roomFallbackLabel}</dd>
            </div>
          </dl>
          <div className={styles.actions}>
            <Button
              size="sm"
              variant="secondary"
              onClick={() => onOpenDeviceDetails(selectedPlacement.deviceId)}
            >
              {props.deviceOpenDetailsLabel}
            </Button>
            <Button
              size="sm"
              variant="danger"
              disabled={isRemovingPlacement}
              onClick={() => onRemovePlacement(selectedPlacement.id)}
            >
              {isRemovingPlacement ? props.deviceRemovingLabel : props.deviceRemoveLabel}
            </Button>
          </div>
        </>
      ) : null}

      {!selectedRoom && !selectedPlacement ? (
        <>
          <h3 className={styles.statsTitle}>{props.statsTitle}</h3>
          <dl className={styles.definitionList}>
            <div className={styles.definitionRow}>
              <dt>{props.statsRoomsLabel}</dt>
              <dd>{floor.floorPlanRooms.length}</dd>
            </div>
            <div className={styles.definitionRow}>
              <dt>{props.statsPlacementsLabel}</dt>
              <dd>{floor.devicePlacements.length}</dd>
            </div>
            <div className={styles.definitionRow}>
              <dt>{props.statsUnplacedLabel}</dt>
              <dd>{unplacedDeviceCount}</dd>
            </div>
          </dl>
        </>
      ) : null}
    </section>
  );
}
