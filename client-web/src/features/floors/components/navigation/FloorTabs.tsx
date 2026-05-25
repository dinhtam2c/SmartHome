import { useTranslation } from "react-i18next";
import type { FloorSummary } from "../../types/floorTypes";
import styles from "./FloorTabs.module.css";

type Props = {
  floors: FloorSummary[];
  activeFloorId: string | null;
  isReordering: boolean;
  onSelect: (floorId: string) => void;
  onDragStart: (floorId: string) => void;
  onDragEnd: () => void;
  onDrop: (floorId: string) => void;
};

export function FloorTabs({
  floors,
  activeFloorId,
  isReordering,
  onSelect,
  onDragStart,
  onDragEnd,
  onDrop,
}: Props) {
  const { t } = useTranslation("floors");

  if (floors.length === 0) {
    return null;
  }

  return (
    <nav className={styles.floorTabs} aria-label={t("floorTabs.label")}>
      {floors.map((floor) => {
        const isActive = floor.id === activeFloorId;

        return (
          <button
            key={floor.id}
            type="button"
            className={`${styles.floorTab} ${isActive ? styles.floorTabActive : ""}`}
            draggable={!isReordering}
            disabled={isReordering}
            onClick={() => onSelect(floor.id)}
            onDragStart={() => onDragStart(floor.id)}
            onDragEnd={onDragEnd}
            onDragOver={(event) => event.preventDefault()}
            onDrop={() => onDrop(floor.id)}
          >
            <span className={styles.floorTabGrip} aria-hidden="true">::</span>
            <span className={styles.floorTabName}>{floor.name}</span>
            <span className={styles.floorTabMeta}>
              {t("floorTabs.meta", {
                rooms: floor.floorPlanRoomCount,
                devices: floor.devicePlacementCount,
              })}
            </span>
          </button>
        );
      })}
    </nav>
  );
}
