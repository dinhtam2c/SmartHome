import type { HomeListElement } from "../homes.types";

import { Cell } from "@/components/Cell";
import { CellGrid } from "@/components/CellGrid";

interface Props {
  homes: HomeListElement[];
  onClick: (id: string) => void;
}

export function HomeList({ homes, onClick }: Props) {
  return (
    <CellGrid>
      {homes.map((h) => (
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
