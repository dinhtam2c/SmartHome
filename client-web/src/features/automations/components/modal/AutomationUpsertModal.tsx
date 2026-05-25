import type { SyntheticEvent } from "react";
import { useTranslation } from "react-i18next";
import { BooleanSwitch } from "@/shared/ui/BooleanSwitch";
import { Button } from "@/shared/ui/Button";
import { DetailRow } from "@/shared/ui/DetailRow/DetailRow";
import { DetailsView } from "@/shared/ui/DetailsView/DetailsView";
import { Form } from "@/shared/ui/Form";
import { FormGroup } from "@/shared/ui/FormGroup";
import { Input } from "@/shared/ui/Input";
import { Modal } from "@/shared/ui/Modal";
import type { CapabilityRegistryMap } from "@/features/capabilities";
import type {
  HomeRoomOverviewDto
} from "@/features/homes";
import type { SelectableDeviceDto } from "@/features/capabilities";
import type { AutomationConditionLogic } from "../../types/automationTypes";
import type {
  AutomationConditionDraft,
  AutomationTimeWindowDraft,
} from "../../services/automationFormService";
import type { AutomationRuleDetailDto } from "../../types/automationTypes";
import { AutomationConditionsEditor } from "../conditions/AutomationConditionsEditor";
import { AutomationTimeWindowEditor } from "../time-window/AutomationTimeWindowEditor";
import styles from "@/shared/styles/featurePage.module.css";
import modalStyles from "@/shared/styles/modalActions.module.css";
import {
  ActionSetEditor,
  type ActionSetDraft,
} from "@/features/action-sets";
import { timestampToDateTime } from "@/shared/lib/dateTimeUtils";

type Props = {
  open: boolean;
  mode: "create" | "edit";
  name: string;
  description: string;
  isEnabled: boolean;
  conditionLogic: AutomationConditionLogic;
  cooldownMsText: string;
  conditions: AutomationConditionDraft[];
  timeWindow: AutomationTimeWindowDraft;
  actionSet: ActionSetDraft;
  rooms?: HomeRoomOverviewDto[];
  availableDevices?: SelectableDeviceDto[];
  availableDevicesByRoom?: Record<string, SelectableDeviceDto[]>;
  registryMap?: CapabilityRegistryMap;
  ruleDetail?: AutomationRuleDetailDto | null;
  isSaving: boolean;
  isExecuting?: boolean;
  isDeleting?: boolean;
  error: string | null;
  onClose: () => void;
  onSubmit: (event: SyntheticEvent<HTMLFormElement>) => void;
  onExecute?: () => void;
  onDelete?: () => void;
  onNameChange: (value: string) => void;
  onDescriptionChange: (value: string) => void;
  onEnabledChange: (value: boolean) => void;
  onConditionLogicChange: (value: AutomationConditionLogic) => void;
  onCooldownMsTextChange: (value: string) => void;
  onChangeCondition: (index: number, condition: AutomationConditionDraft) => void;
  onTimeWindowChange: (value: AutomationTimeWindowDraft) => void;
  onAddCondition: () => void;
  onRemoveCondition: (index: number) => void;
  onActionSetChange: (value: ActionSetDraft) => void;
};

export function AutomationUpsertModal({
  open,
  mode,
  name,
  description,
  isEnabled,
  conditionLogic,
  cooldownMsText,
  conditions,
  timeWindow,
  actionSet,
  rooms,
  availableDevices,
  availableDevicesByRoom,
  registryMap,
  ruleDetail,
  isSaving,
  isExecuting = false,
  isDeleting = false,
  error,
  onClose,
  onSubmit,
  onExecute,
  onDelete,
  onNameChange,
  onDescriptionChange,
  onEnabledChange,
  onConditionLogicChange,
  onCooldownMsTextChange,
  onChangeCondition,
  onTimeWindowChange,
  onAddCondition,
  onRemoveCondition,
  onActionSetChange,
}: Props) {
  const { t } = useTranslation("automations");
  const formDisabled = isSaving || isDeleting;
  const submitDisabled = formDisabled || isExecuting;
  const executeDisabled = formDisabled || isExecuting || !isEnabled;
  const collapseOnOpen = mode !== "create";
  const actionItemCount = ruleDetail
    ? ruleDetail.actionSet.actions.length
    + ruleDetail.actionSet.hooks.before.length
    + ruleDetail.actionSet.hooks.onSuccess.length
    + ruleDetail.actionSet.hooks.onFailure.length
    : 0;

  return (
    <Modal
      open={open}
      title={mode === "create" ? t("automations.create") : t("automations.details")}
      onClose={onClose}
    >
      <Form onSubmit={onSubmit}>
        {mode === "edit" && ruleDetail ? (
          <>
            <DetailsView>
              <DetailRow label={t("automations.conditionLogic")}>
                {t(`automations.conditionLogicOptions.${ruleDetail.conditionLogic}`)}
              </DetailRow>
              <DetailRow label={t("automations.timeWindow.title")}>
                {ruleDetail.timeWindow.enabled
                  ? t("automations.timeWindow.enabled")
                  : t("automations.timeWindow.disabled")}
              </DetailRow>
              <DetailRow label={t("automations.conditions")}>{ruleDetail.conditions.length}</DetailRow>
              <DetailRow label={t("automations.actionItems")}>{actionItemCount}</DetailRow>
              <DetailRow label={t("automations.lastRunAt")}>
                {ruleDetail.lastTriggeredAt
                  ? timestampToDateTime(ruleDetail.lastTriggeredAt)
                  : t("notAvailable")}
              </DetailRow>
            </DetailsView>
          </>
        ) : null}

        <FormGroup label={t("automations.name")} htmlFor="automation-name">
          <Input
            id="automation-name"
            value={name}
            disabled={formDisabled}
            onChange={(event) => onNameChange(event.target.value)}
            required
          />
        </FormGroup>

        <FormGroup
          label={t("automations.description")}
          htmlFor="automation-description"
          required={false}
        >
          <Input
            id="automation-description"
            value={description}
            disabled={formDisabled}
            onChange={(event) => onDescriptionChange(event.target.value)}
          />
        </FormGroup>

        <BooleanSwitch
          id="automation-enabled"
          checked={isEnabled}
          disabled={formDisabled}
          label={isEnabled ? t("automations.enabled") : t("automations.disabled")}
          onChange={onEnabledChange}
        />

        <FormGroup
          label={t("automations.cooldownMs")}
          htmlFor="automation-cooldown-ms"
          required={false}
        >
          <Input
            id="automation-cooldown-ms"
            type="number"
            min={0}
            step={1000}
            value={cooldownMsText}
            disabled={formDisabled}
            onChange={(event) => onCooldownMsTextChange(event.target.value)}
          />
        </FormGroup>

        <AutomationTimeWindowEditor
          value={timeWindow}
          disabled={formDisabled}
          onChange={onTimeWindowChange}
        />

        <AutomationConditionsEditor
          conditions={conditions}
          rooms={rooms}
          availableDevices={availableDevices}
          availableDevicesByRoom={availableDevicesByRoom}
          registryMap={registryMap}
          disabled={formDisabled}
          collapseOnOpen={collapseOnOpen}
          conditionLogic={conditionLogic}
          onConditionLogicChange={onConditionLogicChange}
          onChangeCondition={onChangeCondition}
          onAddCondition={onAddCondition}
          onRemoveCondition={onRemoveCondition}
        />

        <ActionSetEditor
          value={actionSet}
          title={t("automations.then")}
          rooms={rooms}
          availableDevices={availableDevices}
          availableDevicesByRoom={availableDevicesByRoom}
          registryMap={registryMap}
          disabled={formDisabled}
          labels={{
            mainActions: t("automations.actionSet.mainActions"),
            beforeHooks: t("automations.actionSet.beforeHooks"),
            successHooks: t("automations.actionSet.successHooks"),
            failureHooks: t("automations.actionSet.failureHooks"),
            sequential: t("automations.actionSet.sequential"),
            parallel: t("automations.actionSet.parallel"),
            continueOnError: t("automations.actionSet.continueOnError"),
          }}
          onChange={onActionSetChange}
        />

        {error ? (
          <p className={styles.helperText}>{t(error, { defaultValue: error })}</p>
        ) : null}

        <div className={`${styles.metaRow} ${modalStyles.actionsRow}`}>
          <Button type="submit" disabled={submitDisabled}>
            {isSaving ? t("automations.saving") : t("automations.saveShort")}
          </Button>
          <Button type="button" variant="secondary" onClick={onClose}>
            {t("cancel")}
          </Button>
          {mode === "edit" && (onExecute || onDelete) ? (
            <div className={modalStyles.secondaryActions}>
              {onExecute ? (
                <Button
                  type="button"
                  variant="secondary"
                  onClick={onExecute}
                  disabled={executeDisabled}
                >
                  {isExecuting ? t("automations.executing") : t("automations.execute")}
                </Button>
              ) : null}
              {onDelete ? (
                <Button
                  type="button"
                  variant="danger"
                  onClick={onDelete}
                  disabled={formDisabled || isExecuting}
                >
                  {isDeleting ? t("automations.deleting") : t("automations.deleteShort")}
                </Button>
              ) : null}
            </div>
          ) : null}
        </div>
      </Form>
    </Modal>
  );
}
