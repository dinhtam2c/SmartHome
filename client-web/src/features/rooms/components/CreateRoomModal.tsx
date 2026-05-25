import type { SyntheticEvent } from "react";
import { Button } from "@/shared/ui/Button";
import { Form } from "@/shared/ui/Form";
import { FormGroup } from "@/shared/ui/FormGroup";
import { Input } from "@/shared/ui/Input";
import { Modal } from "@/shared/ui/Modal";
import { FormActions } from "@/shared/ui/FormActions";
import styles from "./RoomModal.module.css";
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
  const { t } = useTranslation("rooms");

  return (
    <Modal open={open} title={t("createTitle")} onClose={onClose}>
      <Form onSubmit={onSubmit}>
        <FormGroup label={t("editName")} htmlFor="new-room-name">
          <Input
            id="new-room-name"
            value={name}
            onChange={(event) => onNameChange(event.target.value)}
            required
          />
        </FormGroup>

        <FormGroup label={t("editDescription")} htmlFor="new-room-description" required={false}>
          <Input
            id="new-room-description"
            value={description}
            onChange={(event) => onDescriptionChange(event.target.value)}
          />
        </FormGroup>

        {error ? <p className={styles.error}>{t(error, { defaultValue: error })}</p> : null}

        <FormActions>
          <Button type="button" variant="secondary" onClick={onClose}>
            {t("cancel")}
          </Button>
          <Button type="submit" disabled={isSaving}>
            {isSaving ? t("creatingRoom") : t("create")}
          </Button>
        </FormActions>
      </Form>
    </Modal>
  );
}
