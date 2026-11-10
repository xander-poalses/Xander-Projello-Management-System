import React from "react";
import { notification } from "antd";

const UNDO_WINDOW_MS = 5000;

type UndoOptions = {
  /** What happened, shown in the toast: "Comment deleted" */
  message: string;
  /** Hide the item right away (optimistic) */
  hide: () => void;
  /** Put the item back (undo, or the server refused) */
  restore: () => void;
  /** The real delete. Runs only if the user doesn't undo in time. */
  commit: () => Promise<void>;
  /** Shown if the real delete fails */
  failureMessage?: string;
};

// Undo toast instead of a confirm dialog: the item disappears at once, the user has
// 5 seconds to bring it back, and only then is the delete sent to the server.
export function deleteWithUndo({ message, hide, restore, commit, failureMessage }: UndoOptions) {
  const key = `undo-${Date.now()}-${Math.random()}`;
  let undone = false;

  hide();

  const timer = window.setTimeout(async () => {
    notification.destroy(key);
    if (undone) return;
    try {
      await commit();
    } catch {
      restore();
      notification.error({ message: failureMessage ?? "Couldn't delete that. It's been put back.", placement: "bottomRight" });
    }
  }, UNDO_WINDOW_MS);

  notification.open({
    key,
    message,
    placement: "bottomRight",
    duration: UNDO_WINDOW_MS / 1000,
    actions: (
      <button
        type="button"
        className="btn-secondary"
        onClick={() => {
          undone = true;
          window.clearTimeout(timer);
          notification.destroy(key);
          restore();
        }}
      >
        Undo
      </button>
    ),
  });
}
