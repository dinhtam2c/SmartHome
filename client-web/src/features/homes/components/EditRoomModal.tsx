import type { SyntheticEvent } from "react";
import { Button } from "@/components/Button";
import { Form } from "@/components/Form";
import { FormGroup } from "@/components/FormGroup";
import { Input } from "@/components/Input";
import { Modal } from "@/components/Modal";
import styles from "@/features/shared/featurePage.module.css";
import { useTranslation } from "react-i18next";

type Props = {
  open: boolean;
  title?: string;
  onClose: () => void;
  onSubmit: (event: SyntheticEvent<HTMLFormElement>) => void;
  name: string;
  onNameChange: (value: string) => void;
  description: string;
  onDescriptionChange: (value: string) => void;
  isSaving: boolean;
  error: string | null;
};

export function EditRoomModal({
  open,
  title,
  onClose,
  onSubmit,
  name,
  onNameChange,
  description,
  onDescriptionChange,
  isSaving,
  error,
}: Props) {
  const { t } = useTranslation("homes");

  return (
    <Modal open={open} title={title ?? t("detail.editRoom")} onClose={onClose}>
      <Form onSubmit={onSubmit}>
        <FormGroup label={t("detail.roomName")} htmlFor="edit-room-name">
          <Input
            id="edit-room-name"
            value={name}
            onChange={(event) => onNameChange(event.target.value)}
            required
          />
        </FormGroup>

        <FormGroup
          label={t("detail.roomDescription")}
          htmlFor="edit-room-description"
          required={false}
        >
          <Input
            id="edit-room-description"
            value={description}
            onChange={(event) => onDescriptionChange(event.target.value)}
          />
        </FormGroup>

        {error ? <p className={styles.helperText}>{t(error, { defaultValue: error })}</p> : null}

        <div className={styles.metaRow}>
          <Button type="submit" disabled={isSaving}>
            {isSaving ? t("detail.saving") : t("detail.save")}
          </Button>
          <Button type="button" variant="secondary" onClick={onClose}>
            {t("cancel")}
          </Button>
        </div>
      </Form>
    </Modal>
  );
}
