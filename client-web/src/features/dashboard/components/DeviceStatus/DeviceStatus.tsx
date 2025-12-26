import styles from "../StatusBadge/StatusBadge.module.css";

interface Props {
  onlineCount: number;
  totalCount: number;
}

export function DeviceStatus({ onlineCount, totalCount }: Props) {
  const hasOnlineDevices = onlineCount > 0;

  return (
    <span className={hasOnlineDevices ? styles.online : styles.offline}>
      {onlineCount} / {totalCount}
    </span>
  );
}
