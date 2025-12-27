import { Button } from "@/components/Button";
import { FormGroup } from "@/components/FormGroup";
import type { GatewayListElement } from "@/features/gateways/gateways.types";

import styles from "./DeviceDetails.module.css";

interface Props {
  gateways: GatewayListElement[];
  selectedGatewayId: string;
  isAssigning: boolean;
  isLoading: boolean;
  error: string;
  onGatewayChange: (gatewayId: string) => void;
  onAssign: () => void;
  onCancel: () => void;
}

export function AssignGatewayForm({
  gateways,
  selectedGatewayId,
  isAssigning,
  isLoading,
  error,
  onGatewayChange,
  onAssign,
  onCancel,
}: Props) {
  return (
    <div>
      <FormGroup label="Select Gateway" htmlFor="gateway-select">
        {isLoading ? (
          <div>Loading gateways...</div>
        ) : error ? (
          <div>Error loading gateways: {error}</div>
        ) : (
          <select
            id="gateway-select"
            value={selectedGatewayId}
            onChange={(e) => onGatewayChange(e.target.value)}
            disabled={isAssigning}
            className={styles.select}
          >
            <option value="">Select a gateway</option>
            {gateways.map((g) => (
              <option key={g.id} value={g.id}>
                {g.name || "Unnamed"} ({g.homeName || "No Home"})
              </option>
            ))}
          </select>
        )}
      </FormGroup>

      <div className={styles.actions}>
        <Button
          variant="primary"
          onClick={onAssign}
          disabled={isAssigning || !selectedGatewayId}
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
