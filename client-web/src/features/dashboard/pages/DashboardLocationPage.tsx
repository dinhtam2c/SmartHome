import { useState } from "react";
import { useParams, useNavigate } from "react-router-dom";
import { useLocationDashboard } from "../hooks/useLocationDashboard";
import { PageHeader } from "@/components/PageHeader";
import { Button } from "@/components/Button";
import { DetailsView } from "@/components/DetailsView";
import { DetailRow } from "@/components/DetailRow";
import { CellGrid } from "@/components/CellGrid";
import { Cell } from "@/components/Cell";
import { DeviceStatus } from "../components/DeviceStatus";
import { DeviceSubtitle } from "../components/DeviceSubtitle";
import { DeviceModal } from "../components/DeviceModal";

export function DashboardLocationPage() {
  const { locationId } = useParams();
  const navigate = useNavigate();
  const [selectedDeviceId, setSelectedDeviceId] = useState<string | null>(null);

  if (!locationId) return <p>Location ID not found</p>;

  const { location, summary, devices, isLoading, error } =
    useLocationDashboard(locationId);

  if (isLoading) return <p>Loading...</p>;
  if (error) return <p>Error: {error}</p>;
  if (!location) return <p>Location not found</p>;

  return (
    <>
      <Button variant="secondary" size="sm" onClick={() => navigate(-1)}>
        ← Back
      </Button>

      <PageHeader title={location.name} />

      <div>
        <DetailsView>
          {location.description && (
            <DetailRow label="Description">{location.description}</DetailRow>
          )}
          <DetailRow label="Total Devices">
            {summary?.deviceCount || 0}
          </DetailRow>
          <DetailRow label="Online Devices">
            <DeviceStatus
              onlineCount={summary?.onlineDeviceCount || 0}
              totalCount={summary?.deviceCount || 0}
            />
          </DetailRow>
        </DetailsView>
      </div>

      <div>
        <h3>Devices</h3>

        {devices.length === 0 ? (
          <p>No devices found</p>
        ) : (
          <CellGrid>
            {devices.map((device) => (
              <Cell
                key={device.id}
                id={device.id}
                title={device.name}
                subtitle={
                  <DeviceSubtitle
                    isOnline={device.isOnline}
                    latestSensorData={device.latestSensorData}
                    actuatorStates={device.actuatorStates}
                  />
                }
                onClick={(id) => setSelectedDeviceId(id)}
                disabled={false}
              />
            ))}
          </CellGrid>
        )}
      </div>

      <DeviceModal
        deviceId={selectedDeviceId}
        onClose={() => setSelectedDeviceId(null)}
      />
    </>
  );
}
