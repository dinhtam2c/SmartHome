import { useCallback, useEffect, useMemo, useState } from "react";
import type { SyntheticEvent } from "react";
import { useNavigate, useParams } from "react-router-dom";
import { useTranslation } from "react-i18next";
import { Button } from "@/components/Button";
import { Cell } from "@/components/Cell";
import { CellGrid } from "@/components/CellGrid";
import { PageHeader } from "@/components/PageHeader";
import { StatusChip } from "@/components/StatusChip";
import { getHomeDetail } from "@/features/homes/homes.api";
import type { HomeDetailDto } from "@/features/homes/homes.types";
import { timestampToDateTime } from "@/utils/dateTimeUtils";
import { createScene, getScenes } from "./scenes.api";
import type { SceneListItemDto } from "./scenes.types";
import {
  buildSceneSideEffectRequest,
  buildSceneTargetRequest,
  createEmptySceneTargetDraft,
  createEmptySceneSideEffectDraft,
  type SceneTargetDraft,
  type SceneSideEffectDraft,
} from "./sceneFormUtils";
import { SceneUpsertModal } from "./components/SceneUpsertModal";
import styles from "@/features/shared/featurePage.module.css";
import pageStyles from "./ScenesPage.module.css";

export function ScenesPage() {
  const { t } = useTranslation("homes");
  const navigate = useNavigate();
  const { homeId } = useParams();

  const [home, setHome] = useState<HomeDetailDto | null>(null);
  const [scenes, setScenes] = useState<SceneListItemDto[]>([]);
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const [isCreateOpen, setIsCreateOpen] = useState(false);
  const [isCreating, setIsCreating] = useState(false);
  const [createError, setCreateError] = useState<string | null>(null);
  const [name, setName] = useState("");
  const [description, setDescription] = useState("");
  const [isEnabled, setIsEnabled] = useState(true);
  const [targets, setTargets] = useState<SceneTargetDraft[]>([]);
  const [sideEffects, setSideEffects] = useState<SceneSideEffectDraft[]>([]);

  const loadData = useCallback(async () => {
    if (!homeId) {
      return;
    }

    setIsLoading(true);
    setError(null);

    try {
      const [homeDetail, sceneItems] = await Promise.all([
        getHomeDetail(homeId),
        getScenes(homeId),
      ]);
      setHome(homeDetail);
      setScenes(sceneItems);
    } catch (loadError) {
      setError((loadError as Error).message || "scenes.errors.loadListFailed");
    } finally {
      setIsLoading(false);
    }
  }, [homeId]);

  useEffect(() => {
    void loadData();
  }, [loadData]);

  const sortedScenes = useMemo(
    () => [...scenes].sort((left, right) => right.updatedAt - left.updatedAt),
    [scenes]
  );

  const resetCreateForm = useCallback(() => {
    setName("");
    setDescription("");
    setIsEnabled(true);
    setTargets([]);
    setSideEffects([]);
  }, []);

  const openCreateModal = useCallback(() => {
    setCreateError(null);
    resetCreateForm();
    setIsCreateOpen(true);
  }, [resetCreateForm]);

  const closeCreateModal = useCallback(() => {
    setIsCreateOpen(false);
  }, []);

  const handleTargetChange = useCallback((index: number, action: SceneTargetDraft) => {
    setTargets((current) =>
      current.map((existingAction, currentIndex) =>
        currentIndex === index ? action : existingAction
      )
    );
  }, []);

  const handleAddAction = useCallback(() => {
    setTargets((current) => [...current, createEmptySceneTargetDraft()]);
  }, []);

  const handleRemoveAction = useCallback((index: number) => {
    setTargets((current) =>
      current.filter((_, currentIndex) => currentIndex !== index)
    );
  }, []);

  const handleSideEffectChange = useCallback((index: number, sideEffect: SceneSideEffectDraft) => {
    setSideEffects((current) =>
      current.map((existingSideEffect, currentIndex) =>
        currentIndex === index ? sideEffect : existingSideEffect
      )
    );
  }, []);

  const handleAddSideEffect = useCallback(() => {
    setSideEffects((current) => [...current, createEmptySceneSideEffectDraft()]);
  }, []);

  const handleRemoveSideEffect = useCallback((index: number) => {
    setSideEffects((current) =>
      current.filter((_, currentIndex) => currentIndex !== index)
    );
  }, []);

  const handleCreateScene = useCallback(
    async (event: SyntheticEvent<HTMLFormElement>) => {
      event.preventDefault();

      if (!homeId) {
        return;
      }

      if (!name.trim()) {
        setCreateError("scenes.errors.nameRequired");
        return;
      }

      if (targets.length === 0 && sideEffects.length === 0) {
        setCreateError("scenes.errors.atLeastOneTargetRequired");
        return;
      }

      const targetRequests = [];
      const sideEffectRequests = [];

      for (let index = 0; index < targets.length; index += 1) {
        const parsedTarget = buildSceneTargetRequest(targets[index]);

        if (!parsedTarget.value || parsedTarget.errorKey) {
          setCreateError(parsedTarget.errorKey ?? "scenes.errors.invalidTarget");
          return;
        }

        targetRequests.push(parsedTarget.value);
      }

      for (let index = 0; index < sideEffects.length; index += 1) {
        const parsedSideEffect = buildSceneSideEffectRequest(sideEffects[index]);

        if (!parsedSideEffect.value || parsedSideEffect.errorKey) {
          setCreateError(parsedSideEffect.errorKey ?? "scenes.errors.invalidTarget");
          return;
        }

        sideEffectRequests.push(parsedSideEffect.value);
      }

      setIsCreating(true);
      setCreateError(null);

      try {
        await createScene(homeId, {
          name: name.trim(),
          description: description.trim() || null,
          isEnabled,
          targets: targetRequests,
          sideEffects: sideEffectRequests,
        });

        setIsCreateOpen(false);
        resetCreateForm();
        await loadData();
      } catch (submitError) {
        setCreateError((submitError as Error).message || "scenes.errors.createFailed");
      } finally {
        setIsCreating(false);
      }
    },
    [targets, description, homeId, isEnabled, loadData, name, resetCreateForm, sideEffects]
  );

  if (isLoading) {
    return <div className={styles.emptyState}>{t("scenes.loading")}</div>;
  }

  if (error) {
    return <div className={styles.emptyState}>{t(error, { defaultValue: error })}</div>;
  }

  if (!home) {
    return <div className={styles.emptyState}>{t("detail.notFound")}</div>;
  }

  return (
    <div className={styles.pageStack}>
      <PageHeader
        title={t("scenes.title")}
        action={
          <div className={styles.metaRow}>
            <Button
              variant="secondary"
              size="sm"
              onClick={() => navigate(`/homes/${home.id}`)}
            >
              {t("scenes.backToHome")}
            </Button>
            <Button size="sm" onClick={openCreateModal}>
              {t("scenes.create")}
            </Button>
          </div>
        }
      />

      {sortedScenes.length === 0 ? (
        <div className={styles.emptyState}>{t("scenes.noScenes")}</div>
      ) : (
        <CellGrid>
          {sortedScenes.map((scene) => (
            <Cell
              key={scene.id}
              id={scene.id}
              title={scene.name}
              subtitle={
                <div className={pageStyles.sceneSubtitle}>
                  <div className={pageStyles.statusRow}>
                    <StatusChip
                      label={scene.isEnabled ? t("scenes.enabled") : t("scenes.disabled")}
                      tone={scene.isEnabled ? "online" : "offline"}
                    />
                    <span>{t("scenes.targetCount", { count: scene.targetCount })}</span>
                  </div>
                  <div>{scene.description || t("noDescription")}</div>
                  <div className={pageStyles.sceneMeta}>
                    {t("scenes.updatedAt")}: {timestampToDateTime(scene.updatedAt)}
                  </div>
                </div>
              }
              onClick={() => navigate(`/homes/${home.id}/scenes/${scene.id}`)}
              disabled={false}
            />
          ))}
        </CellGrid>
      )}

      <SceneUpsertModal
        open={isCreateOpen}
        mode="create"
        title={t("scenes.create")}
        submitLabel={t("scenes.saveShort")}
        submittingLabel={t("scenes.saving")}
        name={name}
        description={description}
        isEnabled={isEnabled}
        targets={targets}
        sideEffects={sideEffects}
        isSaving={isCreating}
        error={createError}
        onClose={closeCreateModal}
        onSubmit={handleCreateScene}
        onNameChange={setName}
        onDescriptionChange={setDescription}
        onEnabledChange={setIsEnabled}
        onChangeTarget={handleTargetChange}
        onAddTarget={handleAddAction}
        onRemoveTarget={handleRemoveAction}
        onChangeSideEffect={handleSideEffectChange}
        onAddSideEffect={handleAddSideEffect}
        onRemoveSideEffect={handleRemoveSideEffect}
      />
    </div>
  );
}
