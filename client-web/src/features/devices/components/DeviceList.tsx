import type { DeviceListElement } from "../devices.types";

import { Cell } from "@/components/Cell";
import { CellGrid } from "@/components/CellGrid";

import styles from "./DeviceList.module.css";

interface Props {
  devices: DeviceListElement[];
  onClick: (id: string) => void;
}

export function DeviceList({ devices, onClick }: Props) {
  return (
    <CellGrid>
      {devices.map((d) => (
        <Cell
          key={d.id}
          id={d.id}
          title={d.name || d.identifier}
          subtitle={
            <>
              <div>{d.home || "No Home"}</div>
              <div className={styles.subtitle}>
                {d.gatewayName || "No Gateway"}
              </div>
            </>
          }
          onClick={onClick}
          disabled={false}
        />
      ))}
    </CellGrid>
  );
}
