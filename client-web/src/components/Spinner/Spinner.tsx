import styles from "./Spinner.module.css";

interface Props {
  size?: "sm" | "md" | "lg";
}

export function Spinner({ size = "md" }: Props) {
  const classes = [styles.spinner, styles[`spinner--${size}`]].join(" ");
  return <div className={classes} />;
}
