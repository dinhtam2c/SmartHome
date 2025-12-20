import Cell from "../../../components/Cell";
import CellGrid from "../../../components/CellGrid";
import type { HomeListElement } from "../home.types";

interface Props {
  homes: HomeListElement[];
  onClick: (id: string) => void;
}

export default function HomeList({ homes, onClick }: Props) {
  return (
    <CellGrid>
      {homes.map(h => (
        <Cell
          key={h.id}
          id={h.id}
          title={h.name}
          subtitle={h.description}
          onClick={onClick}
          disabled={false}
        />
      ))}
    </CellGrid>
  );
}
