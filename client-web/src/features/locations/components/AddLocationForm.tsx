import { useState, type FormEvent } from "react";
import type { LocationAddRequest } from "../locations.types";

import { Form } from "@/components/Form";
import { FormGroup } from "@/components/FormGroup";
import { Input } from "@/components/Input";
import { FormActions } from "@/components/FormActions";
import { Button } from "@/components/Button";

interface Props {
  homeId: string;
  onAdd: (request: LocationAddRequest) => Promise<void>;
  onCancel: () => void;
  isSubmitting: boolean;
}

export function AddLocationForm({
  homeId,
  onAdd,
  onCancel,
  isSubmitting,
}: Props) {
  const [name, setName] = useState("");
  const [description, setDescription] = useState("");

  async function handleSubmit(e: FormEvent) {
    e.preventDefault();
    await onAdd({ homeId, name, description });
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
          {isSubmitting ? "Adding..." : "Add"}
        </Button>
        <Button variant="secondary" onClick={onCancel} disabled={isSubmitting}>
          Cancel
        </Button>
      </FormActions>
    </Form>
  );
}
