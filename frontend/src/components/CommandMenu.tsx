import React, { useEffect, useMemo, useRef, useState } from "react";
import { useNavigate } from "react-router-dom";
import "./CommandMenu.css";

type Command = {
  id: string;
  label: string;
  hint?: string;
  run: () => void;
};

// Ctrl+K (Cmd+K on Mac) opens a searchable list of pages and actions.
// Ctrl+1..4 jump straight to the main pages from anywhere.
export default function CommandMenu() {
  const navigate = useNavigate();
  const [open, setOpen] = useState(false);
  const [query, setQuery] = useState("");
  const [active, setActive] = useState(0);
  const inputRef = useRef<HTMLInputElement>(null);
  const returnFocus = useRef<HTMLElement | null>(null);

  const toggleTheme = () => {
    const next = document.documentElement.getAttribute("data-theme") === "dark" ? "light" : "dark";
    document.documentElement.setAttribute("data-theme", next);
    try {
      localStorage.setItem("theme", next);
    } catch {
      // Theme still switches for this session
    }
  };

  const commands: Command[] = useMemo(() => [
    { id: "jello", label: "Go to Jello Jobs", hint: "Ctrl+1", run: () => navigate("/dashboard") },
    { id: "history", label: "Go to History", hint: "Ctrl+2", run: () => navigate("/history") },
    { id: "management", label: "Go to Management", hint: "Ctrl+3", run: () => navigate("/management") },
    { id: "settings", label: "Go to Settings", hint: "Ctrl+4", run: () => navigate("/settings") },
    { id: "theme", label: "Switch light or dark mode", run: toggleTheme },
  ], [navigate]);

  const filtered = commands.filter((c) => c.label.toLowerCase().includes(query.trim().toLowerCase()));

  const close = () => {
    setOpen(false);
    returnFocus.current?.focus?.();
  };

  const runCommand = (command: Command | undefined) => {
    if (!command) return;
    close();
    command.run();
  };

  // Global shortcuts
  useEffect(() => {
    const onKey = (e: KeyboardEvent) => {
      const mod = e.ctrlKey || e.metaKey;
      if (mod && e.key.toLowerCase() === "k") {
        e.preventDefault();
        returnFocus.current = document.activeElement as HTMLElement;
        setQuery("");
        setActive(0);
        setOpen((o) => !o);
        return;
      }
      const index = ["1", "2", "3", "4"].indexOf(e.key);
      if (mod && index >= 0) {
        e.preventDefault();
        commands[index].run();
      }
    };
    window.addEventListener("keydown", onKey);
    return () => window.removeEventListener("keydown", onKey);
  }, [commands]);

  useEffect(() => {
    if (open) inputRef.current?.focus();
  }, [open]);

  if (!open) return null;

  const onListKey = (e: React.KeyboardEvent) => {
    if (e.key === "ArrowDown") {
      e.preventDefault();
      setActive((a) => Math.min(a + 1, filtered.length - 1));
    } else if (e.key === "ArrowUp") {
      e.preventDefault();
      setActive((a) => Math.max(a - 1, 0));
    } else if (e.key === "Enter") {
      e.preventDefault();
      runCommand(filtered[active]);
    } else if (e.key === "Escape") {
      e.preventDefault();
      close();
    } else if (e.key === "Tab") {
      // Keep focus inside the menu
      e.preventDefault();
    }
  };

  return (
    <div className="command-menu__scrim" onMouseDown={close}>
      <div
        className="command-menu"
        role="dialog"
        aria-modal="true"
        aria-label="Command menu"
        onMouseDown={(e) => e.stopPropagation()}
        onKeyDown={onListKey}
      >
        <input
          ref={inputRef}
          className="command-menu__input"
          placeholder="Type a page or action"
          aria-label="Search commands"
          aria-controls="command-menu-list"
          aria-activedescendant={filtered[active] ? `command-${filtered[active].id}` : undefined}
          value={query}
          onChange={(e) => {
            setQuery(e.target.value);
            setActive(0);
          }}
        />
        <ul id="command-menu-list" className="command-menu__list" role="listbox">
          {filtered.length === 0 && <li className="command-menu__empty">No matching pages or actions</li>}
          {filtered.map((c, i) => (
            <li
              key={c.id}
              id={`command-${c.id}`}
              role="option"
              aria-selected={i === active}
              className={`command-menu__item ${i === active ? "is-active" : ""}`}
              onMouseEnter={() => setActive(i)}
              onClick={() => runCommand(c)}
            >
              <span>{c.label}</span>
              {c.hint && <kbd className="command-menu__hint">{c.hint}</kbd>}
            </li>
          ))}
        </ul>
        <p className="command-menu__footer">
          <kbd>↑</kbd> <kbd>↓</kbd> to move, <kbd>Enter</kbd> to open, <kbd>Esc</kbd> to close
        </p>
      </div>
    </div>
  );
}
