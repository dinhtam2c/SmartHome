import type { LocationListElement } from "../locations.types";

import { Cell } from "@/components/Cell";
import { CellGrid } from "@/components/CellGrid";

interface Props {
  locations: LocationListElement[];
  onClick: (id: string) => void;
}

export function LocationList({ locations, onClick }: Props) {
  return (
    <CellGrid>
      {locations.map((loc) => (
        <Cell
          key={loc.id}
          id={loc.id}
          title={loc.name}
          subtitle={loc.description}
          onClick={onClick}
          disabled={false}
        />
      ))}
    </CellGrid>
  );
}
