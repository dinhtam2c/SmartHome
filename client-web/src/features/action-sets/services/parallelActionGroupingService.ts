import type { SelectableDeviceDto } from "@/features/capabilities";
import type { HomeRoomOverviewDto } from "@/features/homes";
import type { ActionDraft } from "./actionSetFormService";

export type IndexedActionDraft = {
  action: ActionDraft;
  originalIndex: number;
};

export type ParallelDeviceActionGroup = {
  device: SelectableDeviceDto;
  actions: IndexedActionDraft[];
};

export type ParallelRoomActionGroup = {
  roomId: string | null;
  roomName: string | null;
  devices: ParallelDeviceActionGroup[];
  actionCount: number;
};

export type ParallelActionGroups = {
  rooms: ParallelRoomActionGroup[];
  incompleteActions: IndexedActionDraft[];
};

export function groupParallelActions(
  actions: ActionDraft[],
  devices: SelectableDeviceDto[],
  rooms: HomeRoomOverviewDto[]
): ParallelActionGroups {
  const devicesById = new Map(devices.map((device) => [device.id, device]));
  const roomNamesById = new Map(rooms.map((room) => [room.id, room.name]));
  const roomOrderById = new Map(rooms.map((room, index) => [room.id, index]));
  const roomGroups = new Map<string, ParallelRoomActionGroup>();
  const deviceGroups = new Map<string, ParallelDeviceActionGroup>();
  const incompleteActions: IndexedActionDraft[] = [];

  actions.forEach((action, originalIndex) => {
    const indexedAction = { action, originalIndex };
    const device = devicesById.get(action.deviceId);
    if (!device) {
      incompleteActions.push(indexedAction);
      return;
    }

    const roomId = device.roomId;
    const roomKey = roomId ?? "unassigned";
    let roomGroup = roomGroups.get(roomKey);
    if (!roomGroup) {
      roomGroup = {
        roomId,
        roomName: roomId
          ? (roomNamesById.get(roomId) ?? device.roomName)
          : null,
        devices: [],
        actionCount: 0,
      };
      roomGroups.set(roomKey, roomGroup);
    }

    let deviceGroup = deviceGroups.get(device.id);
    if (!deviceGroup) {
      deviceGroup = { device, actions: [] };
      deviceGroups.set(device.id, deviceGroup);
      roomGroup.devices.push(deviceGroup);
    }

    deviceGroup.actions.push(indexedAction);
    roomGroup.actionCount += 1;
  });

  const sortedRooms = Array.from(roomGroups.values()).sort((left, right) => {
    const leftOrder = left.roomId
      ? (roomOrderById.get(left.roomId) ?? Number.MAX_SAFE_INTEGER - 1)
      : Number.MAX_SAFE_INTEGER;
    const rightOrder = right.roomId
      ? (roomOrderById.get(right.roomId) ?? Number.MAX_SAFE_INTEGER - 1)
      : Number.MAX_SAFE_INTEGER;
    return leftOrder - rightOrder;
  });

  return { rooms: sortedRooms, incompleteActions };
}
