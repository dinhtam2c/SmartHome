import type { GatewayListElement } from "../gateways.types";

import { Cell } from "@/components/Cell";
import { CellGrid } from "@/components/CellGrid";

interface Props {
  gateways: GatewayListElement[];
  onClick: (id: string) => void;
}

export function GatewayList({ gateways, onClick }: Props) {
  return (
    <CellGrid>
      {gateways.map((g) => (
        <Cell
          key={g.id}
          id={g.id}
          title={g.name || "Unnamed Gateway"}
          subtitle={g.homeName || "No home assigned"}
          onClick={onClick}
          disabled={false}
        />
      ))}
    </CellGrid>
  );
}
