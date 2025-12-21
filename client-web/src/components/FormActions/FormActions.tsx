import styles from "./FormActions.module.css";

type Props = React.PropsWithChildren<{}>;

export function FormActions({ children }: Props) {
  return <div className={styles.formActions}>{children}</div>;
}
