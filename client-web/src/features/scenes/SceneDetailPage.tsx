import { useCallback, useEffect, useMemo, useState } from "react";
import type { SyntheticEvent } from "react";
import { useNavigate, useParams } from "react-router-dom";
import { useTranslation } from "react-i18next";
import { Button } from "@/components/Button";
import { useCapabilityRegistry } from "@/features/capabilities";
import { Cell } from "@/components/Cell";
import { CellGrid } from "@/components/CellGrid";
import { DetailRow } from "@/components/DetailRow";
import { DetailsView } from "@/components/DetailsView";
import { Input } from "@/components/Input";
import { Modal } from "@/components/Modal";
import { PageHeader } from "@/components/PageHeader";
import { StatusChip } from "@/components/StatusChip";
import { getHomeDetail, getHomeDevices } from "@/features/homes/homes.api";
import type {
  HomeRoomOverviewDto,
  HomeSceneBuilderDeviceDto,
} from "@/features/homes/homes.types";
import {
  deleteScene,
  executeScene,
  getSceneDetail,
  getSceneExecutionDetail,
  getSceneExecutions,
  updateScene,
} from "./scenes.api";
import type {
  SceneDetailDto,
  SceneExecutionTargetStatus,
  SceneExecutionDetailDto,
  SceneExecutionListItemDto,
  SceneExecutionStatus,
} from "./scenes.types";
import {
  buildSceneSideEffectRequest,
  buildSceneTargetRequest,
  createEmptySceneTargetDraft,
  createEmptySceneSideEffectDraft,
  sceneDetailSideEffectToDraft,
  sceneDetailTargetToDraft,
  type SceneTargetDraft,
  type SceneSideEffectDraft,
} from "./sceneFormUtils";
import {
  getSideEffectReadOnlyPaths,
  getTargetReadOnlyPaths,
} from "./sceneReadOnlyUtils";
import { SceneUpsertModal } from "./components/SceneUpsertModal";
import { timestampToDateTime } from "@/utils/dateTimeUtils";
import styles from "@/features/shared/featurePage.module.css";
import pageStyles from "./SceneDetailPage.module.css";

const DEFAULT_PAGE_SIZE = 20;

function getExecutionTone(status: SceneExecutionStatus) {
  if (status === "Running") {
    return "pending" as const;
  }

  if (status === "Completed") {
    return "completed" as const;
  }

  return "failed" as const;
}

function getExecutionTargetTone(status: SceneExecutionTargetStatus) {
  if (status === "PendingEvaluation" || status === "CommandPending" || status === "CommandAccepted") {
    return "pending" as const;
  }

  if (
    status === "CommandCompleted"
    || status === "SkippedAlreadySatisfied"
    || status === "Verified"
  ) {
    return "completed" as const;
  }

  return "failed" as const;
}

export function SceneDetailPage() {
  const { t } = useTranslation("homes");
  const navigate = useNavigate();
  const { homeId, sceneId } = useParams();

  const [scene, setScene] = useState<SceneDetailDto | null>(null);
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const [isEditOpen, setIsEditOpen] = useState(false);
  const [isSaving, setIsSaving] = useState(false);
  const [isDeleting, setIsDeleting] = useState(false);
  const [isExecuting, setIsExecuting] = useState(false);
  const [actionError, setActionError] = useState<string | null>(null);
  const [sceneRooms, setSceneRooms] = useState<HomeRoomOverviewDto[]>([]);
  const [sceneBuilderDevices, setSceneBuilderDevices] = useState<
    HomeSceneBuilderDeviceDto[]
  >([]);
  const [sceneBuilderDevicesByRoom, setSceneBuilderDevicesByRoom] = useState<
    Record<string, HomeSceneBuilderDeviceDto[]>
  >({});
  const [sceneBuilderDevicesError, setSceneBuilderDevicesError] = useState<string | null>(null);

  const capabilityRegistry = useCapabilityRegistry();

  const [name, setName] = useState("");
  const [description, setDescription] = useState("");
  const [isEnabled, setIsEnabled] = useState(true);
  const [targets, setTargets] = useState<SceneTargetDraft[]>([]);
  const [sideEffects, setSideEffects] = useState<SceneSideEffectDraft[]>([]);

  const [executionStatusFilter, setExecutionStatusFilter] = useState<
    "all" | SceneExecutionStatus
  >("all");
  const [executionPage, setExecutionPage] = useState(1);
  const [executionPageSize, setExecutionPageSize] = useState(DEFAULT_PAGE_SIZE);
  const [executions, setExecutions] = useState<SceneExecutionListItemDto[]>([]);
  const [executionTotalCount, setExecutionTotalCount] = useState(0);
  const [isExecutionLoading, setIsExecutionLoading] = useState(false);
  const [executionError, setExecutionError] = useState<string | null>(null);

  const [selectedExecution, setSelectedExecution] =
    useState<SceneExecutionDetailDto | null>(null);
  const [isExecutionDetailOpen, setIsExecutionDetailOpen] = useState(false);

  const loadScene = useCallback(async () => {
    if (!homeId || !sceneId) {
      return;
    }

    setIsLoading(true);
    setError(null);

    try {
      const sceneDetail = await getSceneDetail(homeId, sceneId);
      setScene(sceneDetail);
    } catch (loadError) {
      setError((loadError as Error).message || "scenes.errors.loadDetailFailed");
    } finally {
      setIsLoading(false);
    }
  }, [homeId, sceneId]);

  const loadSceneBuilderContext = useCallback(async () => {
    if (!homeId) {
      setSceneRooms([]);
      setSceneBuilderDevices([]);
      setSceneBuilderDevicesByRoom({});
      setSceneBuilderDevicesError(null);
      return;
    }

    setSceneBuilderDevicesError(null);

    try {
      const homeDetail = await getHomeDetail(homeId);
      const roomIds = homeDetail.rooms.map((room) => room.id);
      const [devices, roomEntries] = await Promise.all([
        getHomeDevices(homeId),
        Promise.all(
          roomIds.map(async (roomId) => {
            const roomDevices = await getHomeDevices(homeId, roomId);
            return [roomId, roomDevices] as const;
          })
        ),
      ]);

      setSceneRooms(homeDetail.rooms);
      setSceneBuilderDevices(devices);
      setSceneBuilderDevicesByRoom(Object.fromEntries(roomEntries));
    } catch (loadError) {
      setSceneBuilderDevicesError(
        (loadError as Error).message || "scenes.errors.loadSceneBuilderDevicesFailed"
      );
    }
  }, [homeId]);

  const loadExecutions = useCallback(async () => {
    if (!homeId || !sceneId) {
      return;
    }

    setIsExecutionLoading(true);
    setExecutionError(null);

    try {
      const data = await getSceneExecutions(homeId, sceneId, {
        status: executionStatusFilter === "all" ? undefined : executionStatusFilter,
        page: executionPage,
        pageSize: executionPageSize,
      });
      setExecutions(data.items);
      setExecutionTotalCount(data.totalCount);
    } catch (loadError) {
      setExecutionError((loadError as Error).message || "scenes.errors.loadExecutionsFailed");
    } finally {
      setIsExecutionLoading(false);
    }
  }, [executionPage, executionPageSize, executionStatusFilter, homeId, sceneId]);

  useEffect(() => {
    void loadScene();
  }, [loadScene]);

  useEffect(() => {
    void loadSceneBuilderContext();
  }, [loadSceneBuilderContext]);

  useEffect(() => {
    void loadExecutions();
  }, [loadExecutions]);

  const openEditModal = useCallback(() => {
    if (!scene) {
      return;
    }

    setActionError(null);
    setName(scene.name);
    setDescription(scene.description ?? "");
    setIsEnabled(scene.isEnabled);
    setTargets(
      scene.targets.length > 0
        ? scene.targets
          .slice()
          .sort((left, right) => left.order - right.order)
          .map(sceneDetailTargetToDraft)
        : []
    );
    setSideEffects(
      scene.sideEffects.length > 0
        ? scene.sideEffects
          .slice()
          .sort((left, right) => left.order - right.order)
          .map(sceneDetailSideEffectToDraft)
        : []
    );
    setIsEditOpen(true);
  }, [scene]);

  const closeEditModal = useCallback(() => {
    setIsEditOpen(false);
  }, []);

  const changeTargetDraft = useCallback((index: number, action: SceneTargetDraft) => {
    setTargets((current) =>
      current.map((currentAction, currentIndex) =>
        currentIndex === index ? action : currentAction
      )
    );
  }, []);

  const addTargetDraft = useCallback(() => {
    setTargets((current) => [...current, createEmptySceneTargetDraft()]);
  }, []);

  const removeTargetDraft = useCallback((index: number) => {
    setTargets((current) =>
      current.filter((_, currentIndex) => currentIndex !== index)
    );
  }, []);

  const changeSideEffectDraft = useCallback((index: number, sideEffect: SceneSideEffectDraft) => {
    setSideEffects((current) =>
      current.map((currentSideEffect, currentIndex) =>
        currentIndex === index ? sideEffect : currentSideEffect
      )
    );
  }, []);

  const addSideEffectDraft = useCallback(() => {
    setSideEffects((current) => [...current, createEmptySceneSideEffectDraft()]);
  }, []);

  const removeSideEffectDraft = useCallback((index: number) => {
    setSideEffects((current) =>
      current.filter((_, currentIndex) => currentIndex !== index)
    );
  }, []);

  const handleSaveScene = useCallback(
    async (event: SyntheticEvent<HTMLFormElement>) => {
      event.preventDefault();

      if (!homeId || !sceneId) {
        return;
      }

      if (!name.trim()) {
        setActionError("scenes.errors.nameRequired");
        return;
      }

      if (targets.length === 0 && sideEffects.length === 0) {
        setActionError("scenes.errors.atLeastOneTargetRequired");
        return;
      }

      const targetRequests = [];
      const sideEffectRequests = [];

      for (let index = 0; index < targets.length; index += 1) {
        const parsedTarget = buildSceneTargetRequest(targets[index], {
          readOnlyPaths: getTargetReadOnlyPaths(
            targets[index],
            sceneBuilderDevices,
            capabilityRegistry.registryMap
          ),
        });

        if (!parsedTarget.value || parsedTarget.errorKey) {
          setActionError(parsedTarget.errorKey ?? "scenes.errors.invalidTarget");
          return;
        }

        targetRequests.push(parsedTarget.value);
      }

      for (let index = 0; index < sideEffects.length; index += 1) {
        const parsedSideEffect = buildSceneSideEffectRequest(sideEffects[index], {
          readOnlyPaths: getSideEffectReadOnlyPaths(
            sideEffects[index],
            sceneBuilderDevices,
            capabilityRegistry.registryMap
          ),
        });

        if (!parsedSideEffect.value || parsedSideEffect.errorKey) {
          setActionError(parsedSideEffect.errorKey ?? "scenes.errors.invalidTarget");
          return;
        }

        sideEffectRequests.push(parsedSideEffect.value);
      }

      setIsSaving(true);
      setActionError(null);

      try {
        await updateScene(homeId, sceneId, {
          name: name.trim(),
          description: description.trim() || null,
          isEnabled,
          targets: targetRequests,
          sideEffects: sideEffectRequests,
        });

        setIsEditOpen(false);
        await Promise.all([loadScene(), loadExecutions()]);
      } catch (saveError) {
        setActionError((saveError as Error).message || "scenes.errors.updateFailed");
      } finally {
        setIsSaving(false);
      }
    },
    [
      targets,
      description,
      homeId,
      isEnabled,
      loadExecutions,
      loadScene,
      name,
      capabilityRegistry.registryMap,
      sceneBuilderDevices,
      sideEffects,
      sceneId,
    ]
  );

  const handleDeleteScene = useCallback(async () => {
    if (!homeId || !sceneId) {
      return;
    }

    if (!window.confirm(t("scenes.deleteConfirm"))) {
      return;
    }

    setIsDeleting(true);
    setActionError(null);

    try {
      await deleteScene(homeId, sceneId);
      navigate(`/homes/${homeId}`);
    } catch (deleteError) {
      setActionError((deleteError as Error).message || "scenes.errors.deleteFailed");
    } finally {
      setIsDeleting(false);
    }
  }, [homeId, navigate, sceneId, t]);

  const handleExecuteScene = useCallback(async () => {
    if (!homeId || !sceneId) {
      return;
    }

    setIsExecuting(true);
    setActionError(null);

    try {
      await executeScene(homeId, sceneId, { triggerSource: "web-ui" });
      await loadExecutions();
    } catch (executeError) {
      setActionError((executeError as Error).message || "scenes.errors.executeFailed");
    } finally {
      setIsExecuting(false);
    }
  }, [homeId, loadExecutions, sceneId]);

  const openExecutionDetail = useCallback(
    async (executionId: string) => {
      if (!homeId || !sceneId) {
        return;
      }

      setExecutionError(null);

      try {
        const detail = await getSceneExecutionDetail(homeId, sceneId, executionId);
        setSelectedExecution(detail);
        setIsExecutionDetailOpen(true);
      } catch (detailError) {
        setExecutionError(
          (detailError as Error).message || "scenes.errors.loadExecutionDetailFailed"
        );
      }
    },
    [homeId, sceneId]
  );

  const closeExecutionDetail = useCallback(() => {
    setIsExecutionDetailOpen(false);
  }, []);

  useEffect(() => {
    setExecutionPage(1);
  }, [executionStatusFilter, executionPageSize]);

  const executionStatusOptions = useMemo(
    () => ["Running", "Completed", "CompletedWithErrors"] as SceneExecutionStatus[],
    []
  );

  const sceneRegistryError =
    capabilityRegistry.error?.message ||
    (capabilityRegistry.error ? "scenes.errors.loadRegistryFailed" : null);

  if (isLoading) {
    return <div className={styles.emptyState}>{t("scenes.loading")}</div>;
  }

  if (error) {
    return <div className={styles.emptyState}>{t(error, { defaultValue: error })}</div>;
  }

  if (!scene) {
    return <div className={styles.emptyState}>{t("scenes.notFound")}</div>;
  }

  return (
    <div className={styles.pageStack}>
      <PageHeader
        title={scene.name}
        action={
          <div className={styles.metaRow}>
            <Button
              variant="secondary"
              size="sm"
              onClick={() => navigate(`/homes/${scene.homeId}`)}
            >
              {t("scenes.backToHome")}
            </Button>
            <Button variant="secondary" size="sm" onClick={openEditModal}>
              {t("scenes.edit")}
            </Button>
            <Button size="sm" onClick={() => void handleExecuteScene()} disabled={isExecuting}>
              {isExecuting ? t("scenes.executing") : t("scenes.execute")}
            </Button>
            <Button
              variant="danger"
              size="sm"
              onClick={() => void handleDeleteScene()}
              disabled={isDeleting}
            >
              {isDeleting ? t("scenes.deleting") : t("scenes.delete")}
            </Button>
          </div>
        }
      />

      <section className={styles.section}>
        <div className={styles.sectionHeader}>
          <h2 className={styles.sectionTitle}>{t("scenes.details")}</h2>
        </div>

        <DetailsView>
          <DetailRow label={t("scenes.name")}>{scene.name}</DetailRow>
          <DetailRow label={t("scenes.description")}>{scene.description || t("noDescription")}</DetailRow>
          <DetailRow label={t("scenes.status")}>
            <StatusChip
              label={scene.isEnabled ? t("scenes.enabled") : t("scenes.disabled")}
              tone={scene.isEnabled ? "online" : "offline"}
            />
          </DetailRow>
          <DetailRow label={t("scenes.createdAt")}>
            {timestampToDateTime(scene.createdAt)}
          </DetailRow>
          <DetailRow label={t("scenes.updatedAt")}>
            {timestampToDateTime(scene.updatedAt)}
          </DetailRow>
        </DetailsView>
      </section>

      <section className={styles.section}>
        <div className={styles.sectionHeader}>
          <h2 className={styles.sectionTitle}>{t("scenes.targets")}</h2>
        </div>

        {scene.targets.length === 0 ? (
          <div className={styles.emptyState}>{t("scenes.noTargets")}</div>
        ) : (
          <CellGrid>
            {scene.targets
              .slice()
              .sort((left, right) => left.order - right.order)
              .map((action) => (
                <Cell
                  key={action.id}
                  id={action.id}
                  title={`${action.capabilityId} (${action.order})`}
                  subtitle={
                    <div className={pageStyles.actionMeta}>
                      <div>{t("scenes.deviceId")}: {action.deviceId}</div>
                      <div>{t("scenes.endpointId")}: {action.endpointId}</div>
                      <div className={pageStyles.jsonValue}>
                        {JSON.stringify(action.desiredState, null, 2)}
                      </div>
                    </div>
                  }
                  onClick={() => void 0}
                  disabled={true}
                />
              ))}
          </CellGrid>
        )}
      </section>

      <section className={styles.section}>
        <div className={styles.sectionHeader}>
          <h2 className={styles.sectionTitle}>{t("scenes.sideEffects")}</h2>
        </div>

        {scene.sideEffects.length === 0 ? (
          <div className={styles.emptyState}>{t("scenes.noSideEffects")}</div>
        ) : (
          <CellGrid>
            {scene.sideEffects
              .slice()
              .sort((left, right) => left.order - right.order)
              .map((sideEffect) => (
                <Cell
                  key={sideEffect.id}
                  id={sideEffect.id}
                  title={`${sideEffect.capabilityId}.${sideEffect.operation} (${sideEffect.order})`}
                  subtitle={
                    <div className={pageStyles.actionMeta}>
                      <div>{t("scenes.deviceId")}: {sideEffect.deviceId}</div>
                      <div>{t("scenes.endpointId")}: {sideEffect.endpointId}</div>
                      <div>
                        {t("scenes.timing")}: {t(`scenes.sideEffectTimings.${sideEffect.timing}`, {
                          defaultValue: sideEffect.timing,
                        })}
                      </div>
                      <div>{t("scenes.delayMs")}: {sideEffect.delayMs}</div>
                      <div className={pageStyles.jsonValue}>
                        {JSON.stringify(sideEffect.params, null, 2)}
                      </div>
                    </div>
                  }
                  onClick={() => void 0}
                  disabled={true}
                />
              ))}
          </CellGrid>
        )}
      </section>

      <section className={styles.section}>
        <div className={styles.sectionHeader}>
          <h2 className={styles.sectionTitle}>{t("scenes.executions")}</h2>
        </div>

        <div className={pageStyles.filterRow}>
          <div>
            <label className={styles.helperText} htmlFor="scene-execution-status-filter">
              {t("scenes.filterStatus")}
            </label>
            <select
              id="scene-execution-status-filter"
              className={styles.select}
              value={executionStatusFilter}
              onChange={(event) =>
                setExecutionStatusFilter(
                  event.target.value as "all" | SceneExecutionStatus
                )
              }
            >
              <option value="all">{t("scenes.allStatuses")}</option>
              {executionStatusOptions.map((status) => (
                <option key={status} value={status}>
                  {status}
                </option>
              ))}
            </select>
          </div>

          <div>
            <label className={styles.helperText} htmlFor="scene-execution-page-size-filter">
              {t("scenes.pageSize")}
            </label>
            <select
              id="scene-execution-page-size-filter"
              className={styles.select}
              value={String(executionPageSize)}
              onChange={(event) => setExecutionPageSize(Number(event.target.value))}
            >
              <option value="10">10</option>
              <option value="20">20</option>
              <option value="50">50</option>
              <option value="100">100</option>
            </select>
          </div>

          <div>
            <label className={styles.helperText} htmlFor="scene-execution-page-input">
              {t("scenes.page")}
            </label>
            <Input
              id="scene-execution-page-input"
              type="number"
              min={1}
              value={String(executionPage)}
              onChange={(event) => setExecutionPage(Math.max(1, Number(event.target.value) || 1))}
            />
          </div>

          <div>
            <label className={styles.helperText}>{t("scenes.total")}</label>
            <div className={pageStyles.detailLine}>{executionTotalCount}</div>
          </div>
        </div>

        {isExecutionLoading ? (
          <div className={styles.emptyState}>{t("scenes.loadingExecutions")}</div>
        ) : executionError ? (
          <div className={styles.emptyState}>{t(executionError, { defaultValue: executionError })}</div>
        ) : executions.length === 0 ? (
          <div className={styles.emptyState}>{t("scenes.noExecutions")}</div>
        ) : (
          <CellGrid>
            {executions.map((execution) => (
              <Cell
                key={execution.id}
                id={execution.id}
                title={execution.id}
                subtitle={
                  <div className={pageStyles.executionMeta}>
                    <div className={pageStyles.executionStatusRow}>
                      <StatusChip
                        label={execution.status}
                        tone={getExecutionTone(execution.status)}
                      />
                      <span>
                        {t("scenes.successFailure", {
                          success: execution.successfulTargets,
                          failed: execution.failedTargets,
                          total: execution.totalTargets,
                        })}
                      </span>
                    </div>
                    <div>{t("scenes.triggerSource")}: {execution.triggerSource || "-"}</div>
                    <div>{t("scenes.startedAt")}: {timestampToDateTime(execution.startedAt)}</div>
                    <div>
                      {t("scenes.finishedAt")}: {execution.finishedAt ? timestampToDateTime(execution.finishedAt) : "-"}
                    </div>
                  </div>
                }
                onClick={() => void openExecutionDetail(execution.id)}
                disabled={false}
              />
            ))}
          </CellGrid>
        )}
      </section>

      {actionError ? (
        <div className={styles.emptyState}>{t(actionError, { defaultValue: actionError })}</div>
      ) : null}

      <SceneUpsertModal
        open={isEditOpen}
        mode="edit"
        title={t("scenes.edit")}
        submitLabel={t("scenes.saveShort")}
        submittingLabel={t("scenes.saving")}
        name={name}
        description={description}
        isEnabled={isEnabled}
        targets={targets}
        sideEffects={sideEffects}
        rooms={sceneRooms}
        availableDevices={sceneBuilderDevices}
        availableDevicesByRoom={sceneBuilderDevicesByRoom}
        registryMap={capabilityRegistry.registryMap}
        isSaving={isSaving}
        error={actionError || sceneBuilderDevicesError || sceneRegistryError}
        onClose={closeEditModal}
        onSubmit={handleSaveScene}
        deleteLabel={t("scenes.deleteShort")}
        deletingLabel={t("scenes.deleting")}
        onNameChange={setName}
        onDescriptionChange={setDescription}
        onEnabledChange={setIsEnabled}
        onChangeTarget={changeTargetDraft}
        onAddTarget={addTargetDraft}
        onRemoveTarget={removeTargetDraft}
        onChangeSideEffect={changeSideEffectDraft}
        onAddSideEffect={addSideEffectDraft}
        onRemoveSideEffect={removeSideEffectDraft}
      />

      <Modal
        open={isExecutionDetailOpen}
        title={t("scenes.executionDetails")}
        onClose={closeExecutionDetail}
      >
        {!selectedExecution ? (
          <div className={styles.emptyState}>{t("scenes.errors.loadExecutionDetailFailed")}</div>
        ) : (
          <div className={pageStyles.sectionStack}>
            <DetailsView>
              <DetailRow label={t("scenes.executionId")}>{selectedExecution.id}</DetailRow>
              <DetailRow label={t("scenes.status")}>
                <StatusChip
                  label={selectedExecution.status}
                  tone={getExecutionTone(selectedExecution.status)}
                />
              </DetailRow>
              <DetailRow label={t("scenes.triggerSource")}>{selectedExecution.triggerSource || "-"}</DetailRow>
              <DetailRow label={t("scenes.startedAt")}>{timestampToDateTime(selectedExecution.startedAt)}</DetailRow>
              <DetailRow label={t("scenes.finishedAt")}>
                {selectedExecution.finishedAt
                  ? timestampToDateTime(selectedExecution.finishedAt)
                  : "-"}
              </DetailRow>
            </DetailsView>

            <div className={styles.sectionHeader}>
              <h2 className={styles.sectionTitle}>{t("scenes.executionTargets")}</h2>
            </div>

            {selectedExecution.targets.length === 0 ? (
              <div className={styles.emptyState}>{t("scenes.noTargets")}</div>
            ) : (
              <CellGrid>
                {selectedExecution.targets
                  .slice()
                  .sort((left, right) => left.order - right.order)
                  .map((action) => (
                    <Cell
                      key={action.id}
                      id={action.id}
                      title={`${action.capabilityId} (${action.order})`}
                      subtitle={
                        <div className={pageStyles.actionMeta}>
                          <div className={pageStyles.executionStatusRow}>
                            <StatusChip
                              label={action.status}
                              tone={getExecutionTargetTone(action.status)}
                            />
                          </div>
                          <div>{t("scenes.deviceId")}: {action.deviceId}</div>
                          <div>{t("scenes.endpointId")}: {action.endpointId}</div>
                          <div>{t("scenes.error")} : {action.error || "-"}</div>
                          <div>
                            {t("scenes.unresolvedDiff")}: {action.unresolvedDiff ? "yes" : "-"}
                          </div>
                          <div className={pageStyles.jsonValue}>
                            {JSON.stringify(action.desiredState, null, 2)}
                          </div>
                          {action.unresolvedDiff ? (
                            <div className={pageStyles.jsonValue}>
                              {JSON.stringify(action.unresolvedDiff, null, 2)}
                            </div>
                          ) : null}
                        </div>
                      }
                      onClick={() => void 0}
                      disabled={true}
                    />
                  ))}
              </CellGrid>
            )}

            <div className={styles.sectionHeader}>
              <h2 className={styles.sectionTitle}>{t("scenes.executionSideEffects")}</h2>
            </div>

            {selectedExecution.sideEffects.length === 0 ? (
              <div className={styles.emptyState}>{t("scenes.noSideEffects")}</div>
            ) : (
              <CellGrid>
                {selectedExecution.sideEffects
                  .slice()
                  .sort((left, right) => left.order - right.order)
                  .map((sideEffect) => (
                    <Cell
                      key={sideEffect.id}
                      id={sideEffect.id}
                      title={`${sideEffect.capabilityId}.${sideEffect.operation} (${sideEffect.order})`}
                      subtitle={
                        <div className={pageStyles.actionMeta}>
                          <div className={pageStyles.executionStatusRow}>
                            <StatusChip
                              label={sideEffect.status}
                              tone={
                                sideEffect.status === "Succeeded"
                                  ? "completed"
                                  : sideEffect.status === "Pending"
                                    ? "pending"
                                    : sideEffect.status === "Skipped"
                                      ? "offline"
                                      : "failed"
                              }
                            />
                          </div>
                          <div>{t("scenes.deviceId")}: {sideEffect.deviceId}</div>
                          <div>{t("scenes.endpointId")}: {sideEffect.endpointId}</div>
                          <div>
                            {t("scenes.timing")}: {t(`scenes.sideEffectTimings.${sideEffect.timing}`, {
                              defaultValue: sideEffect.timing,
                            })}
                          </div>
                          <div>{t("scenes.delayMs")}: {sideEffect.delayMs}</div>
                          <div>{t("scenes.error")} : {sideEffect.error || "-"}</div>
                          <div className={pageStyles.jsonValue}>
                            {JSON.stringify(sideEffect.params, null, 2)}
                          </div>
                        </div>
                      }
                      onClick={() => void 0}
                      disabled={true}
                    />
                  ))}
              </CellGrid>
            )}
          </div>
        )}
      </Modal>
    </div>
  );
}
