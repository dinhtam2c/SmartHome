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
  provisionCode: string;
  onProvisionCodeChange: (value: string) => void;
  isSaving: boolean;
  error: string | null;
};

export function CreateDeviceModal({
  open,
  onClose,
  onSubmit,
  provisionCode,
  onProvisionCodeChange,
  isSaving,
  error,
}: Props) {
  const { t } = useTranslation("rooms");

  return (
    <Modal open={open} title={t("createDevice")} onClose={onClose}>
      <Form onSubmit={onSubmit}>
        <FormGroup label={t("deviceCode")} htmlFor="create-room-device-code">
          <Input
            id="create-room-device-code"
            value={provisionCode}
            onChange={(event) => onProvisionCodeChange(event.target.value)}
            placeholder={t("deviceCodePlaceholder")}
            required
          />
        </FormGroup>

        {error ? <p className={styles.helperText}>{t(error, { defaultValue: error })}</p> : null}

        <div className={styles.metaRow}>
          <Button type="submit" disabled={isSaving}>
            {isSaving ? t("creatingDevice") : t("createDevice")}
          </Button>
          <Button type="button" variant="secondary" onClick={onClose}>
            {t("cancel")}
          </Button>
        </div>
      </Form>
    </Modal>
  );
}
