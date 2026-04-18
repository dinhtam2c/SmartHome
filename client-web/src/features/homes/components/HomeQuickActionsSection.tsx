import { useCallback, useEffect, useRef, useState } from "react";
import { Button } from "@/components/Button";
import type { HomeSceneSummaryDto } from "@/features/homes/homes.types";
import { LONG_PRESS_DURATION_MS } from "@/features/shared/interactionConstants";
import styles from "@/features/shared/featurePage.module.css";
import pageStyles from "../HomeDetailPage.module.css";
import { useTranslation } from "react-i18next";

type Props = {
  quickActions: HomeSceneSummaryDto[];
  executingQuickActionId: string | null;
  deletingQuickActionId: string | null;
  onAddQuickAction: () => void;
  onExecuteQuickAction: (sceneId: string) => void;
  onOpenQuickActionEdit: (sceneId: string) => void;
};

export function HomeQuickActionsSection({
  quickActions,
  executingQuickActionId,
  deletingQuickActionId,
  onAddQuickAction,
  onExecuteQuickAction,
  onOpenQuickActionEdit,
}: Props) {
  const { t } = useTranslation("homes");
  const longPressTimerRef = useRef<number | null>(null);
  const longPressTriggeredRef = useRef(false);
  const [pressingSceneId, setPressingSceneId] = useState<string | null>(null);

  const clearLongPressTimer = useCallback(() => {
    if (longPressTimerRef.current !== null) {
      window.clearTimeout(longPressTimerRef.current);
      longPressTimerRef.current = null;
    }
  }, []);

  useEffect(() => () => clearLongPressTimer(), [clearLongPressTimer]);

  const handlePointerDown = useCallback(
    (sceneId: string, disabled: boolean) => {
      if (disabled) {
        return;
      }

      clearLongPressTimer();
      longPressTriggeredRef.current = false;
      setPressingSceneId(sceneId);

      longPressTimerRef.current = window.setTimeout(() => {
        longPressTriggeredRef.current = true;
        setPressingSceneId(null);
        onOpenQuickActionEdit(sceneId);
      }, LONG_PRESS_DURATION_MS);
    },
    [clearLongPressTimer, onOpenQuickActionEdit]
  );

  const handlePointerEnd = useCallback(() => {
    clearLongPressTimer();
    setPressingSceneId(null);
  }, [clearLongPressTimer]);

  const enabledQuickActions = quickActions.filter((quickAction) => quickAction.isEnabled);
  const disabledQuickActions = quickActions.filter((quickAction) => !quickAction.isEnabled);

  return (
    <section className={styles.section}>
      <div className={styles.sectionHeader}>
        <h2 className={styles.sectionTitle}>{t("scenes.title")}</h2>
        <Button size="sm" onClick={onAddQuickAction}>
          {t("scenes.create")}
        </Button>
      </div>

      {quickActions.length === 0 ? (
        <div className={styles.emptyState}>{t("scenes.noScenes")}</div>
      ) : (
        <div className={pageStyles.quickActionGroups}>
          {enabledQuickActions.length > 0 ? (
            <div className={pageStyles.quickActionList}>
              {enabledQuickActions.map((quickAction) => {
                const isExecuting = executingQuickActionId === quickAction.id;
                const isDeleting = deletingQuickActionId === quickAction.id;
                const isDisabled = isExecuting || isDeleting;

                return (
                  <button
                    key={quickAction.id}
                    className={`${pageStyles.quickActionItem} ${pressingSceneId === quickAction.id ? pageStyles.quickActionItemPressed : ""}`}
                    type="button"
                    disabled={isDisabled}
                    onPointerDown={() => handlePointerDown(quickAction.id, isDisabled)}
                    onPointerUp={handlePointerEnd}
                    onPointerCancel={handlePointerEnd}
                    onPointerLeave={handlePointerEnd}
                    onClick={() => {
                      if (longPressTriggeredRef.current) {
                        longPressTriggeredRef.current = false;
                        return;
                      }

                      if (!quickAction.isEnabled) {
                        return;
                      }

                      onExecuteQuickAction(quickAction.id);
                    }}
                    title={t("scenes.execute")}
                  >
                    <span className={pageStyles.quickActionName}>
                      {isExecuting ? t("scenes.executing") : quickAction.name}
                    </span>
                  </button>
                );
              })}
            </div>
          ) : null}

          {disabledQuickActions.length > 0 ? (
            <div className={`${pageStyles.quickActionList} ${pageStyles.quickActionListDisabled}`}>
              {disabledQuickActions.map((quickAction) => {
                const isExecuting = executingQuickActionId === quickAction.id;
                const isDeleting = deletingQuickActionId === quickAction.id;
                const isDisabled = isExecuting || isDeleting;

                return (
                  <button
                    key={quickAction.id}
                    className={`${pageStyles.quickActionItem} ${pageStyles.quickActionItemDisabled} ${pressingSceneId === quickAction.id ? pageStyles.quickActionItemPressed : ""}`}
                    type="button"
                    disabled={isDisabled}
                    onPointerDown={() => handlePointerDown(quickAction.id, isDisabled)}
                    onPointerUp={handlePointerEnd}
                    onPointerCancel={handlePointerEnd}
                    onPointerLeave={handlePointerEnd}
                    onClick={() => {
                      if (longPressTriggeredRef.current) {
                        longPressTriggeredRef.current = false;
                        return;
                      }

                      onExecuteQuickAction(quickAction.id);
                    }}
                    title={t("scenes.execute")}
                  >
                    <span className={pageStyles.quickActionName}>
                      {isExecuting ? t("scenes.executing") : quickAction.name}
                    </span>
                  </button>
                );
              })}
            </div>
          ) : null}
        </div>
      )}
    </section>
  );
}
