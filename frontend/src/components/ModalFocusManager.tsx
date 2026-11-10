import { useEffect } from "react";

// Hand-built modals (not antd) that need dialog behaviour
const DIALOG_SELECTOR = ".action-modal, .status-modal, .overlay-container, .modal-content";
const FOCUSABLE =
  'a[href], button:not([disabled]), input:not([disabled]), select:not([disabled]), textarea:not([disabled]), [tabindex]:not([tabindex="-1"])';

let idCounter = 0;

const focusables = (dialog: HTMLElement) =>
  Array.from(dialog.querySelectorAll<HTMLElement>(FOCUSABLE)).filter((el) => el.offsetParent !== null);

// Gives every custom modal: dialog role and name, focus moved inside,
// Tab kept inside, Escape to close, and focus returned on close.
export default function ModalFocusManager() {
  useEffect(() => {
    let lastFocused: HTMLElement | null = null;

    const trackFocus = (e: FocusEvent) => {
      const target = e.target as HTMLElement;
      if (!target.closest?.(DIALOG_SELECTOR)) lastFocused = target;
    };

    const openDialogs = () => Array.from(document.querySelectorAll<HTMLElement>(DIALOG_SELECTOR));

    const setUp = (dialog: HTMLElement) => {
      if (dialog.dataset.pjDialog) return;
      dialog.dataset.pjDialog = "1";
      dialog.setAttribute("role", "dialog");
      dialog.setAttribute("aria-modal", "true");
      const heading = dialog.querySelector<HTMLElement>("h1, h2, h3");
      if (heading && !dialog.hasAttribute("aria-labelledby")) {
        heading.id ||= `pj-dialog-title-${++idCounter}`;
        dialog.setAttribute("aria-labelledby", heading.id);
      }
      if (!dialog.contains(document.activeElement)) {
        const first = focusables(dialog).find((el) => !el.matches(".action-modal__close")) ?? focusables(dialog)[0];
        if (!first) dialog.tabIndex = -1;
        (first ?? dialog).focus({ preventScroll: true });
      }
    };

    // Remember what opened the first dialog; a step change swaps the dialog node but keeps this
    let opener: HTMLElement | null = null;
    const sync = () => {
      const dialogs = openDialogs();
      if (dialogs.length && !opener) opener = lastFocused;
      dialogs.forEach(setUp);
      if (!dialogs.length && opener) {
        if (document.body.contains(opener)) opener.focus({ preventScroll: true });
        opener = null;
      }
    };

    const onKeyDown = (e: KeyboardEvent) => {
      const dialogs = openDialogs();
      const dialog = dialogs[dialogs.length - 1];
      if (!dialog) return;

      if (e.key === "Escape") {
        const close =
          dialog.querySelector<HTMLElement>(".action-modal__close, [aria-label^='Close']") ?? dialog.parentElement;
        if (close) {
          e.preventDefault();
          close.click();
        }
        return;
      }

      if (e.key !== "Tab") return;
      const items = focusables(dialog);
      if (!items.length) {
        e.preventDefault();
        return;
      }
      const first = items[0];
      const last = items[items.length - 1];
      const active = document.activeElement as HTMLElement | null;
      if (!dialog.contains(active)) {
        e.preventDefault();
        first.focus();
      } else if (e.shiftKey && active === first) {
        e.preventDefault();
        last.focus();
      } else if (!e.shiftKey && active === last) {
        e.preventDefault();
        first.focus();
      }
    };

    const observer = new MutationObserver(sync);
    observer.observe(document.body, { childList: true, subtree: true });
    document.addEventListener("focusin", trackFocus);
    document.addEventListener("keydown", onKeyDown);
    sync();

    return () => {
      observer.disconnect();
      document.removeEventListener("focusin", trackFocus);
      document.removeEventListener("keydown", onKeyDown);
    };
  }, []);

  return null;
}
