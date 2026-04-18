import { useCallback, useEffect, useRef } from "react";
import { Button } from "@/components/Button";
import { Cell } from "@/components/Cell";
import { CellGrid } from "@/components/CellGrid";
import { LONG_PRESS_DURATION_MS } from "@/features/shared/interactionConstants";
import type { HomeDetailDto } from "../homes.types";
import styles from "@/features/shared/featurePage.module.css";
import { useTranslation } from "react-i18next";

type Props = {
  home: HomeDetailDto;
  onOpenRoom: (roomId: string) => void;
  onOpenRoomEdit?: (roomId: string) => void;
  onAddRoom?: () => void;
};

export function HomeRoomsSection({
  home,
  onOpenRoom,
  onOpenRoomEdit,
  onAddRoom,
}: Props) {
  const { t } = useTranslation("homes");
  const longPressTimerRef = useRef<number | null>(null);
  const longPressTriggeredRef = useRef(false);

  const clearLongPressTimer = useCallback(() => {
    if (longPressTimerRef.current !== null) {
      window.clearTimeout(longPressTimerRef.current);
      longPressTimerRef.current = null;
    }
  }, []);

  useEffect(() => () => clearLongPressTimer(), [clearLongPressTimer]);

  const handlePointerDown = useCallback(
    (roomId: string) => {
      if (!onOpenRoomEdit) {
        return;
      }

      clearLongPressTimer();
      longPressTriggeredRef.current = false;

      longPressTimerRef.current = window.setTimeout(() => {
        longPressTriggeredRef.current = true;
        onOpenRoomEdit(roomId);
      }, LONG_PRESS_DURATION_MS);
    },
    [clearLongPressTimer, onOpenRoomEdit]
  );

  const handlePointerEnd = useCallback(() => {
    clearLongPressTimer();
  }, [clearLongPressTimer]);

  const handleCellClick = useCallback(
    (roomId: string) => {
      if (longPressTriggeredRef.current) {
        longPressTriggeredRef.current = false;
        return;
      }

      onOpenRoom(roomId);
    },
    [onOpenRoom]
  );

  return (
    <section className={styles.section}>
      <div className={styles.sectionHeader}>
        <h2 className={styles.sectionTitle}>{t("detail.rooms")}</h2>
        {onAddRoom ? (
          <Button size="sm" onClick={onAddRoom}>
            {t("detail.addRoom")}
          </Button>
        ) : null}
      </div>

      {home.rooms.length === 0 ? (
        <div className={styles.emptyState}>{t("detail.noRooms")}</div>
      ) : (
        <CellGrid>
          {home.rooms.map((room) => {
            const hasTemperature =
              room.temperature !== undefined && room.temperature !== null;
            const hasHumidity =
              room.humidity !== undefined && room.humidity !== null;

            return (
              <Cell
                key={room.id}
                id={room.id}
                title={room.name}
                subtitle={
                  <>
                    <div>{room.description ?? t("noDescription")}</div>
                    <div>
                      {room.onlineDeviceCount} / {room.deviceCount} {t("detail.onlineDevices")}
                    </div>
                    {(hasTemperature || hasHumidity) && (
                      <div>
                        {hasTemperature &&
                          `${t("detail.temperature")}: ${room.temperature}°C`}
                        {hasTemperature && hasHumidity && " · "}
                        {hasHumidity && `${t("detail.humidity")}: ${room.humidity}%`}
                      </div>
                    )}
                  </>
                }
                onClick={handleCellClick}
                onPointerDown={onOpenRoomEdit ? handlePointerDown : undefined}
                onPointerUp={onOpenRoomEdit ? handlePointerEnd : undefined}
                onPointerCancel={onOpenRoomEdit ? handlePointerEnd : undefined}
                onPointerLeave={onOpenRoomEdit ? handlePointerEnd : undefined}
                disabled={false}
              />
            );
          })}
        </CellGrid>
      )}
    </section>
  );
}
