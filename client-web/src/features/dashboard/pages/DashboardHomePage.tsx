import { useParams, useNavigate } from "react-router-dom";
import { useHomeDashboard } from "../hooks/useHomeDashboard";
import { PageHeader } from "@/components/PageHeader";
import { Button } from "@/components/Button";
import { DetailsView } from "@/components/DetailsView";
import { DetailRow } from "@/components/DetailRow";
import { CellGrid } from "@/components/CellGrid";
import { Cell } from "@/components/Cell";
import { DeviceStatus } from "../components/DeviceStatus";
import { LocationSubtitle } from "../components/LocationSubtitle";

export function DashboardHomePage() {
  const { homeId } = useParams();
  const navigate = useNavigate();

  if (!homeId) return <p>Home ID not found</p>;

  const { home, summary, locations, isLoading, error } =
    useHomeDashboard(homeId);

  if (isLoading) return <p>Loading...</p>;
  if (error) return <p>Error: {error}</p>;
  if (!home) return <p>Home not found</p>;

  return (
    <>
      <Button
        variant="secondary"
        size="sm"
        onClick={() => navigate("/dashboard")}
      >
        ← Back to Homes
      </Button>

      <PageHeader title={home.name} />

      <div>
        <DetailsView>
          {home.description && (
            <DetailRow label="Description">{home.description}</DetailRow>
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
        <h3>Locations</h3>

        {locations.length === 0 ? (
          <p>No locations found</p>
        ) : (
          <CellGrid>
            {locations.map((location) => (
              <Cell
                key={location.id}
                id={location.id}
                title={location.name}
                subtitle={
                  <LocationSubtitle
                    description={location.description}
                    onlineDeviceCount={location.onlineDeviceCount}
                    deviceCount={location.deviceCount}
                  />
                }
                onClick={(id) => navigate(`/dashboard/location/${id}`)}
                disabled={false}
              />
            ))}
          </CellGrid>
        )}
      </div>
    </>
  );
}
