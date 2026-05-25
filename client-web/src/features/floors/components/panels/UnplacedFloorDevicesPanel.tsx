import { useTranslation } from "react-i18next";
import {
  DEVICE_CATEGORY_ICON_PATHS,
  DeviceCategoryBadge,
  getDeviceCategoryLabel,
  resolveDeviceCategory,
  useDeviceCategoryRegistry,
  type DeviceCategoryDefinition,
} from "@/features/device-categories";
import type { SelectableDeviceDto } from "@/features/capabilities";
import { FLOOR_DEVICE_DRAG_TYPE } from "../../services/floorConstants";
import styles from "./UnplacedFloorDevicesPanel.module.css";

export type FloorDeviceGroup = {
  id: string;
  title: string;
  devices: SelectableDeviceDto[];
};

type Props = {
  groups: FloorDeviceGroup[];
  selectedDeviceId: string | null;
  title: string;
  helperText: string;
  emptyText: string;
  roomFallbackLabel: string;
  onSelectDevice: (deviceId: string) => void;
};

function createDeviceDragPreview(category: DeviceCategoryDefinition) {
  const preview = document.createElement("canvas");
  preview.width = 64;
  preview.height = 64;
  const paths = DEVICE_CATEGORY_ICON_PATHS[category.iconKey] ?? DEVICE_CATEGORY_ICON_PATHS.box;
  const context = preview.getContext("2d");
  if (!context) return preview;

  context.shadowColor = "rgba(22, 49, 72, 0.2)";
  context.shadowBlur = 16;
  context.shadowOffsetY = 8;
  context.fillStyle = "#ffffff";
  context.beginPath();
  context.arc(32, 32, 30, 0, Math.PI * 2);
  context.fill();
  context.shadowColor = "transparent";
  context.fillStyle = category.color;
  context.strokeStyle = "rgba(255, 255, 255, 0.92)";
  context.lineWidth = 3;
  context.beginPath();
  context.arc(32, 32, 24, 0, Math.PI * 2);
  context.fill();
  context.stroke();
  context.save();
  context.translate(17.5, 17.5);
  context.scale(29 / 24, 29 / 24);
  context.strokeStyle = "#0e2940";
  context.fillStyle = "#0e2940";
  context.lineWidth = 1.9;
  context.lineCap = "round";
  context.lineJoin = "round";
  paths.forEach((iconPath) => {
    const path = new Path2D(iconPath.d);
    if (iconPath.fill) context.fill(path);
    context.stroke(path);
  });
  context.restore();
  return preview;
}

export function UnplacedFloorDevicesPanel({
  groups,
  selectedDeviceId,
  title,
  helperText,
  emptyText,
  roomFallbackLabel,
  onSelectDevice,
}: Props) {
  const { t } = useTranslation("deviceCategories");
  const { categories } = useDeviceCategoryRegistry();
  const deviceCount = groups.reduce((count, group) => count + group.devices.length, 0);

  return (
    <section className={styles.panel}>
      <div className={styles.header}>
        <h2 className={styles.title}>{title}</h2>
        <p className={styles.helper}>{helperText}</p>
      </div>

      {deviceCount === 0 ? (
        <div className={styles.emptyState}>{emptyText}</div>
      ) : (
        <div className={styles.groups}>
          {groups.map((group) => (
            <section key={group.id} className={styles.group}>
              <h3 className={styles.groupTitle}>
                <span>{group.title}</span>
                <span>{group.devices.length}</span>
              </h3>
              <div className={styles.list}>
                {group.devices.map((device) => {
                  const isSelected = device.id === selectedDeviceId;
                  const category = resolveDeviceCategory(categories, device.category);
                  const categoryLabel = getDeviceCategoryLabel(category, (key, fallback) =>
                    t(key, { defaultValue: fallback })
                  );

                  return (
                    <button
                      type="button"
                      key={device.id}
                      className={`${styles.item} ${isSelected ? styles.itemSelected : ""}`}
                      draggable
                      aria-pressed={isSelected}
                      onClick={() => onSelectDevice(device.id)}
                      onDragStart={(event) => {
                        event.dataTransfer.effectAllowed = "move";
                        event.dataTransfer.setData(FLOOR_DEVICE_DRAG_TYPE, device.id);
                        event.dataTransfer.setData("text/plain", device.id);
                        event.dataTransfer.setDragImage(createDeviceDragPreview(category), 32, 32);
                        onSelectDevice(device.id);
                      }}
                    >
                      <div className={styles.itemTopRow}>
                        <div className={styles.itemIdentity}>
                          <DeviceCategoryBadge
                            category={category}
                            label={categoryLabel}
                            className={styles.deviceTypeBadge}
                          />
                          <strong className={styles.deviceName}>{device.name}</strong>
                        </div>
                        <span
                          className={`${styles.statusDot} ${device.isOnline ? styles.statusDotOnline : styles.statusDotOffline}`}
                          aria-hidden="true"
                        />
                      </div>
                      <div className={styles.itemMeta}>
                        {device.roomName?.trim() || roomFallbackLabel}
                      </div>
                    </button>
                  );
                })}
              </div>
            </section>
          ))}
        </div>
      )}
    </section>
  );
}
