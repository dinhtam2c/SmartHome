import type { ReactNode } from "react";

interface Props {
  children: ReactNode;
  actions?: ReactNode;
}

export function DetailsView({ children, actions }: Props) {
  return (
    <div className="details-container">
      <div className="details-grid">
        {children}
      </div>

      {actions && (
        <div className="details-actions">
          {actions}
        </div>
      )}
    </div>
  );
}
