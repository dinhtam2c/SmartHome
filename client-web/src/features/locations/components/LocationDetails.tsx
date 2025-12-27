import { useState } from "react";
import { useLocationDetails } from "../hooks/useLocationDetails";

import type { LocationUpdateRequest } from "../locations.types";

import { LocationForm } from "./LocationForm";

import { Button } from "@/components/Button";
import { DetailsView } from "@/components/DetailsView/DetailsView";
import { DetailRow } from "@/components/DetailRow/DetailRow";
import { timestampToDateTime } from "@/utils/dateTimeUtils";

interface Props {
  id: string | null;
  onUpdateSuccess: () => void;
  onDeleteSuccess: () => void;
}

export function LocationDetails({
  id,
  onUpdateSuccess,
  onDeleteSuccess,
}: Props) {
  const {
    location,
    loading,
    error,
    isUpdating,
    isDeleting,
    updateLocation,
    deleteLocation,
  } = useLocationDetails(id);
  const [isEditing, setIsEditing] = useState(false);

  async function handleDelete() {
    if (!confirm("Are you sure to delete this location?")) return;

    await deleteLocation();
    onDeleteSuccess();
  }

  function handleEdit() {
    setIsEditing(true);
  }

  function handleCancelUpdate() {
    setIsEditing(false);
  }

  async function handleUpdate(request: LocationUpdateRequest) {
    if (!location) return;
    await updateLocation(request);
    onUpdateSuccess();
    setIsEditing(false);
  }

  if (loading) return <div>Loading...</div>;
  if (error) return <div>Error: {error}</div>;
  if (!location) return <div>Location not found</div>;

  if (!isEditing) {
    return (
      <div>
        <DetailsView
          actions={
            <>
              <Button
                variant="secondary"
                onClick={handleEdit}
                disabled={isDeleting}
              >
                Edit
              </Button>
              <Button
                variant="danger"
                onClick={handleDelete}
                disabled={isDeleting}
              >
                {isDeleting ? "Deleting..." : "Delete"}
              </Button>
            </>
          }
        >
          <DetailRow label="Name">{location.name}</DetailRow>
          <DetailRow label="Description">{location.description}</DetailRow>
          <DetailRow label="Created at">
            {timestampToDateTime(location.createdAt)}
          </DetailRow>
          <DetailRow label="Updated at">
            {timestampToDateTime(location.updatedAt)}
          </DetailRow>
        </DetailsView>
      </div>
    );
  } else {
    return (
      <LocationForm
        initialName={location.name}
        initialDescription={location.description ?? ""}
        submitLabel="Save"
        isSubmitting={isUpdating}
        onSubmit={handleUpdate}
        onCancel={handleCancelUpdate}
      />
    );
  }
}
