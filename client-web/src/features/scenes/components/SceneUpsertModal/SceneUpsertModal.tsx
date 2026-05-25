import type { SyntheticEvent } from "react";
import { BooleanSwitch } from "@/shared/ui/BooleanSwitch";
import { Button } from "@/shared/ui/Button";
import type { CapabilityRegistryMap } from "@/features/capabilities";
import type {
  HomeRoomOverviewDto
} from "@/features/homes";
import type { BuilderDeviceDto } from "@/features/capability-builder";
import { Form } from "@/shared/ui/Form";
import { FormGroup } from "@/shared/ui/FormGroup";
import { Input } from "@/shared/ui/Input";
import { Modal } from "@/shared/ui/Modal";
import {
  ActionSetEditor,
  type ActionSetDraft,
} from "@/features/action-sets";
import styles from "@/shared/styles/featurePage.module.css";
import modalStyles from "@/shared/styles/modalActions.module.css";
import { useTranslation } from "react-i18next";

type Props = {
  open: boolean;
  mode: "create" | "edit";
  title: string;
  submitLabel: string;
  submittingLabel: string;
  name: string;
  description: string;
  isEnabled: boolean;
  actionSet: ActionSetDraft;
  rooms?: HomeRoomOverviewDto[];
  availableDevices?: BuilderDeviceDto[];
  availableDevicesByRoom?: Record<string, BuilderDeviceDto[]>;
  registryMap?: CapabilityRegistryMap;
  isSaving: boolean;
  isDeleting?: boolean;
  error: string | null;
  onClose: () => void;
  onSubmit: (event: SyntheticEvent<HTMLFormElement>) => void;
  onDelete?: () => void;
  deleteLabel?: string;
  deletingLabel?: string;
  onNameChange: (value: string) => void;
  onDescriptionChange: (value: string) => void;
  onEnabledChange: (value: boolean) => void;
  onActionSetChange: (value: ActionSetDraft) => void;
};

export function SceneUpsertModal({
  open,
  mode,
  title,
  submitLabel,
  submittingLabel,
  name,
  description,
  isEnabled,
  actionSet,
  rooms,
  availableDevices,
  availableDevicesByRoom,
  registryMap,
  isSaving,
  isDeleting = false,
  error,
  onClose,
  onSubmit,
  onDelete,
  deleteLabel,
  deletingLabel,
  onNameChange,
  onDescriptionChange,
  onEnabledChange,
  onActionSetChange,
}: Props) {
  const { t } = useTranslation("scenes");

  return (
    <Modal open={open} title={title} onClose={onClose}>
      <Form onSubmit={onSubmit}>
        <FormGroup label={t("scenes.name")} htmlFor="scene-name">
          <Input
            id="scene-name"
            value={name}
            onChange={(event) => onNameChange(event.target.value)}
            required
          />
        </FormGroup>

        <FormGroup
          label={t("scenes.description")}
          htmlFor="scene-description"
          required={false}
        >
          <Input
            id="scene-description"
            value={description}
            onChange={(event) => onDescriptionChange(event.target.value)}
          />
        </FormGroup>

        <BooleanSwitch
          id="scene-enabled"
          checked={isEnabled}
          disabled={isSaving || isDeleting}
          label={isEnabled ? t("scenes.enabled") : t("scenes.disabled")}
          onChange={onEnabledChange}
        />

        <ActionSetEditor
          value={actionSet}
          rooms={rooms}
          availableDevices={availableDevices}
          availableDevicesByRoom={availableDevicesByRoom}
          registryMap={registryMap}
          disabled={isSaving}
          labels={{
            mainActions: t("scenes.actionSet.mainActions"),
            beforeHooks: t("scenes.actionSet.beforeHooks"),
            successHooks: t("scenes.actionSet.successHooks"),
            failureHooks: t("scenes.actionSet.failureHooks"),
            sequential: t("scenes.actionSet.sequential"),
            parallel: t("scenes.actionSet.parallel"),
            continueOnError: t("scenes.actionSet.continueOnError"),
          }}
          onChange={onActionSetChange}
        />

        {error ? (
          <p className={styles.helperText}>{t(error, { defaultValue: error })}</p>
        ) : null}

        <div className={`${styles.metaRow} ${modalStyles.actionsRow}`}>
          <Button type="submit" disabled={isSaving || isDeleting}>
            {isSaving ? submittingLabel : submitLabel}
          </Button>
          <Button type="button" variant="secondary" onClick={onClose}>
            {t("cancel")}
          </Button>
          {mode === "edit" && onDelete ? (
            <Button
              type="button"
              variant="danger"
              className={modalStyles.deleteButton}
              onClick={onDelete}
              disabled={isSaving || isDeleting}
            >
              {isDeleting
                ? (deletingLabel ?? t("scenes.deleting"))
                : (deleteLabel ?? t("scenes.delete"))}
            </Button>
          ) : null}
        </div>
      </Form>
    </Modal>
  );
}
