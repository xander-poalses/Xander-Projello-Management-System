import { app, ipcMain, safeStorage } from 'electron';
import fs from 'fs';
import path from 'path';

// Main-process side of the token store: the sign-in token is encrypted with the
// operating system's key store (DPAPI on Windows) and written to the app's profile folder.
const tokenFile = (): string => path.join(app.getPath('userData'), 'session.bin');

function readToken(): string | null {
  try {
    if (!safeStorage.isEncryptionAvailable() || !fs.existsSync(tokenFile())) return null;
    return safeStorage.decryptString(fs.readFileSync(tokenFile()));
  } catch {
    // A corrupt or foreign file just means "signed out"
    return null;
  }
}

function writeToken(token: string): void {
  if (!safeStorage.isEncryptionAvailable()) return;
  fs.writeFileSync(tokenFile(), safeStorage.encryptString(token));
}

function clearToken(): void {
  try {
    fs.rmSync(tokenFile(), { force: true });
  } catch {
    // Nothing to clear
  }
}

export function registerSecureTokenStore(): void {
  ipcMain.on('secure-token:get', (event) => { event.returnValue = readToken(); });
  ipcMain.on('secure-token:set', (event, token: unknown) => {
    if (typeof token === 'string') writeToken(token);
    event.returnValue = null;
  });
  ipcMain.on('secure-token:clear', (event) => { clearToken(); event.returnValue = null; });
}
