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
  isEditing: boolean;
  roomId: string;
  roomName: string;
  fillColor: string;
  polygonPointCount: number;
  availableRooms: HomeRoomOverviewDto[];
  isSaving: boolean;
  isDeleting: boolean;
  error: string | null;
  deleteLabel: string;
  deletingLabel: string;
  saveLabel: string;
  savingLabel: string;
  cancelLabel: string;
  roomLabel: string;
  selectRoomLabel: string;
  fillColorLabel: string;
  polygonLabel: string;
  polygonHint: string;
  onClose: () => void;
  onSubmit: (event: SyntheticEvent<HTMLFormElement>) => void;
  onDelete?: () => void;
  onRoomIdChange: (value: string) => void;
  onFillColorChange: (value: string) => void;
};

export function RoomFormModal(props: Props) {
  const {
    open,
    title,
    isEditing,
    roomId,
    roomName,
    fillColor,
    polygonPointCount,
    availableRooms,
    isSaving,
    isDeleting,
    error,
    deleteLabel,
    deletingLabel,
    saveLabel,
    savingLabel,
    cancelLabel,
    roomLabel,
    selectRoomLabel,
    fillColorLabel,
    polygonLabel,
    polygonHint,
    onClose,
    onSubmit,
    onDelete,
    onRoomIdChange,
    onFillColorChange,
  } = props;

  return (
    <Modal open={open} title={title} onClose={onClose}>
      <Form onSubmit={onSubmit}>
        <FormGroup label={roomLabel} htmlFor="floors-room-logical">
          {isEditing ? (
            <Input id="floors-room-logical" value={roomName} readOnly />
          ) : (
            <select
              id="floors-room-logical"
              className={sharedStyles.select}
              value={roomId}
              required
              onChange={(event) => onRoomIdChange(event.target.value)}
            >
              <option value="" disabled>{selectRoomLabel}</option>
              {availableRooms.map((room) => (
                <option key={room.id} value={room.id}>{room.name}</option>
              ))}
            </select>
          )}
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
          <span>{polygonPointCount} · {polygonHint}</span>
        </div>

        {error ? <div className={styles.error}>{error}</div> : null}

        <FormActions>
          <Button type="button" variant="secondary" onClick={onClose}>{cancelLabel}</Button>
          {onDelete ? (
            <Button type="button" variant="danger" disabled={isDeleting} onClick={onDelete}>
              {isDeleting ? deletingLabel : deleteLabel}
            </Button>
          ) : null}
          <Button type="submit" disabled={isSaving || (!isEditing && !roomId)}>
            {isSaving ? savingLabel : saveLabel}
          </Button>
        </FormActions>
      </Form>
    </Modal>
  );
}
