import { API_BASE_URL } from "../app/config";

export async function api<T>(
  path: string, options: RequestInit = {}
): Promise<T> {
  const res = await fetch(`${API_BASE_URL}${path}`, {
    headers: {
      "Content-type": "application/json",
      ...options.headers
    },
    ...options
  });

  //await new Promise(resolve => setTimeout(resolve, 1000));

  if (!res.ok) {
    const text = await res.text();
    throw new Error(text || res.statusText);
  }

  const contentType = res.headers.get("content-type");
  if (contentType && contentType.includes("application/json")) {
    return res.json();
  } else {
    return undefined as T;
  }
}
