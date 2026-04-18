import type { SyntheticEvent } from "react";
import { Button } from "@/components/Button";
import { Form } from "@/components/Form";
import { FormGroup } from "@/components/FormGroup";
import { Input } from "@/components/Input";
import { Modal } from "@/components/Modal";
import type { HomeRoomOverviewDto } from "../homes.types";
import styles from "@/features/shared/featurePage.module.css";
import { useTranslation } from "react-i18next";

type Props = {
  open: boolean;
  onClose: () => void;
  onSubmit: (event: SyntheticEvent<HTMLFormElement>) => void;
  roomId: string;
  onRoomIdChange: (value: string) => void;
  rooms: HomeRoomOverviewDto[];
  provisionCode: string;
  onProvisionCodeChange: (value: string) => void;
  isSaving: boolean;
  error: string | null;
};

export function CreateDeviceModal({
  open,
  onClose,
  onSubmit,
  roomId,
  onRoomIdChange,
  rooms,
  provisionCode,
  onProvisionCodeChange,
  isSaving,
  error,
}: Props) {
  const { t } = useTranslation("homes");

  return (
    <Modal open={open} title={t("detail.createDevice")} onClose={onClose}>
      <Form onSubmit={onSubmit}>
        <FormGroup label={t("detail.deviceRoom")} htmlFor="new-device-room">
          <select
            id="new-device-room"
            className={styles.select}
            value={roomId}
            onChange={(event) => onRoomIdChange(event.target.value)}
          >
            <option value="">
              {t("detail.unassignedRoom")}
            </option>
            {rooms.map((room) => (
              <option key={room.id} value={room.id}>
                {room.name}
              </option>
            ))}
          </select>
        </FormGroup>

        <FormGroup label={t("detail.deviceCode")} htmlFor="new-device-code">
          <Input
            id="new-device-code"
            value={provisionCode}
            onChange={(event) => onProvisionCodeChange(event.target.value)}
            placeholder={t("detail.deviceCodePlaceholder")}
            required
          />
        </FormGroup>

        {error ? <p className={styles.helperText}>{t(error, { defaultValue: error })}</p> : null}

        <div className={styles.metaRow}>
          <Button type="submit" disabled={isSaving}>
            {isSaving ? t("detail.creatingDevice") : t("detail.createDevice")}
          </Button>
          <Button type="button" variant="secondary" onClick={onClose}>
            {t("cancel")}
          </Button>
        </div>
      </Form>
    </Modal>
  );
}
