import { API_BASE_URL } from "../config";

const RETRY_DELAYS_MS = [800, 2000];

// randomUUID exists in Electron's Chromium; the project's TS lib just doesn't declare it
const newKey = (): string => {
  const c = (typeof crypto !== "undefined" ? crypto : undefined) as (Crypto & { randomUUID?: () => string }) | undefined;
  return c?.randomUUID ? c.randomUUID() : `${Date.now()}-${Math.random().toString(36).slice(2)}`;
};

// Every create/submit (POST) to the Projello API gets an Idempotency-Key. If the
// connection drops, the request is retried with the same key, and the API replays the
// first result instead of creating a duplicate. Other requests pass through untouched.
export function installIdempotentFetch(): void {
  const originalFetch = window.fetch.bind(window);

  window.fetch = async (input: RequestInfo | URL, init?: RequestInit): Promise<Response> => {
    const url = typeof input === "string" ? input : input instanceof URL ? input.href : input.url;
    const method = (init?.method ?? (input instanceof Request ? input.method : "GET")).toUpperCase();

    if (method !== "POST" || !url.startsWith(API_BASE_URL)) {
      return originalFetch(input, init);
    }

    const headers = new Headers(init?.headers ?? (input instanceof Request ? input.headers : undefined));
    if (!headers.has("Idempotency-Key")) headers.set("Idempotency-Key", newKey());
    const request: RequestInit = { ...init, headers };

    for (let attempt = 0; ; attempt++) {
      try {
        return await originalFetch(input, request);
      } catch (err) {
        // Only network failures are retried; an HTTP error response is returned as normal
        if (attempt >= RETRY_DELAYS_MS.length) throw err;
        await new Promise((resolve) => setTimeout(resolve, RETRY_DELAYS_MS[attempt]));
      }
    }
  };
}
