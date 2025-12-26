import { useNavigate } from "react-router-dom";
import { useHomes } from "../hooks/useHomes";
import { PageHeader } from "@/components/PageHeader";
import { CellGrid } from "@/components/CellGrid";
import { Cell } from "@/components/Cell";

export function DashboardHomeListPage() {
  const { homes, isLoading, error } = useHomes();
  const navigate = useNavigate();

  if (isLoading) return <p>Loading...</p>;
  if (error) return <p>Error: {error.message}</p>;

  return (
    <>
      <PageHeader title="Dashboard" />
      <h2>Homes</h2>
      <CellGrid>
        {homes.map((h) => (
          <Cell
            key={h.id}
            id={h.id}
            title={h.name}
            subtitle={h.description}
            onClick={(id) => navigate(`/dashboard/home/${id}`)}
            disabled={false}
          />
        ))}
      </CellGrid>
    </>
  );
}
