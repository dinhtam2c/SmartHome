import type { KonvaEventObject } from "konva/lib/Node";
import { Circle, Group, Layer, Path, Text } from "react-konva";
import { getRgbHex } from "@/features/capabilities";
import {
  DEVICE_CATEGORY_ICON_PATHS,
  resolveDeviceCategory,
  useDeviceCategoryRegistry,
  type DeviceCategoryDefinition,
} from "@/features/device-categories";
import type { CanvasDevice, Point } from "../../types/floorTypes";
import { clampPoint } from "../../services/floorGeometry";

type Props = {
  devices: CanvasDevice[];
  selectedPlacedFloorDeviceId: string | null;
  canvasOffset: Point;
  canvasWidth: number;
  canvasHeight: number;
  dragPadding: number;
  interactive: boolean;
  draggable: boolean;
  onDeviceClick: (placedFloorDeviceId: string) => void;
  onDeviceDragEnd: (placedFloorDeviceId: string, x: number, y: number) => void;
};

function getCapabilityStateBadge(device: CanvasDevice) {
  if (device.isDeleted) {
    return "#d66676";
  }

  if (!device.isOnline) {
    return "#76879a";
  }

  const states =
    device.deviceSnapshot?.endpoints.flatMap((endpoint) =>
      endpoint.capabilities.map((capability) => capability.state)
    ) ?? [];
  const rgbColor = states
    .map((state) => getRgbHex(state))
    .find((value): value is string => typeof value === "string");

  if (rgbColor) {
    return rgbColor;
  }

  const hasTruthyState = states.some((state) => {
    if (!state || typeof state !== "object") {
      return false;
    }

    const values = Object.values(state);

    return values.some((value) => {
      if (value === true) {
        return true;
      }

      if (typeof value === "string") {
        const normalized = value.trim().toLowerCase();
        return (
          normalized === "on" ||
          normalized === "open" ||
          normalized === "active" ||
          normalized === "detected"
        );
      }

      return false;
    });
  });

  return hasTruthyState ? "#39a66d" : "#4b92cc";
}

type DeviceNodeProps = {
  device: CanvasDevice;
  isSelected: boolean;
  interactive: boolean;
  draggable: boolean;
  canvasWidth: number;
  canvasHeight: number;
  dragPadding: number;
  categories: DeviceCategoryDefinition[];
  onDeviceClick: (placedFloorDeviceId: string) => void;
  onDeviceDragEnd: (placedFloorDeviceId: string, x: number, y: number) => void;
};

function DeviceCategoryKonvaIcon({ category }: { category: DeviceCategoryDefinition }) {
  const paths =
    DEVICE_CATEGORY_ICON_PATHS[category.iconKey] ?? DEVICE_CATEGORY_ICON_PATHS.box;

  return (
    <Group x={-12} y={-12}>
      {paths.map((path) => (
        <Path
          key={path.d}
          data={path.d}
          fill={path.fill ? "#0e2940" : undefined}
          stroke="#0e2940"
          strokeWidth={1.9}
          lineCap="round"
          lineJoin="round"
          listening={false}
        />
      ))}
    </Group>
  );
}

function DeviceNode({
  device,
  isSelected,
  interactive,
  draggable,
  canvasWidth,
  canvasHeight,
  dragPadding,
  categories,
  onDeviceClick,
  onDeviceDragEnd,
}: DeviceNodeProps) {
  const category = resolveDeviceCategory(
    categories,
    device.deviceSnapshot?.category
  );
  const badgeColor = getCapabilityStateBadge(device);

  const handleClick = (event: KonvaEventObject<MouseEvent | TouchEvent>) => {
    event.cancelBubble = true;
    onDeviceClick(device.id);
  };

  const handleDragMove = (event: KonvaEventObject<DragEvent>) => {
    const node = event.target;
    node.position(
      clampPoint(
        {
          x: node.x(),
          y: node.y(),
        },
        canvasWidth,
        canvasHeight,
        dragPadding
      )
    );
  };

  const handleDragEnd = (event: KonvaEventObject<DragEvent>) => {
    const node = event.target;
    const nextPoint = clampPoint(
      {
        x: node.x(),
        y: node.y(),
      },
      canvasWidth,
      canvasHeight,
      dragPadding
    );

    node.position(nextPoint);
    onDeviceDragEnd(device.id, nextPoint.x, nextPoint.y);
  };

  return (
    <Group
      x={device.x}
      y={device.y}
      listening={interactive}
      draggable={draggable}
      opacity={device.isDeleted ? 0.58 : 1}
      onClick={handleClick}
      onTap={handleClick}
      onDragMove={handleDragMove}
      onDragEnd={handleDragEnd}
    >
      <Circle
        radius={30}
        fill="#ffffff"
        shadowBlur={isSelected ? 20 : 12}
        shadowColor={isSelected ? "rgba(36, 92, 143, 0.28)" : "rgba(16, 42, 68, 0.18)"}
      />
      <Circle
        radius={24}
        fill={category.color}
        stroke={isSelected ? "#1f6fa4" : "rgba(255,255,255,0.82)"}
        strokeWidth={isSelected ? 4 : 2}
      />
      {device.isDeleted ? (
        <Text
          x={-20}
          y={-8}
          width={40}
          align="center"
          text="X"
          fontSize={13}
          fontStyle="bold"
          fill="#0e2940"
          listening={false}
          verticalAlign="middle"
          wrap="none"
        />
      ) : (
        <DeviceCategoryKonvaIcon category={category} />
      )}
      <Circle
        x={17}
        y={-18}
        radius={8}
        fill={badgeColor}
        stroke="#ffffff"
        strokeWidth={2}
      />
      <Text
        x={-52}
        y={36}
        width={104}
        height={30}
        align="center"
        text={device.displayName}
        fontSize={11}
        fill="#183249"
        ellipsis
        listening={false}
        verticalAlign="top"
        wrap="none"
      />
    </Group>
  );
}

export function DevicesLayer({
  devices,
  selectedPlacedFloorDeviceId,
  canvasOffset,
  canvasWidth,
  canvasHeight,
  dragPadding,
  interactive,
  draggable,
  onDeviceClick,
  onDeviceDragEnd,
}: Props) {
  const { categories } = useDeviceCategoryRegistry();

  return (
    <Layer x={canvasOffset.x} y={canvasOffset.y}>
      {devices.map((device) => (
        <DeviceNode
          key={device.id}
          device={device}
          isSelected={device.id === selectedPlacedFloorDeviceId}
          interactive={interactive}
          draggable={draggable}
          canvasWidth={canvasWidth}
          canvasHeight={canvasHeight}
          dragPadding={dragPadding}
          categories={categories}
          onDeviceClick={onDeviceClick}
          onDeviceDragEnd={onDeviceDragEnd}
        />
      ))}
    </Layer>
  );
}
