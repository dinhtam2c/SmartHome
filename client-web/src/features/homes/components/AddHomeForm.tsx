import type { HomeAddRequest } from "../homes.types";

import { HomeForm } from "./HomeForm";

interface Props {
  onAdd: (request: HomeAddRequest) => void;
  onCancel?: () => void;
  isSubmitting?: boolean;
}

export function AddHomeForm({ onAdd, onCancel, isSubmitting }: Props) {
  return (
    <HomeForm
      submitLabel="Add"
      onSubmit={onAdd}
      onCancel={onCancel}
      isSubmitting={isSubmitting}
    />
  );
}
