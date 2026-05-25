import { useMemo, useState } from "react";
import { useTranslation } from "react-i18next";
import {
  DEVICE_CATEGORY_ICON_PATHS,
  DeviceCategoryBadge,
  getDeviceCategoryLabel,
  resolveDeviceCategory,
  useDeviceCategoryRegistry,
  type DeviceCategoryDefinition,
} from "@/features/device-categories";
import type { BuilderDeviceDto } from "@/features/capability-builder";
import { FLOOR_DEVICE_DRAG_TYPE } from "../../services/floorConstants";
import styles from "./UnplacedFloorDevicesPanel.module.css";

const ALL_ROOMS_FILTER = "__all__";
const UNASSIGNED_ROOM_FILTER = "__unassigned__";

type Props = {
  devices: BuilderDeviceDto[];
  selectedDeviceId: string | null;
  title: string;
  helperText: string;
  emptyText: string;
  roomFallbackLabel: string;
  onSelectDevice: (deviceId: string) => void;
};

type RoomFilterOption = {
  value: string;
  label: string;
  count: number;
};

function createDeviceDragPreview(category: DeviceCategoryDefinition) {
  const preview = document.createElement("canvas");
  preview.width = 64;
  preview.height = 64;

  const paths =
    DEVICE_CATEGORY_ICON_PATHS[category.iconKey] ?? DEVICE_CATEGORY_ICON_PATHS.box;
  const context = preview.getContext("2d");

  if (!context) {
    return preview;
  }

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

    if (iconPath.fill) {
      context.fill(path);
    }

    context.stroke(path);
  });

  context.restore();

  return preview;
}

function getRoomFilterValue(device: BuilderDeviceDto) {
  const roomId = device.roomId?.trim();
  return roomId ? roomId : UNASSIGNED_ROOM_FILTER;
}

function getDeviceRoomLabel(
  device: BuilderDeviceDto,
  roomFallbackLabel: string
) {
  const roomName = device.roomName?.trim();
  return roomName ? roomName : roomFallbackLabel;
}

function buildRoomFilterOptions(
  devices: BuilderDeviceDto[],
  roomFallbackLabel: string
) {
  const options = new Map<string, RoomFilterOption>();

  devices.forEach((device) => {
    const value = getRoomFilterValue(device);
    const existingOption = options.get(value);

    if (existingOption) {
      existingOption.count++;
      return;
    }

    options.set(value, {
      value,
      label: getDeviceRoomLabel(device, roomFallbackLabel),
      count: 1,
    });
  });

  return [...options.values()].sort((left, right) => {
    if (left.value === UNASSIGNED_ROOM_FILTER) {
      return 1;
    }

    if (right.value === UNASSIGNED_ROOM_FILTER) {
      return -1;
    }

    return left.label.localeCompare(right.label);
  });
}

export function UnplacedFloorDevicesPanel({
  devices,
  selectedDeviceId,
  title,
  helperText,
  emptyText,
  roomFallbackLabel,
  onSelectDevice,
}: Props) {
  const { t } = useTranslation("deviceCategories");
  const { t: tFloor } = useTranslation("floors");
  const { categories } = useDeviceCategoryRegistry();
  const [selectedRoomFilter, setSelectedRoomFilter] = useState(ALL_ROOMS_FILTER);
  const roomFilterOptions = useMemo(
    () => buildRoomFilterOptions(devices, roomFallbackLabel),
    [devices, roomFallbackLabel]
  );
  const activeRoomFilter =
    selectedRoomFilter === ALL_ROOMS_FILTER ||
    roomFilterOptions.some((option) => option.value === selectedRoomFilter)
      ? selectedRoomFilter
      : ALL_ROOMS_FILTER;
  const filteredDevices = useMemo(
    () =>
      activeRoomFilter === ALL_ROOMS_FILTER
        ? devices
        : devices.filter((device) => getRoomFilterValue(device) === activeRoomFilter),
    [activeRoomFilter, devices]
  );
  const showRoomFilter = roomFilterOptions.length > 1;

  return (
    <section className={styles.panel}>
      <div className={styles.header}>
        <h2 className={styles.title}>{title}</h2>
        <p className={styles.helper}>{helperText}</p>
      </div>

      {devices.length === 0 ? (
        <div className={styles.emptyState}>{emptyText}</div>
      ) : (
        <>
          {showRoomFilter ? (
            <label className={styles.filterField} htmlFor="unplaced-device-room-filter">
              <span className={styles.filterLabel}>
                {tFloor("panels.unplacedRoomFilterLabel")}
              </span>
              <select
                id="unplaced-device-room-filter"
                className={styles.roomSelect}
                value={activeRoomFilter}
                onChange={(event) => setSelectedRoomFilter(event.target.value)}
              >
                <option value={ALL_ROOMS_FILTER}>
                  {tFloor("panels.unplacedRoomFilterAll", {
                    count: devices.length,
                  })}
                </option>
                {roomFilterOptions.map((option) => (
                  <option key={option.value} value={option.value}>
                    {option.value === UNASSIGNED_ROOM_FILTER
                      ? tFloor("panels.unplacedRoomFilterUnassigned", {
                        count: option.count,
                        label: option.label,
                      })
                      : tFloor("panels.unplacedRoomFilterRoom", {
                        count: option.count,
                        label: option.label,
                      })}
                  </option>
                ))}
              </select>
            </label>
          ) : null}

          {filteredDevices.length === 0 ? (
            <div className={styles.emptyState}>
              {tFloor("panels.unplacedFilteredEmpty")}
            </div>
          ) : (
            <div className={styles.list}>
              {filteredDevices.map((device) => {
                const isSelected = device.id === selectedDeviceId;
                const statusClassName = device.isOnline
                  ? styles.statusDotOnline
                  : styles.statusDotOffline;
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
                      const dragPreview = createDeviceDragPreview(category);
                      event.dataTransfer.setDragImage(dragPreview, 32, 32);
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
                        className={`${styles.statusDot} ${statusClassName}`}
                        aria-hidden="true"
                      />
                    </div>
                    <div className={styles.itemMeta}>
                      {getDeviceRoomLabel(device, roomFallbackLabel)}
                    </div>
                  </button>
                );
              })}
            </div>
          )}
        </>
      )}
    </section>
  );
}
