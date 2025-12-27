import { useState } from "react";
import { useGatewayDetails } from "../hooks/useGatewayDetails";
import { getHomes } from "@/features/homes/homes.api";
import type { HomeListElement } from "@/features/homes/homes.types";

import type { GatewayHomeAssignRequest } from "../gateways.types";

import { Button } from "@/components/Button";
import { DetailsView } from "@/components/DetailsView/DetailsView";
import { DetailRow } from "@/components/DetailRow/DetailRow";
import { FormGroup } from "@/components/FormGroup";
import { timestampToDateTime } from "@/utils/dateTimeUtils";

import styles from "./GatewayDetails.module.css";

interface Props {
  id: string | null;
  onAssignSuccess: () => void;
}

export function GatewayDetails({ id, onAssignSuccess }: Props) {
  const { gateway, loading, error, isAssigning, assignHome } =
    useGatewayDetails(id);

  const [homes, setHomes] = useState<HomeListElement[]>([]);
  const [homesLoading, setHomesLoading] = useState(false);
  const [homesError, setHomesError] = useState("");

  const [isAssigningMode, setIsAssigningMode] = useState(false);
  const [selectedHomeId, setSelectedHomeId] = useState<string>("");

  async function handleAssign() {
    if (!gateway || !selectedHomeId) return;

    const request: GatewayHomeAssignRequest = {
      homeId: selectedHomeId,
    };

    await assignHome(request);
    onAssignSuccess();
    setIsAssigningMode(false);
  }

  function handleCancelAssign() {
    setIsAssigningMode(false);
    setSelectedHomeId("");
  }

  async function handleStartAssign() {
    setIsAssigningMode(true);
    setSelectedHomeId(gateway?.homeId || "");

    if (homes.length === 0 && !homesLoading) {
      setHomesLoading(true);
      try {
        const data = await getHomes();
        setHomes(data);
      } catch (err: any) {
        setHomesError(err.message);
      } finally {
        setHomesLoading(false);
      }
    }
  }

  if (loading) return <div>Loading...</div>;
  if (error) return <div>Error: {error}</div>;
  if (!gateway) return <div>Gateway not found</div>;

  if (!isAssigningMode) {
    return (
      <div>
        <DetailsView
          actions={
            <>
              <Button
                variant="primary"
                onClick={handleStartAssign}
              >
                Assign Home
              </Button>
            </>
          }
        >
          <DetailRow label="Name">{gateway.name}</DetailRow>
          <DetailRow label="Home">
            {gateway.homeName || "No home assigned"}
          </DetailRow>
          <DetailRow label="MAC">{gateway.mac}</DetailRow>
          <DetailRow label="Manufacturer">
            {gateway.manufacturer || "N/A"}
          </DetailRow>
          <DetailRow label="Model">{gateway.model || "N/A"}</DetailRow>
          <DetailRow label="Firmware Version">
            {gateway.firmwareVersion}
          </DetailRow>
          <DetailRow label="Created at">
            {timestampToDateTime(gateway.createdAt)}
          </DetailRow>
          <DetailRow label="Updated at">
            {timestampToDateTime(gateway.updatedAt)}
          </DetailRow>
        </DetailsView>
      </div>
    );
  } else {
    return (
      <div>
        <FormGroup label="Select Home" htmlFor="home-select">
          {homesLoading ? (
            <div>Loading homes...</div>
          ) : homesError ? (
            <div>Error loading homes: {homesError}</div>
          ) : (
            <select
              id="home-select"
              value={selectedHomeId}
              onChange={(e) => setSelectedHomeId(e.target.value)}
              disabled={isAssigning}
              className={styles.select}
            >
              <option value="">Select a home</option>
              {homes.map((home) => (
                <option key={home.id} value={home.id}>
                  {home.name}
                </option>
              ))}
            </select>
          )}
        </FormGroup>

        <div className={styles.actions}>
          <Button
            variant="primary"
            onClick={handleAssign}
            disabled={isAssigning || !selectedHomeId}
          >
            {isAssigning ? "Assigning..." : "Assign"}
          </Button>
          <Button
            variant="secondary"
            onClick={handleCancelAssign}
            disabled={isAssigning}
          >
            Cancel
          </Button>
        </div>
      </div>
    );
  }
}
