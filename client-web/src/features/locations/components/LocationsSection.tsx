import { useState } from "react";
import { useLocations } from "../hooks/useLocations";

import type { LocationAddRequest } from "../locations.types";

import { AddLocationForm } from "./AddLocationForm";
import { LocationDetails } from "./LocationDetails";

import { Modal } from "@/components/Modal/Modal";
import { Button } from "@/components/Button";
import { Cell } from "@/components/Cell";

import styles from "./LocationsSection.module.css";

interface Props {
  homeId: string;
}

export function LocationsSection({ homeId }: Props) {
  const { locations, loading, isAdding, error, addLocation, reload } =
    useLocations(homeId);
  const [isAddModalOpen, setIsAddModalOpen] = useState(false);
  const [selectedLocationId, setSelectedLocationId] = useState<string | null>(
    null
  );

  const selectedLocation = locations.find(
    (loc) => loc.id === selectedLocationId
  );

  async function handleAdd(request: LocationAddRequest) {
    await addLocation(request);
    setIsAddModalOpen(false);
    reload();
  }

  function handleDeleteSuccess() {
    setSelectedLocationId(null);
    reload();
  }

  if (loading) return <div>Loading locations...</div>;
  if (error) return <div>Error loading locations: {error}</div>;

  return (
    <div className={styles.section}>
      <div className={styles.header}>
        <h3 className={styles.title}>Locations</h3>
        <Button variant="primary" onClick={() => setIsAddModalOpen(true)}>
          Add Location
        </Button>
      </div>

      {locations.length === 0 ? (
        <div className={styles.emptyState}>
          No locations yet. Add one to get started.
        </div>
      ) : (
        <div className={styles.grid}>
          {locations.map((loc) => (
            <Cell
              key={loc.id}
              id={loc.id}
              title={loc.name}
              subtitle={loc.description}
              onClick={setSelectedLocationId}
              disabled={false}
            />
          ))}
        </div>
      )}

      <Modal
        open={!!selectedLocationId}
        title={selectedLocation?.name || "Location Details"}
        onClose={() => setSelectedLocationId(null)}
      >
        <LocationDetails
          id={selectedLocationId}
          onUpdateSuccess={reload}
          onDeleteSuccess={handleDeleteSuccess}
        />
      </Modal>

      <Modal
        open={isAddModalOpen}
        title="Add Location"
        onClose={() => setIsAddModalOpen(false)}
      >
        <AddLocationForm
          homeId={homeId}
          onAdd={handleAdd}
          onCancel={() => setIsAddModalOpen(false)}
          isSubmitting={isAdding}
        />
      </Modal>
    </div>
  );
}
