import type { HomeAddRequest } from "../home.types";
import HomeForm from "./HomeForm";

interface Props {
  onAdd: (request: HomeAddRequest) => void;
  onCancel?: () => void;
  isSubmitting?: boolean;
}

export default function AddHomeForm({ onAdd, onCancel, isSubmitting }: Props) {
  return (
    <HomeForm
      submitLabel="Add"
      onSubmit={onAdd}
      onCancel={onCancel}
      isSubmitting={isSubmitting}
    />
  );
}
