import React, { useEffect, useState } from "react";
import { message as antdMessage } from "antd";
import { API_BASE_URL } from "../../config";
import { getToken } from "../../auth/tokenStore";
import { SkeletonRows } from "../../components/Skeleton";
import EmptyState from "../../components/EmptyState";

type DeletedProject = { projectID: number; name: string; clientName: string; deletedAt: string };
type AuditEntry = { id: number; action: string; entityType: string; summary: string; userName: string | null; createdAt: string };

const authHeaders = (): HeadersInit => {
  const token = getToken();
  return token ? { Authorization: `Bearer ${token}` } : {};
};

const when = (iso: string) =>
  new Date(iso).toLocaleString(undefined, { day: "numeric", month: "short", hour: "2-digit", minute: "2-digit" });

// For Owners and Admins: restore deleted projects and see who changed what
export default function HistoryActivity({ onRestored }: { onRestored?: () => void }) {
  const [deleted, setDeleted] = useState<DeletedProject[] | null>(null);
  const [audit, setAudit] = useState<AuditEntry[] | null>(null);
  const [failed, setFailed] = useState(false);
  const [restoringId, setRestoringId] = useState<number | null>(null);

  const load = async () => {
    setFailed(false);
    try {
      const [d, a] = await Promise.all([
        fetch(`${API_BASE_URL}/api/projects/deleted`, { headers: authHeaders() }),
        fetch(`${API_BASE_URL}/api/audit?take=20`, { headers: authHeaders() }),
      ]);
      if (!d.ok || !a.ok) throw new Error();
      setDeleted(await d.json());
      setAudit(await a.json());
    } catch {
      setFailed(true);
      setDeleted([]);
      setAudit([]);
    }
  };

  useEffect(() => {
    load();
  }, []);

  const restore = async (project: DeletedProject) => {
    setRestoringId(project.projectID);
    try {
      const res = await fetch(`${API_BASE_URL}/api/projects/${project.projectID}/restore`, { method: "POST", headers: authHeaders() });
      if (!res.ok) throw new Error();
      antdMessage.success(`${project.name} restored`);
      await load();
      onRestored?.();
    } catch {
      antdMessage.error(`Couldn't restore ${project.name}. Try again.`);
    } finally {
      setRestoringId(null);
    }
  };

  if (failed) {
    return <EmptyState tone="error" title="Couldn't load recent activity" message="Check your connection and open History again." />;
  }

  return (
    <div className="history-activity">
      <section aria-labelledby="recently-deleted-title">
        <h3 id="recently-deleted-title" className="pj-section-title">Recently deleted</h3>
        {deleted === null ? (
          <SkeletonRows count={2} height={64} label="Loading deleted projects" />
        ) : deleted.length === 0 ? (
          <EmptyState title="Nothing deleted" message="Deleted projects stay here so you can restore them." />
        ) : (
          <ul className="history-activity__list">
            {deleted.map((p) => (
              <li key={p.projectID} className="history-activity__row">
                <div>
                  <strong>{p.name}</strong>
                  <span className="history-activity__meta">{p.clientName} · deleted {when(p.deletedAt)}</span>
                </div>
                <button
                  type="button"
                  className="btn-secondary"
                  onClick={() => restore(p)}
                  disabled={restoringId === p.projectID}
                  aria-busy={restoringId === p.projectID}
                >
                  {restoringId === p.projectID && <span className="pj-spinner" aria-hidden="true" />}
                  {restoringId === p.projectID ? "Restoring..." : "Restore project"}
                </button>
              </li>
            ))}
          </ul>
        )}
      </section>

      <section aria-labelledby="recent-changes-title">
        <h3 id="recent-changes-title" className="pj-section-title">Recent changes</h3>
        {audit === null ? (
          <SkeletonRows count={3} height={56} label="Loading recent changes" />
        ) : audit.length === 0 ? (
          <EmptyState title="No changes recorded yet" message="Changes to clients, projects, milestones and tasks show up here." />
        ) : (
          <ul className="history-activity__list">
            {audit.map((a) => (
              <li key={a.id} className="history-activity__row">
                <div>
                  <strong>{a.action} {a.entityType.toLowerCase()}</strong>
                  {a.summary && <span className="history-activity__summary">{a.summary}</span>}
                </div>
                <span className="history-activity__meta">{a.userName ?? "Someone"} · {when(a.createdAt)}</span>
              </li>
            ))}
          </ul>
        )}
      </section>
    </div>
  );
}
