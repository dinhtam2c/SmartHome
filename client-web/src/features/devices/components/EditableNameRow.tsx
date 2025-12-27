import { Button } from "@/components/Button";
import { DetailRow } from "@/components/DetailRow/DetailRow";
import styles from "./DeviceDetails.module.css";

interface Props {
  name: string;
  isEditing: boolean;
  editValue: string;
  isUpdating: boolean;
  onStartEdit: () => void;
  onSave: () => void;
  onCancel: () => void;
  onChange: (value: string) => void;
}

export function EditableNameRow({
  name,
  isEditing,
  editValue,
  isUpdating,
  onStartEdit,
  onSave,
  onCancel,
  onChange,
}: Props) {
  return (
    <DetailRow label="Name">
      {isEditing ? (
        <div className={styles.editContainer}>
          <input
            value={editValue}
            onChange={(e) => onChange(e.target.value)}
            className={styles.editInput}
          />
          <div className={styles.editActions}>
            <Button
              variant="primary"
              onClick={onSave}
              disabled={isUpdating}
              className={styles.editBtn}
            >
              Save
            </Button>
            <Button
              variant="secondary"
              onClick={onCancel}
              disabled={isUpdating}
              className={styles.editBtn}
            >
              Cancel
            </Button>
          </div>
        </div>
      ) : (
        <div className={styles.viewContainer}>
          {name}
          <Button
            variant="secondary"
            onClick={onStartEdit}
            size="sm"
            className={styles.startEditBtn}
          >
            Edit
          </Button>
        </div>
      )}
    </DetailRow>
  );
}
