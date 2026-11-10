import { API_BASE_URL } from "../config";
import { getToken } from "./tokenStore";

// Small stale-while-revalidate cache for GET requests: show what we had straight away,
// refresh in the background. Kept in memory and sessionStorage for this signed-in session.
type Entry<T> = { data: T; savedAt: number };

const memory = new Map<string, Entry<unknown>>();
const STORAGE_PREFIX = "pj-cache:";
/** After this long, cached data is shown with an "updating" note */
export const STALE_AFTER_MS = 60_000;

export function readCache<T>(path: string): Entry<T> | undefined {
  const hit = memory.get(path) as Entry<T> | undefined;
  if (hit) return hit;
  try {
    const raw = sessionStorage.getItem(STORAGE_PREFIX + path);
    if (!raw) return undefined;
    const entry = JSON.parse(raw) as Entry<T>;
    memory.set(path, entry);
    return entry;
  } catch {
    return undefined;
  }
}

function writeCache<T>(path: string, data: T) {
  const entry = { data, savedAt: Date.now() };
  memory.set(path, entry);
  try {
    sessionStorage.setItem(STORAGE_PREFIX + path, JSON.stringify(entry));
  } catch {
    // Storage full or blocked: the memory copy still works
  }
}

/** Fetch fresh JSON for an API path and store it. Throws on HTTP errors. */
export async function fetchAndCache<T>(path: string): Promise<T> {
  const token = getToken();
  const res = await fetch(`${API_BASE_URL}${path}`, {
    headers: token ? { Authorization: `Bearer ${token}` } : undefined,
  });
  if (!res.ok) throw new Error(`Status ${res.status}`);
  const data = (await res.json()) as T;
  writeCache(path, data);
  return data;
}

/** Warm the cache for a page the user is likely to open next (for example on hover) */
export function prefetch(path: string) {
  const hit = readCache(path);
  if (hit && Date.now() - hit.savedAt < STALE_AFTER_MS) return;
  fetchAndCache(path).catch(() => {
    // Prefetch is best effort
  });
}

/** Forget cached data, for example after signing out */
export function clearCache() {
  memory.clear();
  try {
    Object.keys(sessionStorage)
      .filter((k) => k.startsWith(STORAGE_PREFIX))
      .forEach((k) => sessionStorage.removeItem(k));
  } catch {
    // Nothing stored
  }
}
