import { StatusBadge } from "../StatusBadge";
import styles from "./DeviceSubtitle.module.css";
import type {
  DashboardSensorDataDto,
  DashboardActuatorStateDto,
} from "../../dashboard.types";

interface Props {
  isOnline: boolean;
  latestSensorData: DashboardSensorDataDto[];
  actuatorStates: DashboardActuatorStateDto[];
}

export function DeviceSubtitle({
  isOnline,
  latestSensorData,
  actuatorStates,
}: Props) {
  return (
    <div className={styles.subtitle}>
      <div className={styles.status}>
        <StatusBadge isOnline={isOnline} />
      </div>

      {latestSensorData.length > 0 && (
        <div className={styles.sensorData}>
          {latestSensorData.slice(0, 2).map((sensor) => (
            <div key={sensor.sensorId} className={styles.sensorItem}>
              {sensor.sensorName}: {sensor.value} {sensor.unit}
            </div>
          ))}
        </div>
      )}

      {actuatorStates.length > 0 && (
        <div className={styles.actuatorStates}>
          {actuatorStates.slice(0, 2).map((actuator) => (
            <div key={actuator.actuatorId} className={styles.actuatorItem}>
              {actuator.actuatorName}:{" "}
              {actuator.states
                ? Object.values(actuator.states).join(", ")
                : "N/A"}
            </div>
          ))}
        </div>
      )}
    </div>
  );
}
