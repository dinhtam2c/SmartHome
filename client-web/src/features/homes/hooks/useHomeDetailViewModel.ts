import { useCallback, useMemo } from "react";
import type { TFunction } from "i18next";
import { useHomeDetail } from "./useHomeDetail";

type UseHomeDetailViewModelParams = {
  homeId: string | null;
  t: TFunction<"homes">;
  tScenes: TFunction<"scenes">;
};

export function useHomeDetailViewModel({
  homeId,
  t,
  tScenes,
}: UseHomeDetailViewModelParams) {
  const { home, isLoading, error, reload } = useHomeDetail(homeId);

  const canDeleteHome = Boolean(home && home.deviceCount === 0);
  const quickActions = useMemo(() => home?.scenes ?? [], [home?.scenes]);
  const sceneRooms = useMemo(() => home?.rooms ?? [], [home?.rooms]);

  const formatCapabilityStatePreview = useCallback(
    (state: unknown) => {
      if (state === null || state === undefined) return t("notAvailable");
      if (typeof state === "string" || typeof state === "number") {
        return String(state);
      }

      if (typeof state === "boolean") {
        return state ? tScenes("scenes.stateOn") : tScenes("scenes.stateOff");
      }

      if (Array.isArray(state)) {
        return state.length === 0
          ? "[]"
          : `[${t("itemsCount", { count: state.length })}]`;
      }

      if (typeof state === "object") {
        const entries = Object.entries(state as Record<string, unknown>);
        if (entries.length === 0) return "{}";
        return entries
          .slice(0, 2)
          .map(([key, value]) => {
            if (typeof value === "boolean") {
              return `${key}: ${value ? tScenes("scenes.stateOn") : tScenes("scenes.stateOff")}`;
            }

            return `${key}: ${String(value)}`;
          })
          .join(" · ");
      }

      return String(state);
    },
    [t, tScenes]
  );

  return {
    canDeleteHome,
    error,
    formatCapabilityStatePreview,
    home,
    isLoading,
    quickActions,
    reload,
    sceneRooms,
  };
}
