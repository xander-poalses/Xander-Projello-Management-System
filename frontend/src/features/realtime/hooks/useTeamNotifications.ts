import { useEffect } from "react";
import { message as antdMessage } from "antd";
import { createSignalRClient } from "../services/signalrClient";
import { API_BASE_URL } from "../../../config";
import { getToken } from "../../../auth/tokenStore";

type TeamJoinedNotification = {
  projectId: number;
  projectName: string;
  memberName: string;
  assignedAs: string;
  message: string;
};

const teamNotificationClient = createSignalRClient<{
  WorkerJoinedProject: [TeamJoinedNotification];
}>({
   hubUrl: `${API_BASE_URL}/teamNotificationHub`,
  getAccessToken: () => {
    const token = getToken();
    if (!token) return null;
    return token.startsWith('"') && token.endsWith('"') ? token.slice(1, -1) : token;
  },
});

export function useTeamNotifications() {
  useEffect(() => {
    let unsubscribe: (() => void) | undefined;
    let cancelled = false;

    let retryTimer: ReturnType<typeof setTimeout> | undefined;

    // This hook mounts before sign-in finishes and while Render may still be waking up,
    // so a single attempt often fails. Keep trying with the latest token.
    const RETRY_DELAYS_MS = [2000, 5000, 10000, 30000];

    const start = async (attempt = 0) => {
      if (cancelled) return;

      const retry = () => {
        if (cancelled) return;
        const delay = RETRY_DELAYS_MS[Math.min(attempt, RETRY_DELAYS_MS.length - 1)];
        retryTimer = setTimeout(() => void start(attempt + 1), delay);
      };

      // Not signed in yet: check again shortly instead of giving up for the session
      if (!getToken()) {
        retry();
        return;
      }

      try {
        await teamNotificationClient.start();

        unsubscribe?.();
        unsubscribe = teamNotificationClient.on("WorkerJoinedProject", (payload) => {
          antdMessage.success(payload.message || `${payload.memberName} joined ${payload.projectName}.`);
        });
      } catch (err) {
        if (cancelled) return;
        console.warn(`Team notification hub not connected yet (attempt ${attempt + 1}), retrying:`, err);
        retry();
      }
    };

    void start();

    return () => {
      cancelled = true;
      if (retryTimer) clearTimeout(retryTimer);
      unsubscribe?.();
    };
  }, []);
}