import { useState } from "react";
import { useDeviceDetails } from "../hooks/useDeviceDetails";
import { useLocations } from "@/features/locations";
import { getGatewayDetails, getGateways } from "@/features/gateways/gateways.api";
import type { GatewayListElement } from "@/features/gateways/gateways.types";

import type {
  DeviceGatewayAssignRequest,
  DeviceLocationAssignRequest,
} from "../devices.types";

import { Button } from "@/components/Button";
import { DetailsView } from "@/components/DetailsView/DetailsView";
import { DetailRow } from "@/components/DetailRow/DetailRow";
import { timestampToDateTime } from "@/utils/dateTimeUtils";

import { AssignGatewayForm } from "./AssignGatewayForm";
import { AssignLocationForm } from "./AssignLocationForm";
import { EditableNameRow } from "./EditableNameRow";
import { SensorsList, ActuatorsList } from "./DeviceComponents";

interface Props {
  id: string | null;
  onAssignSuccess: () => void;
  onDelete: () => void;
}

export function DeviceDetails({ id, onAssignSuccess, onDelete }: Props) {
  const {
    device,
    loading,
    error,
    isAssigningGateway,
    isAssigningLocation,
    isUpdating,
    assignGateway,
    assignLocation,
    updateDevice,
  } = useDeviceDetails(id);

  const [gateways, setGateways] = useState<GatewayListElement[]>([]);
  const [gatewaysLoading, setGatewaysLoading] = useState(false);
  const [gatewaysError, setGatewaysError] = useState("");

  const [isAssigningGatewayMode, setIsAssigningGatewayMode] = useState(false);
  const [selectedGatewayId, setSelectedGatewayId] = useState<string>("");

  const [isAssigningLocationMode, setIsAssigningLocationMode] = useState(false);
  const [selectedHomeId, setSelectedHomeId] = useState<string>("");
  const [gatewayHomeName, setGatewayHomeName] = useState<string>("");
  const [selectedLocationId, setSelectedLocationId] = useState<string>("");

  const [isEditingName, setIsEditingName] = useState(false);
  const [nameEditValue, setNameEditValue] = useState("");

  const {
    locations,
    loading: locationsLoading,
    error: locationsError,
  } = useLocations(selectedHomeId || null);

  async function handleAssignGateway() {
    if (!device || !selectedGatewayId) return;

    const request: DeviceGatewayAssignRequest = {
      gatewayId: selectedGatewayId,
    };

    await assignGateway(request);
    onAssignSuccess();
    setIsAssigningGatewayMode(false);
  }

  function handleCancelAssignGateway() {
    setIsAssigningGatewayMode(false);
    setSelectedGatewayId("");
  }

  async function handleStartAssignGateway() {
    setIsAssigningGatewayMode(true);
    setSelectedGatewayId(device?.gatewayId || "");

    if (gateways.length === 0 && !gatewaysLoading) {
      setGatewaysLoading(true);
      try {
        const data = await getGateways();
        setGateways(data);
      } catch (err: any) {
        setGatewaysError(err.message);
      } finally {
        setGatewaysLoading(false);
      }
    }
  }

  function handleStartEditName() {
    setIsEditingName(true);
    setNameEditValue(device?.name || "");
  }

  async function handleSaveName() {
    if (!device) return;
    await updateDevice({ name: nameEditValue });
    setIsEditingName(false);
  }

  function handleCancelEditName() {
    setIsEditingName(false);
  }

  function resetLocationAssignmentState() {
    setIsAssigningLocationMode(false);
    setSelectedHomeId("");
    setGatewayHomeName("");
    setSelectedLocationId("");
  }

  async function handleStartAssignLocation() {
    setIsAssigningLocationMode(true);

    // Fetch gateway details to get home info for location filtering
    if (device?.gatewayId) {
      try {
        const gatewayDetails = await getGatewayDetails(device.gatewayId);
        setSelectedHomeId(gatewayDetails.homeId || "");
        setGatewayHomeName(gatewayDetails.homeName || "Unknown Home");

        if (device?.locationId) {
          setSelectedLocationId(device.locationId);
        } else {
          setSelectedLocationId("");
        }
      } catch (err) {
        alert("Failed to fetch gateway details");
        resetLocationAssignmentState();
      }
    } else {
      // If no gateway, still show the form but server will reject
      setSelectedHomeId("");
      setGatewayHomeName("");
      setSelectedLocationId("");
    }
  }

  async function handleAssignLocation() {
    if (!device || !selectedLocationId) return;

    const request: DeviceLocationAssignRequest = {
      locationId: selectedLocationId,
    };

    await assignLocation(request);
    onAssignSuccess();
    resetLocationAssignmentState();
  }

  function handleCancelAssignLocation() {
    resetLocationAssignmentState();
  }

  // Early returns for loading/error states
  if (loading) return <div>Loading...</div>;
  if (error) return <div>Error: {error}</div>;
  if (!device) return <div>Device not found</div>;

  // Location assignment mode
  if (isAssigningLocationMode) {
    return (
      <AssignLocationForm
        homeName={gatewayHomeName}
        locations={locations}
        selectedLocationId={selectedLocationId}
        isAssigning={isAssigningLocation}
        isLoading={locationsLoading}
        error={locationsError}
        onLocationChange={setSelectedLocationId}
        onAssign={handleAssignLocation}
        onCancel={handleCancelAssignLocation}
      />
    );
  }

  // Gateway assignment mode
  if (isAssigningGatewayMode) {
    return (
      <AssignGatewayForm
        gateways={gateways}
        selectedGatewayId={selectedGatewayId}
        isAssigning={isAssigningGateway}
        isLoading={gatewaysLoading}
        error={gatewaysError}
        onGatewayChange={setSelectedGatewayId}
        onAssign={handleAssignGateway}
        onCancel={handleCancelAssignGateway}
      />
    );
  }

  return (
    <div>
      <DetailsView
        actions={
          <>
            <Button
              variant="primary"
              onClick={handleStartAssignGateway}
            >
              Assign Gateway
            </Button>
            <Button
              variant="primary"
              onClick={handleStartAssignLocation}
              disabled={!device?.gatewayId}
            >
              Assign Location
            </Button>
            <Button variant="danger" onClick={onDelete}>
              Delete Device
            </Button>
          </>
        }
      >
        <EditableNameRow
          name={device.name}
          isEditing={isEditingName}
          editValue={nameEditValue}
          isUpdating={isUpdating}
          onStartEdit={handleStartEditName}
          onSave={handleSaveName}
          onCancel={handleCancelEditName}
          onChange={setNameEditValue}
        />
        <DetailRow label="Identifier">{device.identifier}</DetailRow>
        <DetailRow label="Gateway">
          {device.gatewayName || "No gateway assigned"}
        </DetailRow>
        <DetailRow label="Location">
          {device.locationName || "No location assigned"}
        </DetailRow>
        <DetailRow label="Manufacturer">
          {device.manufacturer || "N/A"}
        </DetailRow>
        <DetailRow label="Model">{device.model || "N/A"}</DetailRow>
        <DetailRow label="Firmware Version">
          {device.firmwareVersion || "N/A"}
        </DetailRow>
        <DetailRow label="Created at">
          {timestampToDateTime(device.createdAt)}
        </DetailRow>
        <DetailRow label="Updated at">
          {timestampToDateTime(device.updatedAt)}
        </DetailRow>
      </DetailsView>

      <SensorsList sensors={device.sensors || []} />
      <ActuatorsList actuators={device.actuators || []} />
    </div>
  );
}
