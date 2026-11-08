// See the Electron documentation for details on how to use preload scripts:
// https://www.electronjs.org/docs/latest/tutorial/process-model#preload-scripts
import { contextBridge, ipcRenderer } from 'electron';

// Only these three calls cross into the main process, which keeps the token
// encrypted with safeStorage (see secureTokenStore.ts)
contextBridge.exposeInMainWorld('secureStore', {
  getToken: (): string | null => ipcRenderer.sendSync('secure-token:get'),
  setToken: (token: string): void => { ipcRenderer.sendSync('secure-token:set', token); },
  clearToken: (): void => { ipcRenderer.sendSync('secure-token:clear'); },
});
