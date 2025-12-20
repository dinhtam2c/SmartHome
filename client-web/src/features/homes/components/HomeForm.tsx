import { useState, type FormEvent } from "react";
import type { HomeAddRequest, HomeUpdateRequest } from "../home.types";
import Button from "../../../components/Button";

interface Props {
  initialName?: string;
  initialDescription?: string;
  submitLabel: string;
  isSubmitting?: boolean;
  onSubmit: (request: HomeAddRequest | HomeUpdateRequest) => void;
  onCancel?: () => void;
}

export default function HomeForm({
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
    <form className="form" onSubmit={handleSubmit}>
      <label htmlFor="name">Name</label>
      <input
        id="name"
        value={name}
        onChange={(e) => setName(e.target.value)}
        placeholder="Name"
      />

      <label htmlFor="description">Description</label>
      <input
        id="description"
        value={description}
        onChange={(e) => setDescription(e.target.value)}
        placeholder="Description"
      />

      <div className="form__actions">
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
      </div>
    </form>
  );
}
