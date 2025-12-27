import { Button } from "@/components/Button";
import { DetailRow } from "@/components/DetailRow/DetailRow";
import { FormGroup } from "@/components/FormGroup";
import type { LocationListElement } from "@/features/locations/locations.types";

import styles from "./DeviceDetails.module.css";

interface Props {
  homeName: string;
  locations: LocationListElement[];
  selectedLocationId: string;
  isAssigning: boolean;
  isLoading: boolean;
  error: string;
  onLocationChange: (locationId: string) => void;
  onAssign: () => void;
  onCancel: () => void;
}

export function AssignLocationForm({
  homeName,
  locations,
  selectedLocationId,
  isAssigning,
  isLoading,
  error,
  onLocationChange,
  onAssign,
  onCancel,
}: Props) {
  return (
    <div>
      <div className={styles.formGroup}>
        <DetailRow label="Home">{homeName}</DetailRow>
      </div>

      <div className={styles.formGroup}>
        <FormGroup label="Select Location" htmlFor="location-select">
          {isLoading ? (
            <div>Loading locations...</div>
          ) : error ? (
            <div>Error loading locations: {error}</div>
          ) : locations.length === 0 ? (
            <div>No locations available in this home</div>
          ) : (
            <select
              id="location-select"
              value={selectedLocationId}
              onChange={(e) => onLocationChange(e.target.value)}
              disabled={isAssigning}
              className={styles.select}
            >
              <option value="">Select a location</option>
              {locations.map((l) => (
                <option key={l.id} value={l.id}>
                  {l.name}
                </option>
              ))}
            </select>
          )}
        </FormGroup>
      </div>

      <div className={styles.actions}>
        <Button
          variant="primary"
          onClick={onAssign}
          disabled={isAssigning || !selectedLocationId}
        >
          {isAssigning ? "Assigning..." : "Assign"}
        </Button>
        <Button variant="secondary" onClick={onCancel} disabled={isAssigning}>
          Cancel
        </Button>
      </div>
    </div>
  );
}
