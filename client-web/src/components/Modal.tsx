import type { ReactNode } from "react";

interface Props {
  open: boolean;
  title: string;
  onClose: () => void;
  children: ReactNode;
}

export default function Modal({ open, title, onClose, children }: Props) {
  if (!open) return null;

  return (
    <div className="modal-overlay" onClick={onClose}>
      <div className="modal" onClick={e => e.stopPropagation()}>
        <div className="modal__header">
          <strong>{title}</strong>
          <button className="modal__close" onClick={onClose} aria-label="Close">x</button>
        </div>

        <div className="modal__body">{children}</div>
      </div>
    </div>
  );
}
