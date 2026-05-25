import type { SyntheticEvent } from "react";
import { Button } from "@/shared/ui/Button";
import { Form } from "@/shared/ui/Form";
import { FormActions } from "@/shared/ui/FormActions";
import { FormGroup } from "@/shared/ui/FormGroup";
import { Input } from "@/shared/ui/Input";
import { Modal } from "@/shared/ui/Modal";
import type { HomeRoomOverviewDto } from "@/features/homes";
import sharedStyles from "@/shared/styles/featurePage.module.css";
import styles from "./RoomFormModal.module.css";

type Props = {
  open: boolean;
  title: string;
  label: string;
  linkedRoomId: string;
  fillColor: string;
  polygonPointCount: number;
  rooms: HomeRoomOverviewDto[];
  isSaving: boolean;
  isDeleting: boolean;
  error: string | null;
  deleteLabel: string;
  deletingLabel: string;
  saveLabel: string;
  savingLabel: string;
  cancelLabel: string;
  labelFieldLabel: string;
  labelPlaceholder: string;
  linkedRoomLabel: string;
  noLinkedRoomLabel: string;
  fillColorLabel: string;
  polygonLabel: string;
  polygonHint: string;
  onClose: () => void;
  onSubmit: (event: SyntheticEvent<HTMLFormElement>) => void;
  onDelete?: () => void;
  onLabelChange: (value: string) => void;
  onLinkedRoomIdChange: (value: string) => void;
  onFillColorChange: (value: string) => void;
};

export function RoomFormModal({
  open,
  title,
  label,
  linkedRoomId,
  fillColor,
  polygonPointCount,
  rooms,
  isSaving,
  isDeleting,
  error,
  deleteLabel,
  deletingLabel,
  saveLabel,
  savingLabel,
  cancelLabel,
  labelFieldLabel,
  labelPlaceholder,
  linkedRoomLabel,
  noLinkedRoomLabel,
  fillColorLabel,
  polygonLabel,
  polygonHint,
  onClose,
  onSubmit,
  onDelete,
  onLabelChange,
  onLinkedRoomIdChange,
  onFillColorChange,
}: Props) {
  return (
    <Modal open={open} title={title} onClose={onClose}>
      <Form onSubmit={onSubmit}>
        <FormGroup label={labelFieldLabel} htmlFor="floors-room-label">
          <Input
            id="floors-room-label"
            value={label}
            onChange={(event) => onLabelChange(event.target.value)}
            placeholder={labelPlaceholder}
          />
        </FormGroup>

        <FormGroup label={linkedRoomLabel} htmlFor="floors-room-linked" required={false}>
          <select
            id="floors-room-linked"
            className={sharedStyles.select}
            value={linkedRoomId}
            onChange={(event) => onLinkedRoomIdChange(event.target.value)}
          >
            <option value="">{noLinkedRoomLabel}</option>
            {rooms.map((room) => (
              <option key={room.id} value={room.id}>
                {room.name}
              </option>
            ))}
          </select>
        </FormGroup>

        <div className={styles.colorRow}>
          <FormGroup label={fillColorLabel} htmlFor="floors-room-color" required={false}>
            <div className={styles.colorField}>
              <input
                id="floors-room-color"
                type="color"
                className={styles.colorInput}
                value={fillColor}
                onChange={(event) => onFillColorChange(event.target.value)}
              />
              <span className={styles.colorValue}>{fillColor}</span>
            </div>
          </FormGroup>
        </div>

        <div className={styles.polygonSummary}>
          <strong>{polygonLabel}</strong>
          <span>
            {polygonPointCount} · {polygonHint}
          </span>
        </div>

        {error ? <div className={styles.error}>{error}</div> : null}

        <FormActions>
          <Button type="button" variant="secondary" onClick={onClose}>
            {cancelLabel}
          </Button>
          {onDelete ? (
            <Button
              type="button"
              variant="danger"
              disabled={isDeleting}
              onClick={onDelete}
            >
              {isDeleting ? deletingLabel : deleteLabel}
            </Button>
          ) : null}
          <Button type="submit" disabled={isSaving}>
            {isSaving ? savingLabel : saveLabel}
          </Button>
        </FormActions>
      </Form>
    </Modal>
  );
}
