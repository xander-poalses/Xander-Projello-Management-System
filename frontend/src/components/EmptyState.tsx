import React from "react";
import "./EmptyState.css";

type EmptyStateProps = {
  title: string;
  message?: string;
  /** Optional next step, for example an Add button */
  action?: React.ReactNode;
  tone?: "neutral" | "error";
};

// Clear empty and error states: what's missing, and what the user can do about it
export default function EmptyState({ title, message, action, tone = "neutral" }: EmptyStateProps) {
  return (
    <div className={`pj-empty-state pj-empty-state--${tone}`} role={tone === "error" ? "alert" : "status"}>
      <p className="pj-empty-state__title">{title}</p>
      {message && <p className="pj-empty-state__message">{message}</p>}
      {action && <div className="pj-empty-state__action">{action}</div>}
    </div>
  );
}
