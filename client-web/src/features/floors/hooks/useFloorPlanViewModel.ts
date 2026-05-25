import { useMemo } from "react";
import { useTranslation } from "react-i18next";
import type { SelectableDeviceDto } from "@/features/capabilities";
import type { HomeRoomOverviewDto } from "@/features/homes";
import type { FloorDeviceGroup } from "../components/panels/UnplacedFloorDevicesPanel";
import type {
  CanvasDevice,
  CanvasRoom,
  Floor,
  FloorSummary,
} from "../types/floorTypes";

type Params = {
  floor: Floor | null;
  floors: FloorSummary[];
  rooms: HomeRoomOverviewDto[];
  devices: SelectableDeviceDto[];
  devicesById: Map<string, SelectableDeviceDto>;
  selectedRoomId: string | null;
  selectedPlacementId: string | null;
};

export function useFloorPlanViewModel({
  floor,
  floors,
  rooms,
  devices,
  devicesById,
  selectedRoomId,
  selectedPlacementId,
}: Params) {
  const { t } = useTranslation("floors");
  const roomNamesById = useMemo(
    () => new Map(rooms.map((room) => [room.id, room.name])),
    [rooms]
  );
  const allMappedRoomIds = useMemo(
    () => new Set(floors.flatMap((summary) => summary.mappedRoomIds)),
    [floors]
  );
  const currentMappedRoomIds = useMemo(
    () => new Set(floor?.floorPlanRooms.map((room) => room.roomId) ?? []),
    [floor?.floorPlanRooms]
  );
  const availableRooms = useMemo(
    () => rooms.filter((room) => !allMappedRoomIds.has(room.id)),
    [allMappedRoomIds, rooms]
  );
  const canvasRooms = useMemo<CanvasRoom[]>(
    () => floor?.floorPlanRooms.map((room) => ({
      ...room,
      name: roomNamesById.get(room.roomId) ?? t("panels.unassignedRoom"),
    })) ?? [],
    [floor?.floorPlanRooms, roomNamesById, t]
  );
  const canvasDevices = useMemo<CanvasDevice[]>(() => {
    if (!floor) return [];
    return floor.devicePlacements.flatMap((placement) => {
      const snapshot = devicesById.get(placement.deviceId);
      return snapshot
        ? [{ ...placement, displayName: snapshot.name, deviceSnapshot: snapshot }]
        : [];
    });
  }, [devicesById, floor]);
  const unplacedDevices = useMemo(() => {
    const placedDeviceIds = new Set(floors.flatMap((summary) => summary.placedDeviceIds));
    return devices.filter((device) => {
      if (placedDeviceIds.has(device.id)) return false;
      if (!device.roomId) return true;
      return currentMappedRoomIds.has(device.roomId)
        || !allMappedRoomIds.has(device.roomId);
    });
  }, [allMappedRoomIds, currentMappedRoomIds, devices, floors]);
  const deviceGroups = useMemo<FloorDeviceGroup[]>(() => {
    const groups: FloorDeviceGroup[] = [];
    const byRoom = new Map<string, SelectableDeviceDto[]>();
    unplacedDevices.forEach((device) => {
      if (!device.roomId) return;
      const roomDevices = byRoom.get(device.roomId) ?? [];
      roomDevices.push(device);
      byRoom.set(device.roomId, roomDevices);
    });

    rooms.forEach((room) => {
      const roomDevices = byRoom.get(room.id);
      if (!roomDevices?.length) return;
      groups.push({
        id: room.id,
        title: currentMappedRoomIds.has(room.id)
          ? t("panels.groupCurrentFloor", { room: room.name })
          : t("panels.groupNotDrawn", { room: room.name }),
        devices: roomDevices,
      });
    });
    const unassigned = unplacedDevices.filter((device) => !device.roomId);
    if (unassigned.length) {
      groups.push({
        id: "unassigned",
        title: t("panels.groupUnassigned"),
        devices: unassigned,
      });
    }
    return groups;
  }, [currentMappedRoomIds, rooms, t, unplacedDevices]);

  return {
    availableRooms,
    canvasDevices,
    canvasRooms,
    deviceGroups,
    roomNamesById,
    selectedCanvasDevice:
      canvasDevices.find((device) => device.id === selectedPlacementId) ?? null,
    selectedPlacement:
      floor?.devicePlacements.find((placement) => placement.id === selectedPlacementId) ?? null,
    selectedRoom: canvasRooms.find((room) => room.id === selectedRoomId) ?? null,
    unplacedDevices,
  };
}
