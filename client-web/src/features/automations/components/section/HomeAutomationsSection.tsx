import { useTranslation } from "react-i18next";
import { Button } from "@/shared/ui/Button";
import { StatusChip } from "@/shared/ui/StatusChip";
import type { HomeRoomOverviewDto } from "@/features/homes";
import { useHomeAutomations } from "../../hooks/useHomeAutomations";
import { AutomationUpsertModal } from "../modal/AutomationUpsertModal";
import styles from "@/shared/styles/featurePage.module.css";
import pageStyles from "./HomeAutomationsSection.module.css";

type Props = {
  homeId: string;
  rooms: HomeRoomOverviewDto[];
};

export function HomeAutomationsSection({ homeId, rooms }: Props) {
  const { t } = useTranslation("automations");
  const {
    availableDevices,
    availableDevicesByRoom,
    capabilityRegistry,
    deletingRuleId,
    executingRuleId,
    form,
    handleDelete,
    handleExecute,
    handleSave,
    hasLoadedRules,
    isLoading,
    loadError,
    openCreateModal,
    openEditModal,
    sortedRules,
  } = useHomeAutomations({ homeId, rooms });
  const editingRuleId = form.editingRuleId;

  return (
    <section className={styles.section}>
      <div className={styles.sectionHeader}>
        <h2 className={styles.sectionTitle}>{t("automations.title")}</h2>
        <Button size="sm" onClick={() => void openCreateModal()}>
          {t("automations.create")}
        </Button>
      </div>

      {isLoading && !hasLoadedRules ? (
        <div className={styles.emptyState}>{t("automations.loading")}</div>
      ) : loadError ? (
        <div className={styles.emptyState}>{t(loadError, { defaultValue: loadError })}</div>
      ) : sortedRules.length === 0 ? (
        <div className={styles.emptyState}>{t("automations.empty")}</div>
      ) : (
        <div className={pageStyles.ruleList}>
          {sortedRules.map((rule) => {
            const isExecuting = executingRuleId === rule.id;
            const isDeleting = deletingRuleId === rule.id;
            const busy = isExecuting || isDeleting;

            return (
              <button
                key={rule.id}
                type="button"
                className={pageStyles.ruleCard}
                disabled={busy}
                onClick={() => void openEditModal(rule.id)}
              >
                <div className={pageStyles.ruleHeader}>
                  <div className={pageStyles.ruleCopy}>
                    <div className={pageStyles.ruleTitleRow}>
                      <div className={pageStyles.ruleName}>{rule.name}</div>
                      <StatusChip
                        label={rule.isEnabled ? t("automations.enabled") : t("automations.disabled")}
                        tone={rule.isEnabled ? "online" : "offline"}
                      />
                    </div>
                    <div
                      className={pageStyles.ruleDescription}
                      title={rule.description || t("noDescription")}
                    >
                      {rule.description || t("noDescription")}
                    </div>
                  </div>
                </div>
              </button>
            );
          })}
        </div>
      )}

      <AutomationUpsertModal
        open={form.isModalOpen}
        mode={form.modalMode}
        name={form.name}
        description={form.description}
        isEnabled={form.isEnabled}
        conditionLogic={form.conditionLogic}
        cooldownMsText={form.cooldownMsText}
        conditions={form.conditions}
        timeWindow={form.timeWindow}
        actionSet={form.actionSet}
        rooms={rooms}
        availableDevices={availableDevices}
        availableDevicesByRoom={availableDevicesByRoom}
        registryMap={capabilityRegistry.registryMap}
        isSaving={form.isSaving}
        isExecuting={Boolean(form.editingRuleId && executingRuleId === form.editingRuleId)}
        isDeleting={Boolean(form.editingRuleId && deletingRuleId === form.editingRuleId)}
        ruleDetail={form.editingRuleDetail}
        error={
          form.modalError ||
          (capabilityRegistry.error ? "automations.errors.loadRegistryFailed" : null)
        }
        onClose={form.closeForm}
        onSubmit={handleSave}
        onExecute={
          editingRuleId
            ? () => void handleExecute(editingRuleId, form.isEnabled)
            : undefined
        }
        onDelete={
          editingRuleId ? () => void handleDelete(editingRuleId) : undefined
        }
        onNameChange={form.setName}
        onDescriptionChange={form.setDescription}
        onEnabledChange={form.setIsEnabled}
        onConditionLogicChange={form.setConditionLogic}
        onCooldownMsTextChange={form.setCooldownMsText}
        onChangeCondition={form.changeCondition}
        onTimeWindowChange={form.setTimeWindow}
        onAddCondition={form.addCondition}
        onRemoveCondition={form.removeCondition}
        onActionSetChange={form.setActionSet}
      />
    </section>
  );
}
