import React from "react";
import { createRoot } from "react-dom/client";
import "./index.css";
import App from "./App";
import { installIdempotentFetch } from "./auth/idempotentFetch";

// Before anything renders, so every create request carries an Idempotency-Key
installIdempotentFetch();

const savedTheme = localStorage.getItem("theme");
if (savedTheme) {
  document.documentElement.setAttribute("data-theme", savedTheme);
}

// Keep the window's title bar buttons in the current theme, wherever the theme is changed
const shell = (window as unknown as { projelloShell?: { setTheme(theme: string): void } }).projelloShell;
const reportTheme = () => shell?.setTheme(document.documentElement.getAttribute("data-theme") || "light");
reportTheme();
new MutationObserver(reportTheme).observe(document.documentElement, { attributes: true, attributeFilter: ["data-theme"] });

const container = document.getElementById("root");
if (container) {
  const root = createRoot(container);
  root.render(<App />);
}