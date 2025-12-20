interface Props {
  id: string;
  title: string;
  subtitle?: string;
  onClick: (id: string) => void;
  disabled: boolean;
}

export default function Cell({ id, title, subtitle, onClick, disabled = false }: Props) {
  return (
    <button className="cell" disabled={disabled} onClick={() => onClick(id)}>
      <div className="cell-title">{title}</div>
      {subtitle && <div className="cell-description">{subtitle}</div>}
    </button>
  );
}
