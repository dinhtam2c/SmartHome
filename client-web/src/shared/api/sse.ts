import { API_BASE_URL } from "@/config";

type SseEventHandlers = Record<
  string,
  (event: MessageEvent<string>) => void
>;

const REALTIME_DELTA_EVENT = "RealtimeDelta";

type RealtimeEntity =
  | "AutomationExecution"
  | "AutomationRule"
  | "Device"
  | "DeviceCapability"
  | "DeviceCommandExecution"
  | "Floor"
  | "Room"
  | "Scene"
  | "SceneExecution";

type RealtimeChange =
  | "Created"
  | "Deleted"
  | "Moved"
  | "StateChanged"
  | "StatusChanged"
  | "Updated";

export interface RealtimeDeltaEvent<TDelta = unknown> {
  version: number;
  entity: RealtimeEntity;
  change: RealtimeChange;
  occurredAt: number;
  homeId?: string | null;
  roomId?: string | null;
  previousRoomId?: string | null;
  deviceId?: string | null;
  floorId?: string | null;
  sceneId?: string | null;
  ruleId?: string | null;
  executionId?: string | null;
  endpointId?: string | null;
  capabilityId?: string | null;
  correlationId?: string | null;
  delta?: TDelta | null;
}

type SubscribeToSseOptions = {
  path: string;
  handlers: SseEventHandlers;
  onReconnect?: () => void | Promise<void>;
};

function subscribeToSse({
  path,
  handlers,
  onReconnect,
}: SubscribeToSseOptions) {
  if (typeof window === "undefined" || typeof EventSource === "undefined") {
    return () => undefined;
  }

  const source = new EventSource(`${API_BASE_URL}${path}`);
  let hasOpened = false;

  const openListener: EventListener = () => {
    if (hasOpened) {
      void onReconnect?.();
    }

    hasOpened = true;
  };

  source.addEventListener("open", openListener);

  const entries = Object.entries(handlers);
  entries.forEach(([eventName, handler]) => {
    source.addEventListener(eventName, handler as EventListener);
  });

  return () => {
    source.removeEventListener("open", openListener);

    entries.forEach(([eventName, handler]) => {
      source.removeEventListener(eventName, handler as EventListener);
    });

    source.close();
  };
}

function parseSseEventData<T>(event: MessageEvent<string>) {
  try {
    return JSON.parse(event.data) as T;
  } catch {
    return null;
  }
}

function parseRealtimeDeltaEvent(
  event: MessageEvent<string>
): RealtimeDeltaEvent | null {
  const payload = parseSseEventData<RealtimeDeltaEvent>(event);

  if (
    !payload ||
    payload.version !== 1 ||
    typeof payload.entity !== "string" ||
    typeof payload.change !== "string"
  ) {
    return null;
  }

  return payload;
}

export function subscribeToRealtimeDeltas({
  path,
  onDelta,
  onReconnect,
}: {
  path: string;
  onDelta: (event: RealtimeDeltaEvent) => void;
  onReconnect?: () => void | Promise<void>;
}) {
  return subscribeToSse({
    path,
    handlers: {
      [REALTIME_DELTA_EVENT]: (event) => {
        const delta = parseRealtimeDeltaEvent(event);
        if (delta) {
          onDelta(delta);
        }
      },
    },
    onReconnect,
  });
}
