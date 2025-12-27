import { useState, type FormEvent } from "react";
import type { LocationUpdateRequest } from "../locations.types";

import { Form } from "@/components/Form";
import { FormGroup } from "@/components/FormGroup";
import { Input } from "@/components/Input";
import { FormActions } from "@/components/FormActions";
import { Button } from "@/components/Button";

interface Props {
  initialName: string;
  initialDescription: string;
  submitLabel: string;
  isSubmitting: boolean;
  onSubmit: (request: LocationUpdateRequest) => Promise<void>;
  onCancel: () => void;
}

export function LocationForm({
  initialName,
  initialDescription,
  submitLabel,
  isSubmitting,
  onSubmit,
  onCancel,
}: Props) {
  const [name, setName] = useState(initialName);
  const [description, setDescription] = useState(initialDescription);

  async function handleSubmit(e: FormEvent) {
    e.preventDefault();
    await onSubmit({ name, description });
  }

  return (
    <Form onSubmit={handleSubmit}>
      <FormGroup label="Name" htmlFor="name">
        <Input
          id="name"
          value={name}
          onChange={(e) => setName(e.target.value)}
          disabled={isSubmitting}
          required
        />
      </FormGroup>

      <FormGroup label="Description" htmlFor="description" required={false}>
        <Input
          id="description"
          value={description}
          onChange={(e) => setDescription(e.target.value)}
          disabled={isSubmitting}
        />
      </FormGroup>

      <FormActions>
        <Button variant="primary" type="submit" disabled={isSubmitting}>
          {submitLabel}
        </Button>
        <Button variant="secondary" onClick={onCancel} disabled={isSubmitting}>
          Cancel
        </Button>
      </FormActions>
    </Form>
  );
}
