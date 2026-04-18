import type { SyntheticEvent } from "react";
import { BooleanSwitch } from "@/components/BooleanSwitch";
import { Button } from "@/components/Button";
import type { CapabilityRegistryMap } from "@/features/capabilities";
import type {
  HomeRoomOverviewDto,
  HomeSceneBuilderDeviceDto,
} from "@/features/homes/homes.types";
import { Form } from "@/components/Form";
import { FormGroup } from "@/components/FormGroup";
import { Input } from "@/components/Input";
import { Modal } from "@/components/Modal";
import { SceneTargetsEditor } from "./SceneTargetsEditor";
import { SceneSideEffectsEditor } from "./SceneSideEffectsEditor";
import type { SceneTargetDraft, SceneSideEffectDraft } from "../sceneFormUtils";
import styles from "@/features/shared/featurePage.module.css";
import modalStyles from "./SceneUpsertModal.module.css";
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
  targets: SceneTargetDraft[];
  sideEffects: SceneSideEffectDraft[];
  rooms?: HomeRoomOverviewDto[];
  availableDevices?: HomeSceneBuilderDeviceDto[];
  availableDevicesByRoom?: Record<string, HomeSceneBuilderDeviceDto[]>;
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
  onChangeTarget: (index: number, action: SceneTargetDraft) => void;
  onAddTarget: () => void;
  onRemoveTarget: (index: number) => void;
  onChangeSideEffect: (index: number, sideEffect: SceneSideEffectDraft) => void;
  onAddSideEffect: () => void;
  onRemoveSideEffect: (index: number) => void;
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
  targets,
  sideEffects,
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
  onChangeTarget,
  onAddTarget,
  onRemoveTarget,
  onChangeSideEffect,
  onAddSideEffect,
  onRemoveSideEffect,
}: Props) {
  const { t } = useTranslation("homes");

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

        <SceneTargetsEditor
          targets={targets}
          rooms={rooms}
          availableDevices={availableDevices}
          availableDevicesByRoom={availableDevicesByRoom}
          registryMap={registryMap}
          disabled={isSaving}
          onChangeTarget={onChangeTarget}
          onAddTarget={onAddTarget}
          onRemoveTarget={onRemoveTarget}
        />

        <SceneSideEffectsEditor
          sideEffects={sideEffects}
          targets={targets}
          rooms={rooms}
          availableDevices={availableDevices}
          availableDevicesByRoom={availableDevicesByRoom}
          registryMap={registryMap}
          disabled={isSaving}
          onChangeSideEffect={onChangeSideEffect}
          onAddSideEffect={onAddSideEffect}
          onRemoveSideEffect={onRemoveSideEffect}
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
