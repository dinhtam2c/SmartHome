import { useMemo, useState } from "react";
import { useHomes } from "./hooks/useHomes";

import type { HomeAddRequest } from "./home.types";

import { HomeList } from "./components/HomeList";
import { AddHomeForm } from "./components/AddHomeForm";
import { HomeDetails } from "./components/HomeDetails";

import { Modal } from "@/components/Modal/Modal";
import { Button } from "@/components/Button";
import { PageHeader } from "@/components/PageHeader";

export default function HomePage() {
  const { homes, loading, reloading, isAdding, error, addHome, reload } =
    useHomes();
  const [isAddModalOpen, setIsAddModalOpen] = useState(false);
  const [selectedHomeId, setSelectedHomeId] = useState<string | null>(null);

  const selectedHomeSummary = useMemo(
    () => homes.find((h) => h.id === selectedHomeId) || null,
    [homes, selectedHomeId]
  );

  async function handleAdd(request: HomeAddRequest) {
    await addHome(request);
    setIsAddModalOpen(false);
    reload();
  }

  function handleDeleteSuccess() {
    setSelectedHomeId(null);
    reload();
  }

  if (loading) return <div>Loading...</div>;
  if (error) return <div>Error: {error}</div>;

  return (
    <>
      <PageHeader
        title={`Homes ${reloading ? "(Reloading...)" : ""}`}
        action={
          <Button variant="primary" onClick={() => setIsAddModalOpen(true)}>
            Add Home
          </Button>
        }
      />

      <HomeList homes={homes} onClick={setSelectedHomeId} />

      <Modal
        open={!!selectedHomeId}
        title={selectedHomeSummary?.name || "Home Details"}
        onClose={() => setSelectedHomeId(null)}
      >
        <HomeDetails
          id={selectedHomeId}
          onUpdateSuccess={reload}
          onDeleteSuccess={handleDeleteSuccess}
        />
      </Modal>

      <Modal
        open={isAddModalOpen}
        title="Add Home"
        onClose={() => setIsAddModalOpen(false)}
      >
        <AddHomeForm
          onAdd={handleAdd}
          onCancel={() => setIsAddModalOpen(false)}
          isSubmitting={isAdding}
        />
      </Modal>
    </>
  );
}
