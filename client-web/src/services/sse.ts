import { API_BASE_URL } from "@/config";

export type SseEventHandlers = Record<
  string,
  (event: MessageEvent<string>) => void
>;

type SubscribeToSseOptions = {
  path: string;
  handlers: SseEventHandlers;
  onReconnect?: () => void | Promise<void>;
};

export function subscribeToSse({
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

export function parseSseEventData<T>(event: MessageEvent<string>) {
  try {
    return JSON.parse(event.data) as T;
  } catch {
    return null;
  }
}

function toCamelCaseKey(key: string) {
  if (key.length === 0) {
    return key;
  }

  return `${key.charAt(0).toLowerCase()}${key.slice(1)}`;
}

function toCamelCasePayload(value: unknown): unknown {
  if (Array.isArray(value)) {
    return value.map((entry) => toCamelCasePayload(entry));
  }

  if (!value || typeof value !== "object") {
    return value;
  }

  const entries = Object.entries(value as Record<string, unknown>).map(
    ([key, entryValue]) => [toCamelCaseKey(key), toCamelCasePayload(entryValue)]
  );

  return Object.fromEntries(entries);
}

export function parseSseEventDataCamelCase<T>(event: MessageEvent<string>) {
  const payload = parseSseEventData<unknown>(event);

  if (payload === null) {
    return null;
  }

  return toCamelCasePayload(payload) as T;
}
