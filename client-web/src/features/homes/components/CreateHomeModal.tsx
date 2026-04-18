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
  isCreating: boolean;
  error: string | null;
};

export function CreateHomeModal({
  open,
  onClose,
  onSubmit,
  name,
  onNameChange,
  description,
  onDescriptionChange,
  isCreating,
  error,
}: Props) {
  const { t } = useTranslation("homes");

  return (
    <Modal open={open} title={t("createHome")} onClose={onClose}>
      <Form onSubmit={onSubmit}>
        <FormGroup label={t("name")} htmlFor="home-name">
          <Input
            id="home-name"
            value={name}
            onChange={(event) => onNameChange(event.target.value)}
            placeholder={t("namePlaceholder")}
            required
          />
        </FormGroup>

        <FormGroup label={t("description")} htmlFor="home-description" required={false}>
          <Input
            id="home-description"
            value={description}
            onChange={(event) => onDescriptionChange(event.target.value)}
            placeholder={t("descriptionPlaceholder")}
          />
        </FormGroup>

        {error ? <p className={styles.helperText}>{t(error, { defaultValue: error })}</p> : null}

        <div className={styles.metaRow}>
          <Button type="submit" disabled={isCreating}>
            {isCreating ? t("creating") : t("createHome")}
          </Button>
          <Button type="button" variant="secondary" onClick={onClose}>
            {t("cancel")}
          </Button>
        </div>
      </Form>
    </Modal>
  );
}
