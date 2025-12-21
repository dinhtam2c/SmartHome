import styles from "./FormGroup.module.css";

type Props = React.PropsWithChildren<{
  label: string;
  htmlFor: string;
}>;

export function FormGroup({ label, htmlFor, children }: Props) {
  return (
    <div className={styles.formGroup}>
      <label htmlFor={htmlFor} className={styles.label}>
        {label}
      </label>
      {children}
    </div>
  );
}
