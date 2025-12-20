import type { ReactNode } from "react";

interface Props {
  children: ReactNode;
}

export default function CellGrid({ children }: Props) {
  return (
    <div className="cell-grid">
      {children}
    </div>
  );
}
