import { useState } from "react";
import { useDevices } from "./hooks/useDevices";

import { DeviceList } from "./components/DeviceList";
import { DeviceDetails } from "./components/DeviceDetails";
import { AddDeviceForm } from "./components/AddDeviceForm";

import { Modal } from "@/components/Modal/Modal";
import { Button } from "@/components/Button";
import { PageHeader } from "@/components/PageHeader";
import type { DeviceAddRequest } from "./devices.types";

export function DevicesPage() {
  const {
    devices,
    loading,
    reloading,
    error,
    reload,
    addDevice,
    deleteDevice,
  } = useDevices();
  const [selectedDeviceId, setSelectedDeviceId] = useState<string | null>(null);
  const [isAddModalOpen, setIsAddModalOpen] = useState(false);
  const [isAdding, setIsAdding] = useState(false);

  const selectedDeviceSummary = devices.find((d) => d.id === selectedDeviceId);

  async function handleAddDevice(data: DeviceAddRequest) {
    setIsAdding(true);
    try {
      await addDevice(data);
      setIsAddModalOpen(false);
      reload();
    } catch (e) {
      console.error("Failed to add device", e);
    } finally {
      setIsAdding(false);
    }
  }

  async function handleDeleteDevice() {
    if (!selectedDeviceId) return;
    if (!confirm("Are you sure you want to delete this device?")) return;

    try {
      await deleteDevice(selectedDeviceId);
      setSelectedDeviceId(null);
      reload();
    } catch (e) {
      console.error("Failed to delete device", e);
      alert("Failed to delete device");
    }
  }

  function handleAssignSuccess() {
    setSelectedDeviceId(null);
    reload();
  }

  if (loading) return <div>Loading...</div>;
  if (error) return <div>Error: {error}</div>;

  return (
    <>
      <PageHeader
        title={`Devices ${reloading ? "(Reloading...)" : ""}`}
        action={
          <Button variant="primary" onClick={() => setIsAddModalOpen(true)}>
            Add Device
          </Button>
        }
      />

      <DeviceList devices={devices} onClick={setSelectedDeviceId} />

      {/* Details Modal */}
      <Modal
        open={!!selectedDeviceId}
        title={selectedDeviceSummary?.name || "Device Details"}
        onClose={() => setSelectedDeviceId(null)}
      >
        <DeviceDetails
          id={selectedDeviceId}
          onAssignSuccess={handleAssignSuccess}
          onDelete={handleDeleteDevice}
        />
      </Modal>

      {/* Add Device Modal */}
      <Modal
        open={isAddModalOpen}
        title="Add New Device"
        onClose={() => setIsAddModalOpen(false)}
      >
        <AddDeviceForm
          onSubmit={handleAddDevice}
          onCancel={() => setIsAddModalOpen(false)}
          isLoading={isAdding}
        />
      </Modal>
    </>
  );
}
