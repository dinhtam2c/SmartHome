import { DeviceStatus } from "../DeviceStatus";
import styles from "./LocationSubtitle.module.css";

interface Props {
  description?: string;
  onlineDeviceCount: number;
  deviceCount: number;
}

export function LocationSubtitle({
  description,
  onlineDeviceCount,
  deviceCount,
}: Props) {
  return (
    <div className={styles.subtitle}>
      {description && <div>{description}</div>}
      <div className={styles.status}>
        <DeviceStatus
          onlineCount={onlineDeviceCount}
          totalCount={deviceCount}
        />
      </div>
    </div>
  );
}
