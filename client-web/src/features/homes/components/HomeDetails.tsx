import { useState } from "react";
import { useHomeDetails } from "../hooks/useHomeDetails";

import type { HomeUpdateRequest } from "../homes.types";

import { HomeForm } from "./HomeForm";

import { Button } from "@/components/Button";
import { DetailsView } from "@/components/DetailsView/DetailsView";
import { DetailRow } from "@/components/DetailRow/DetailRow";
import { timestampToDateTime } from "@/utils/dateTimeUtils";

interface Props {
  id: string | null;
  onUpdateSuccess: () => void;
  onDeleteSuccess: () => void;
}

export function HomeDetails({
  id,
  onUpdateSuccess,
  onDeleteSuccess,
}: Props) {
  const { home, loading, error, isUpdating, isDeleting, updateHome, deleteHome } =
    useHomeDetails(id);
  const [isEditing, setIsEditing] = useState(false);

  async function handleDelete() {
    if (!confirm("Are you sure to delete this home?")) return;

    await deleteHome();
    onDeleteSuccess();
  }

  function handleEdit() {
    setIsEditing(true);
  }

  function handleCancelUpdate() {
    setIsEditing(false);
  }

  async function handleUpdate(request: HomeUpdateRequest) {
    if (!home) return;
    await updateHome(request);
    onUpdateSuccess();
    setIsEditing(false);
  }

  if (loading) return <div>Loading...</div>;
  if (error) return <div>Error: {error}</div>;
  if (!home) return <div>Home not found</div>;

  if (!isEditing) {
    return (
      <div>
        <DetailsView
          actions={
            <>
              <Button variant="secondary" onClick={handleEdit} disabled={isDeleting}>Edit</Button>
              <Button variant="danger" onClick={handleDelete} disabled={isDeleting}>
                {isDeleting ? "Deleting..." : "Delete"}
              </Button>
            </>
          }
        >
          <DetailRow label="Name">{home.name}</DetailRow>
          <DetailRow label="Description">{home.description}</DetailRow>
          <DetailRow label="Created at">
            {timestampToDateTime(home.createdAt)}
          </DetailRow>
          <DetailRow label="Updated at">
            {timestampToDateTime(home.updatedAt)}
          </DetailRow>
        </DetailsView>
      </div >
    );
  } else {
    return (
      <HomeForm
        initialName={home.name}
        initialDescription={home.description ?? ""}
        submitLabel="Save"
        isSubmitting={isUpdating}
        onSubmit={handleUpdate}
        onCancel={handleCancelUpdate}
      />
    );
  }
}
