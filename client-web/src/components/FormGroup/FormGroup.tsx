import styles from "./FormGroup.module.css";

type Props = React.PropsWithChildren<{
  label: string;
  htmlFor: string;
  required?: boolean;
}>;

export function FormGroup({
  label,
  htmlFor,
  children,
  required = true,
}: Props) {
  return (
    <div className={styles.formGroup}>
      <label htmlFor={htmlFor} className={styles.label}>
        {label}
        {required && <span className={styles.required}> *</span>}
      </label>
      {children}
    </div>
  );
}
