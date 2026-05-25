import type { SyntheticEvent } from "react";
import { Button } from "@/shared/ui/Button";
import { Form } from "@/shared/ui/Form";
import { FormActions } from "@/shared/ui/FormActions";
import { FormGroup } from "@/shared/ui/FormGroup";
import { Input } from "@/shared/ui/Input";
import { Modal } from "@/shared/ui/Modal";
import styles from "./FloorInfoModal.module.css";

type Props = {
  open: boolean;
  title: string;
  name: string;
  canvasWidth: string;
  canvasHeight: string;
  isSaving: boolean;
  error: string | null;
  saveLabel: string;
  savingLabel: string;
  cancelLabel: string;
  nameLabel: string;
  widthLabel: string;
  heightLabel: string;
  minCanvasWidth: number;
  minCanvasHeight: number;
  helperText: string;
  onClose: () => void;
  onSubmit: (event: SyntheticEvent<HTMLFormElement>) => void;
  onNameChange: (value: string) => void;
  onCanvasWidthChange: (value: string) => void;
  onCanvasHeightChange: (value: string) => void;
};

export function FloorInfoModal({
  open,
  title,
  name,
  canvasWidth,
  canvasHeight,
  isSaving,
  error,
  saveLabel,
  savingLabel,
  cancelLabel,
  nameLabel,
  widthLabel,
  heightLabel,
  minCanvasWidth,
  minCanvasHeight,
  helperText,
  onClose,
  onSubmit,
  onNameChange,
  onCanvasWidthChange,
  onCanvasHeightChange,
}: Props) {
  return (
    <Modal open={open} title={title} onClose={onClose}>
      <Form onSubmit={onSubmit}>
        <FormGroup label={nameLabel} htmlFor="floors-name">
          <Input
            id="floors-name"
            value={name}
            onChange={(event) => onNameChange(event.target.value)}
          />
        </FormGroup>

        <div className={styles.dimensionGrid}>
          <FormGroup label={widthLabel} htmlFor="floors-width">
            <Input
              id="floors-width"
              type="number"
              min={minCanvasWidth}
              step={10}
              value={canvasWidth}
              onChange={(event) => onCanvasWidthChange(event.target.value)}
            />
          </FormGroup>

          <FormGroup label={heightLabel} htmlFor="floors-height">
            <Input
              id="floors-height"
              type="number"
              min={minCanvasHeight}
              step={10}
              value={canvasHeight}
              onChange={(event) => onCanvasHeightChange(event.target.value)}
            />
          </FormGroup>
        </div>

        <p className={styles.helper}>{helperText}</p>
        {error ? <div className={styles.error}>{error}</div> : null}

        <FormActions>
          <Button type="button" variant="secondary" onClick={onClose}>
            {cancelLabel}
          </Button>
          <Button type="submit" disabled={isSaving}>
            {isSaving ? savingLabel : saveLabel}
          </Button>
        </FormActions>
      </Form>
    </Modal>
  );
}
