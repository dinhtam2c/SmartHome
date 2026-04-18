import styles from "./Form.module.css";

type Props = React.PropsWithChildren<React.FormHTMLAttributes<HTMLFormElement>>;

export function Form({ children, ...rest }: Props) {
  return (
    <form className={styles.form} {...rest}>
      {children}
    </form>
  );
}
