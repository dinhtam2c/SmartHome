import { API_BASE_URL } from "@/config";

export async function api<T>(
  path: string,
  options: RequestInit = {}
): Promise<T> {
  const res = await fetch(`${API_BASE_URL}${path}`, {
    headers: {
      "Content-type": "application/json",
      ...options.headers,
    },
    ...options,
  });

  //await new Promise(resolve => setTimeout(resolve, 1000));

  if (!res.ok) {
    const text = await res.text();
    let message = text || res.statusText;

    if (text) {
      try {
        const parsed = JSON.parse(text) as { detail?: string; title?: string; };
        message = parsed.detail || parsed.title || message;
      } catch {
        // Keep original response text for non-JSON errors.
      }
    }

    throw new Error(message);
  }

  const contentType = res.headers.get("content-type");
  if (contentType && contentType.includes("application/json")) {
    return res.json();
  } else {
    return undefined as T;
  }
}
