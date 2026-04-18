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
  onClose: () => void;
  onSubmit: (event: SyntheticEvent<HTMLFormElement>) => void;
  name: string;
  onNameChange: (value: string) => void;
  description: string;
  onDescriptionChange: (value: string) => void;
  isSaving: boolean;
  error: string | null;
};

export function CreateRoomModal({
  open,
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
    <Modal open={open} title={t("detail.createRoom")} onClose={onClose}>
      <Form onSubmit={onSubmit}>
        <FormGroup label={t("detail.roomName")} htmlFor="new-room-name">
          <Input
            id="new-room-name"
            value={name}
            onChange={(event) => onNameChange(event.target.value)}
            required
          />
        </FormGroup>

        <FormGroup label={t("detail.roomDescription")} htmlFor="new-room-description" required={false}>
          <Input
            id="new-room-description"
            value={description}
            onChange={(event) => onDescriptionChange(event.target.value)}
          />
        </FormGroup>

        {error ? <p className={styles.helperText}>{t(error, { defaultValue: error })}</p> : null}

        <div className={styles.metaRow}>
          <Button type="submit" disabled={isSaving}>
            {isSaving ? t("detail.creatingRoom") : t("detail.createRoom")}
          </Button>
          <Button type="button" variant="secondary" onClick={onClose}>
            {t("cancel")}
          </Button>
        </div>
      </Form>
    </Modal>
  );
}
