import { useState, type FormEvent } from "react";

import type { HomeAddRequest, HomeUpdateRequest } from "../home.types";

import { Button } from "@/components/Button";
import { Form } from "@/components/Form";
import { FormActions } from "@/components/FormActions";
import { FormGroup } from "@/components/FormGroup";
import { Input } from "@/components/Input";

interface Props {
  initialName?: string;
  initialDescription?: string;
  submitLabel: string;
  isSubmitting?: boolean;
  onSubmit: (request: HomeAddRequest | HomeUpdateRequest) => void;
  onCancel?: () => void;
}

export function HomeForm({
  initialName = "",
  initialDescription = "",
  submitLabel,
  isSubmitting,
  onSubmit,
  onCancel,
}: Props) {
  const [name, setName] = useState(initialName);
  const [description, setDescription] = useState(initialDescription);

  function handleSubmit(e: FormEvent) {
    e.preventDefault();
    if (!name.trim()) return;

    onSubmit({ name, description });
  }

  return (
    <Form onSubmit={handleSubmit}>
      <FormGroup label="Name" htmlFor="name">
        <Input
          id="name"
          value={name}
          onChange={(e) => setName(e.target.value)}
          placeholder="Name"
          required
        />
      </FormGroup>

      <FormGroup label="Description" htmlFor="description">
        <Input
          id="description"
          value={description}
          onChange={(e) => setDescription(e.target.value)}
          placeholder="Description"
        />
      </FormGroup>

      <FormActions>
        <Button type="submit" disabled={isSubmitting}>
          {isSubmitting ? "..." : submitLabel}
        </Button>
        {onCancel && (
          <Button
            variant="secondary"
            type="button"
            onClick={onCancel}
            disabled={isSubmitting}
          >
            Cancel
          </Button>
        )}
      </FormActions>
    </Form>
  );
}
