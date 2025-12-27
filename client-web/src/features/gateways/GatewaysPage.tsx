import { useMemo, useState } from "react";
import { useGateways } from "./hooks/useGateways";

import { GatewayList } from "./components/GatewayList";
import { GatewayDetails } from "./components/GatewayDetails";

import { Modal } from "@/components/Modal/Modal";
import { Button } from "@/components/Button";
import { PageHeader } from "@/components/PageHeader";

export function GatewaysPage() {
  const { gateways, loading, reloading, error, reload } = useGateways();
  const [selectedGatewayId, setSelectedGatewayId] = useState<string | null>(
    null
  );
  const [filterUnassigned, setFilterUnassigned] = useState(false);

  const filteredGateways = useMemo(() => {
    if (!filterUnassigned) return gateways;
    return gateways.filter((g) => g.homeName === null);
  }, [gateways, filterUnassigned]);

  const selectedGatewaySummary = useMemo(
    () => gateways.find((g) => g.id === selectedGatewayId) || null,
    [gateways, selectedGatewayId]
  );

  function handleAssignSuccess() {
    setSelectedGatewayId(null);
    reload();
  }

  if (loading) return <div>Loading...</div>;
  if (error) return <div>Error: {error}</div>;

  return (
    <>
      <PageHeader
        title={`Gateways ${reloading ? "(Reloading...)" : ""}`}
        action={
          <Button
            variant={filterUnassigned ? "primary" : "secondary"}
            onClick={() => setFilterUnassigned(!filterUnassigned)}
          >
            {filterUnassigned ? "Show All" : "Show Unassigned"}
          </Button>
        }
      />

      <GatewayList gateways={filteredGateways} onClick={setSelectedGatewayId} />

      <Modal
        open={!!selectedGatewayId}
        title={selectedGatewaySummary?.name || "Gateway Details"}
        onClose={() => setSelectedGatewayId(null)}
      >
        <GatewayDetails
          id={selectedGatewayId}
          onAssignSuccess={handleAssignSuccess}
        />
      </Modal>
    </>
  );
}
