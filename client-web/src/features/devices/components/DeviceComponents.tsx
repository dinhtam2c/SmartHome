import type { SensorDetail, ActuatorDetail } from "../devices.types";
import styles from "./DeviceDetails.module.css";

interface SensorsListProps {
  sensors: SensorDetail[];
}

export function SensorsList({ sensors }: SensorsListProps) {
  if (!sensors || sensors.length === 0) return null;

  return (
    <div className={styles.section}>
      <h4>Sensors</h4>
      <div className={styles.grid}>
        {sensors.map((s) => (
          <div key={s.id} className={styles.card}>
            <div className={styles.cardTitle}>{s.name}</div>
            <div className={styles.cardSubtitle}>Type: {s.type}</div>
            <div className={styles.cardDetail}>Unit: {s.unit}</div>
            <div className={styles.cardDetail}>
              Range: {s.min} to {s.max}
            </div>
          </div>
        ))}
      </div>
    </div>
  );
}

interface ActuatorsListProps {
  actuators: ActuatorDetail[];
}

export function ActuatorsList({ actuators }: ActuatorsListProps) {
  if (!actuators || actuators.length === 0) return null;

  return (
    <div className={styles.section}>
      <h4>Actuators</h4>
      <div className={styles.grid}>
        {actuators.map((a) => (
          <div key={a.id} className={styles.card}>
            <div className={styles.cardTitle}>{a.name}</div>
            <div className={styles.cardSubtitle}>Type: {a.type}</div>
          </div>
        ))}
      </div>
    </div>
  );
}
