import { Button } from "@/shared/ui/Button";
import type { EditorMode } from "../../types/floorTypes";
import styles from "./CanvasToolbar.module.css";

type Props = {
  mode: EditorMode;
  drawingPointCount: number;
  canDrawRoom: boolean;
  onEnterViewMode: () => void;
  onEnterPlaceDeviceMode: () => void;
  onStartDrawingRoom: () => void;
  onUndoDrawingPoint: () => void;
  onCancelDrawingRoom: () => void;
  viewLabel: string;
  editLabel: string;
  drawRoomLabel: string;
  undoPointLabel: string;
  cancelDrawingLabel: string;
  drawingPointCountLabel: string;
  drawRoomHint: string;
  idleHint: string;
};

export function CanvasToolbar({
  mode,
  drawingPointCount,
  canDrawRoom,
  onEnterViewMode,
  onEnterPlaceDeviceMode,
  onStartDrawingRoom,
  onUndoDrawingPoint,
  onCancelDrawingRoom,
  viewLabel,
  editLabel,
  drawRoomLabel,
  undoPointLabel,
  cancelDrawingLabel,
  drawingPointCountLabel,
  drawRoomHint,
  idleHint,
}: Props) {
  const canUndoDrawingPoint = mode === "draw-room" && drawingPointCount > 0;

  return (
    <section className={styles.toolbar}>
      <div className={styles.actions}>
        <Button
          variant={mode === "view" ? "primary" : "secondary"}
          size="sm"
          onClick={onEnterViewMode}
        >
          {viewLabel}
        </Button>
        <Button
          variant={mode === "place-device" ? "primary" : "secondary"}
          size="sm"
          onClick={onEnterPlaceDeviceMode}
        >
          {editLabel}
        </Button>
        <Button
          variant={mode === "draw-room" ? "primary" : "secondary"}
          size="sm"
          disabled={!canDrawRoom}
          onClick={onStartDrawingRoom}
        >
          {drawRoomLabel}
        </Button>
      </div>

      <div className={styles.status}>
        <div className={styles.hint}>
          {mode === "draw-room" ? drawRoomHint : idleHint}
        </div>

        {mode === "draw-room" ? (
          <div className={styles.drawingActions}>
            <span className={styles.pointCount}>{drawingPointCountLabel}</span>
            <Button
              variant="secondary"
              size="sm"
              disabled={!canUndoDrawingPoint}
              onClick={onUndoDrawingPoint}
            >
              {undoPointLabel}
            </Button>
            <Button
              variant="secondary"
              size="sm"
              onClick={onCancelDrawingRoom}
            >
              {cancelDrawingLabel}
            </Button>
          </div>
        ) : null}
      </div>
    </section>
  );
}
