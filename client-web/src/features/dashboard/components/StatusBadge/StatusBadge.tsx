import styles from "./StatusBadge.module.css";

interface Props {
  isOnline: boolean;
}

export function StatusBadge({ isOnline }: Props) {
  return (
    <span className={isOnline ? styles.online : styles.offline}>
      {isOnline ? "● Online" : "● Offline"}
    </span>
  );
}
