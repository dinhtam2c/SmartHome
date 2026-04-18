import { useCallback, useEffect, useRef } from "react";
import { Cell } from "@/components/Cell";
import { CellGrid } from "@/components/CellGrid";
import { DetailRow } from "@/components/DetailRow";
import { DetailsView } from "@/components/DetailsView";
import { LONG_PRESS_DURATION_MS } from "@/features/shared/interactionConstants";
import type { HomeListItemDto } from "../homes.types";
import styles from "@/features/shared/featurePage.module.css";
import { useTranslation } from "react-i18next";

type Props = {
  homes: HomeListItemDto[];
  filteredHomes: HomeListItemDto[];
  query: string;
  onQueryChange: (value: string) => void;
  onOpenHome: (homeId: string) => void;
  onOpenHomeEdit: (homeId: string) => void;
};

export function HomesOverviewSection({
  homes,
  filteredHomes,
  query,
  onQueryChange,
  onOpenHome,
  onOpenHomeEdit,
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
    (homeId: string) => {
      clearLongPressTimer();
      longPressTriggeredRef.current = false;

      longPressTimerRef.current = window.setTimeout(() => {
        longPressTriggeredRef.current = true;
        onOpenHomeEdit(homeId);
      }, LONG_PRESS_DURATION_MS);
    },
    [clearLongPressTimer, onOpenHomeEdit]
  );

  const handlePointerEnd = useCallback(() => {
    clearLongPressTimer();
  }, [clearLongPressTimer]);

  const handleOpenHome = useCallback(
    (homeId: string) => {
      if (longPressTriggeredRef.current) {
        longPressTriggeredRef.current = false;
        return;
      }

      onOpenHome(homeId);
    },
    [onOpenHome]
  );

  return (
    <>
      <div className={styles.toolbar}>
        <input
          className={styles.field}
          placeholder={t("searchPlaceholder")}
          value={query}
          onChange={(event) => onQueryChange(event.target.value)}
        />
      </div>

      <DetailsView>
        <DetailRow label={t("totalHomes")}>{homes.length}</DetailRow>
        <DetailRow label={t("filteredResults")}>{filteredHomes.length}</DetailRow>
      </DetailsView>

      {filteredHomes.length === 0 ? (
        <div className={styles.emptyState}>{t("noMatch")}</div>
      ) : (
        <CellGrid>
          {filteredHomes.map((home) => (
            <Cell
              key={home.id}
              id={home.id}
              title={home.name}
              subtitle={<div>{home.description ?? t("noDescription")}</div>}
              onClick={handleOpenHome}
              onPointerDown={handlePointerDown}
              onPointerUp={handlePointerEnd}
              onPointerCancel={handlePointerEnd}
              onPointerLeave={handlePointerEnd}
              disabled={false}
            />
          ))}
        </CellGrid>
      )}
    </>
  );
}
