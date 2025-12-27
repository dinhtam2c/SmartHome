import { useState } from "react";
import type { DeviceAddRequest } from "../devices.types";
import { useGateways } from "@/features/gateways";
import { Button } from "@/components/Button";
import { FormGroup } from "@/components/FormGroup";
import { Input } from "@/components/Input";
import { Form } from "@/components/Form";
import { FormActions } from "@/components/FormActions";

import styles from "./AddDeviceForm.module.css";

interface Props {
  onSubmit: (data: DeviceAddRequest) => Promise<void>;
  onCancel: () => void;
  isLoading: boolean;
}

export function AddDeviceForm({ onSubmit, onCancel, isLoading }: Props) {
  const { gateways, loading: gatewaysLoading } = useGateways();
  const [name, setName] = useState("");
  const [identifier, setIdentifier] = useState("");
  const [gatewayId, setGatewayId] = useState("");

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    await onSubmit({
      name,
      identifier,
      gatewayId: gatewayId || undefined,
    });
  };

  return (
    <Form onSubmit={handleSubmit}>
      <FormGroup label="Name" htmlFor="name">
        <Input
          id="name"
          value={name}
          onChange={(e) => setName(e.target.value)}
          placeholder="Device Name"
          required
        />
      </FormGroup>

      <FormGroup label="Identifier" htmlFor="identifier">
        <Input
          id="identifier"
          value={identifier}
          onChange={(e) => setIdentifier(e.target.value)}
          placeholder="Unique Identifier (e.g. MAC address)"
          required
        />
      </FormGroup>

      <FormGroup
        label="Gateway"
        htmlFor="gateway"
        required={false}
      >
        {gatewaysLoading ? (
          <div>Loading gateways...</div>
        ) : (
          <select
            id="gateway"
            value={gatewayId}
            onChange={(e) => setGatewayId(e.target.value)}
            className={styles.select}
          >
            <option value="">No Gateway</option>
            {gateways.map((g) => (
              <option key={g.id} value={g.id}>
                {g.name || "Unnamed"} ({g.homeName || "No Home"})
              </option>
            ))}
          </select>
        )}
      </FormGroup>

      <FormActions>
        <Button type="button" variant="secondary" onClick={onCancel}>
          Cancel
        </Button>
        <Button type="submit" variant="primary" disabled={isLoading}>
          {isLoading ? "Adding..." : "Add Device"}
        </Button>
      </FormActions>
    </Form>
  );
}
