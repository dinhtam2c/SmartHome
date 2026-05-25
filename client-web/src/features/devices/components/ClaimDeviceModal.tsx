import type { SyntheticEvent } from "react";
import { useTranslation } from "react-i18next";
import { Button } from "@/shared/ui/Button";
import { Form } from "@/shared/ui/Form";
import { FormActions } from "@/shared/ui/FormActions";
import { FormGroup } from "@/shared/ui/FormGroup";
import { Input } from "@/shared/ui/Input";
import { Modal } from "@/shared/ui/Modal";
import styles from "./ClaimDeviceModal.module.css";

export type DeviceRoomOption = {
  id: string;
  name: string;
};

type Props = {
  open: boolean;
  onClose: () => void;
  onSubmit: (event: SyntheticEvent<HTMLFormElement>) => void;
  provisionCode: string;
  onProvisionCodeChange: (value: string) => void;
  roomId?: string;
  onRoomIdChange?: (value: string) => void;
  rooms?: DeviceRoomOption[];
  isSaving: boolean;
  error: string | null;
};

export function ClaimDeviceModal({
  open,
  onClose,
  onSubmit,
  provisionCode,
  onProvisionCodeChange,
  roomId,
  onRoomIdChange,
  rooms,
  isSaving,
  error,
}: Props) {
  const { t } = useTranslation("devices");
  const showRoomSelect = roomId !== undefined && onRoomIdChange && rooms;

  return (
    <Modal open={open} title={t("claimTitle")} onClose={onClose}>
      <Form onSubmit={onSubmit}>
        {showRoomSelect ? (
          <FormGroup label={t("room")} htmlFor="claim-device-room">
            <select
              id="claim-device-room"
              className={styles.select}
              value={roomId}
              onChange={(event) => onRoomIdChange(event.target.value)}
            >
              <option value="">{t("unassigned")}</option>
              {rooms.map((room) => (
                <option key={room.id} value={room.id}>{room.name}</option>
              ))}
            </select>
          </FormGroup>
        ) : null}

        <FormGroup label={t("provisionCode")} htmlFor="claim-device-code">
          <Input
            id="claim-device-code"
            value={provisionCode}
            onChange={(event) => onProvisionCodeChange(event.target.value)}
            placeholder={t("provisionCodePlaceholder")}
            required
          />
        </FormGroup>

        {error ? <p className={styles.error}>{error}</p> : null}

        <FormActions>
          <Button type="button" variant="secondary" onClick={onClose}>{t("cancel")}</Button>
          <Button type="submit" disabled={isSaving}>
            {isSaving ? t("claiming") : t("claim")}
          </Button>
        </FormActions>
      </Form>
    </Modal>
  );
}
