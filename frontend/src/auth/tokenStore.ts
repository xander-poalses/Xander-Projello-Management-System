// The sign-in token. In the Electron app it lives in the main process, encrypted with
// the operating system's key store (safeStorage), so other web content and anyone
// reading the profile folder can't lift it from localStorage. Tests and plain browsers
// have no bridge and fall back to localStorage.

interface SecureStoreBridge {
  getToken(): string | null;
  setToken(token: string): void;
  clearToken(): void;
}

const LEGACY_KEY = "token";

function bridge(): SecureStoreBridge | undefined {
  return (window as unknown as { secureStore?: SecureStoreBridge }).secureStore;
}

export function getToken(): string | null {
  const store = bridge();
  if (!store) return localStorage.getItem(LEGACY_KEY);

  const token = store.getToken();
  if (token) return token;

  // One-time move for people who signed in before the token was moved out of localStorage
  const legacy = localStorage.getItem(LEGACY_KEY);
  if (legacy) {
    store.setToken(legacy);
    localStorage.removeItem(LEGACY_KEY);
  }
  return legacy;
}

export function setToken(token: string): void {
  const store = bridge();
  if (store) store.setToken(token);
  else localStorage.setItem(LEGACY_KEY, token);
}

export function clearToken(): void {
  bridge()?.clearToken();
  localStorage.removeItem(LEGACY_KEY);
}
