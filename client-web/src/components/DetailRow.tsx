import type { ReactNode } from "react";

interface Props {
  label: string;
  children: ReactNode;
}

export function DetailRow({ label, children }: Props) {
  return (
    <>
      <div className="detail-label">{label}</div>
      <div className="detail-value">{children}</div>
    </>
  );
}
