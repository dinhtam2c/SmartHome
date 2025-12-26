import { useState } from "react";
import { Modal } from "@/components/Modal";
import { DetailsView } from "@/components/DetailsView";
import { DetailRow } from "@/components/DetailRow";
import { Button } from "@/components/Button";
import { StatusBadge } from "../StatusBadge";
import { useDeviceDashboard } from "../../hooks/useDeviceDashboard";
import { sendDeviceCommand } from "../../dashboard.api";
import { timestampToDateTime } from "@/utils/dateTimeUtils";
import styles from "./DeviceModal.module.css";

interface Props {
  deviceId: string | null;
  onClose: () => void;
}

export function DeviceModal({ deviceId, onClose }: Props) {
  const { device, isLoading, error, refetch } = useDeviceDashboard(deviceId);
  const [isSendingCommand, setIsSendingCommand] = useState(false);
  const [commandError, setCommandError] = useState<string | null>(null);

  const handleSendCommand = async (actuatorId: string, command: string) => {
    if (!deviceId) return;

    setIsSendingCommand(true);
    setCommandError(null);
    try {
      await sendDeviceCommand(deviceId, {
        actuatorId,
        command,
        parameters: null,
      });
      await new Promise((resolve) => setTimeout(resolve, 100));
      await refetch();
    } catch (err) {
      setCommandError((err as Error).message);
    } finally {
      setIsSendingCommand(false);
    }
  };

  if (!deviceId) return null;

  return (
    <Modal open={!!deviceId} title={device?.name || "Device"} onClose={onClose}>
      {isLoading && <p>Loading...</p>}
      {error && <p className={styles.error}>Error: {error}</p>}

      {device && (
        <div className={styles.content}>
          <DetailsView>
            <DetailRow label="Status">
              <StatusBadge isOnline={device.isOnline} />
            </DetailRow>
            <DetailRow label="Uptime">{device.upTime}s</DetailRow>
            <DetailRow label="Last Seen">
              {timestampToDateTime(device.lastSeenAt)}
            </DetailRow>
          </DetailsView>

          {/* Sensor Data Section */}
          {device.latestSensorData && device.latestSensorData.length > 0 && (
            <div className={styles.section}>
              <h4 className={styles.sectionTitle}>Sensor Data</h4>
              <div className={styles.sensorGrid}>
                {device.latestSensorData.map((sensor) => (
                  <div key={sensor.sensorId} className={styles.sensorCard}>
                    <div className={styles.sensorName}>{sensor.sensorName}</div>
                    <div className={styles.sensorValue}>
                      {sensor.value} {sensor.unit}
                    </div>
                    <div className={styles.sensorTime}>
                      {timestampToDateTime(sensor.timestamp)}
                    </div>
                  </div>
                ))}
              </div>
            </div>
          )}

          {/* Actuator Controls Section */}
          {device.actuatorStates && device.actuatorStates.length > 0 && (
            <div className={styles.section}>
              <h4 className={styles.sectionTitle}>Actuator Controls</h4>
              {commandError && <p className={styles.error}>{commandError}</p>}
              <div className={styles.actuatorGrid}>
                {device.actuatorStates.map((actuator) => (
                  <div
                    key={actuator.actuatorId}
                    className={styles.actuatorCard}
                  >
                    <div className={styles.actuatorName}>
                      {actuator.actuatorName}
                    </div>
                    <div className={styles.actuatorStates}>
                      {actuator.states &&
                        Object.entries(actuator.states).map(([key, value]) => (
                          <div key={key} className={styles.stateItem}>
                            <span className={styles.stateKey}>{key}:</span>
                            <span className={styles.stateValue}>{value}</span>
                          </div>
                        ))}
                    </div>
                    <div className={styles.actuatorButtons}>
                      <Button
                        variant="primary"
                        size="sm"
                        onClick={() =>
                          handleSendCommand(actuator.actuatorId, "TurnOn")
                        }
                        disabled={isSendingCommand || !device.isOnline}
                      >
                        Turn On
                      </Button>
                      <Button
                        variant="secondary"
                        size="sm"
                        onClick={() =>
                          handleSendCommand(actuator.actuatorId, "TurnOff")
                        }
                        disabled={isSendingCommand || !device.isOnline}
                      >
                        Turn Off
                      </Button>
                    </div>
                  </div>
                ))}
              </div>
            </div>
          )}
        </div>
      )}
    </Modal>
  );
}
