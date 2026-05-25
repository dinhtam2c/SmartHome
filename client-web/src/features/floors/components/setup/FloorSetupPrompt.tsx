import type { SyntheticEvent } from "react";
import { Button } from "@/shared/ui/Button";
import { Form } from "@/shared/ui/Form";
import { FormActions } from "@/shared/ui/FormActions";
import { FormGroup } from "@/shared/ui/FormGroup";
import { Input } from "@/shared/ui/Input";
import styles from "./FloorSetupPrompt.module.css";

type Props = {
  homeName: string;
  name: string;
  canvasWidth: string;
  canvasHeight: string;
  isCreating: boolean;
  error: string | null;
  title: string;
  saveLabel: string;
  savingLabel: string;
  nameLabel: string;
  widthLabel: string;
  heightLabel: string;
  minCanvasWidth: number;
  minCanvasHeight: number;
  onSubmit: (event: SyntheticEvent<HTMLFormElement>) => void;
  onNameChange: (value: string) => void;
  onCanvasWidthChange: (value: string) => void;
  onCanvasHeightChange: (value: string) => void;
};

export function FloorSetupPrompt({
  homeName,
  name,
  canvasWidth,
  canvasHeight,
  isCreating,
  error,
  title,
  saveLabel,
  savingLabel,
  nameLabel,
  widthLabel,
  heightLabel,
  minCanvasWidth,
  minCanvasHeight,
  onSubmit,
  onNameChange,
  onCanvasWidthChange,
  onCanvasHeightChange,
}: Props) {
  return (
    <section className={styles.prompt}>
      <div className={styles.intro}>
        <div className={styles.eyebrow}>{homeName}</div>
        <h2 className={styles.title}>{title}</h2>
      </div>

      <Form onSubmit={onSubmit}>
        <FormGroup label={nameLabel} htmlFor="floors-create-name">
          <Input
            id="floors-create-name"
            value={name}
            onChange={(event) => onNameChange(event.target.value)}
          />
        </FormGroup>

        <div className={styles.dimensionGrid}>
          <FormGroup label={widthLabel} htmlFor="floors-create-width">
            <Input
              id="floors-create-width"
              type="number"
              min={minCanvasWidth}
              step={10}
              value={canvasWidth}
              onChange={(event) => onCanvasWidthChange(event.target.value)}
            />
          </FormGroup>

          <FormGroup label={heightLabel} htmlFor="floors-create-height">
            <Input
              id="floors-create-height"
              type="number"
              min={minCanvasHeight}
              step={10}
              value={canvasHeight}
              onChange={(event) => onCanvasHeightChange(event.target.value)}
            />
          </FormGroup>
        </div>

        {error ? <div className={styles.error}>{error}</div> : null}

        <FormActions>
          <Button type="submit" disabled={isCreating}>
            {isCreating ? savingLabel : saveLabel}
          </Button>
        </FormActions>
      </Form>
    </section>
  );
}
