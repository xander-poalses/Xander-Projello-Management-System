import React, { useEffect, useState } from "react";
import { message as antdMessage } from "antd";
import { API_BASE_URL } from "../config";
import { getToken } from "../auth/tokenStore";
import "./ProjectPayments.css";

type Props = {
  projectId: number;
  totalPaid?: number | null;
  outstanding?: number | null;
};

const formatRand = (value: number) =>
  new Intl.NumberFormat("en-ZA", { style: "currency", currency: "ZAR" }).format(value);

// Payments are recorded per project; the client's totals are the sum of its projects
export default function ProjectPayments({ projectId, totalPaid, outstanding }: Props) {
  const [saved, setSaved] = useState({ paid: totalPaid ?? 0, owed: outstanding ?? 0 });
  const [paid, setPaid] = useState(String(totalPaid ?? 0));
  const [owed, setOwed] = useState(String(outstanding ?? 0));
  const [editing, setEditing] = useState(false);
  const [busy, setBusy] = useState(false);

  useEffect(() => {
    setSaved({ paid: totalPaid ?? 0, owed: outstanding ?? 0 });
  }, [totalPaid, outstanding]);

  const paidNumber = Number(paid);
  const owedNumber = Number(owed);
  const invalid = !(paid.trim() !== "" && paidNumber >= 0) || !(owed.trim() !== "" && owedNumber >= 0);

  const save = async (e: React.FormEvent) => {
    e.preventDefault();
    if (invalid || busy) return;
    setBusy(true);
    try {
      const token = getToken();
      const res = await fetch(`${API_BASE_URL}/api/projects/${projectId}/payments`, {
        method: "PUT",
        headers: { "Content-Type": "application/json", ...(token ? { Authorization: `Bearer ${token}` } : {}) },
        body: JSON.stringify({ totalPaid: paidNumber, outstanding: owedNumber }),
      });
      if (!res.ok) throw new Error();
      const data = await res.json();
      setSaved({ paid: data.totalPaid, owed: data.outstanding });
      setEditing(false);
      antdMessage.success("Payments saved");
    } catch {
      // Keep what they typed so they can try again
      antdMessage.error("Couldn't save payments. Check your connection and try again.");
    } finally {
      setBusy(false);
    }
  };

  return (
    <section className="single-project-view__panel project-payments" aria-labelledby="project-payments-title">
      <div className="single-project-view__panel-header-row">
        <h3 id="project-payments-title" className="single-project-view__panel-title">Payments</h3>
        {!editing && (
          <button
            type="button"
            className="single-project-view__view-all-button"
            onClick={() => {
              setPaid(String(saved.paid));
              setOwed(String(saved.owed));
              setEditing(true);
            }}
          >
            Edit payments
          </button>
        )}
      </div>

      {!editing ? (
        <dl className="project-payments__figures">
          <div>
            <dt>Paid</dt>
            <dd>{formatRand(saved.paid)}</dd>
          </div>
          <div>
            <dt>Outstanding</dt>
            <dd>{formatRand(saved.owed)}</dd>
          </div>
        </dl>
      ) : (
        <form className="project-payments__form" onSubmit={save} noValidate>
          <label className="project-payments__field">
            <span>Paid (R)</span>
            <input
              className="pj-field project-payments__input"
              type="number"
              min="0"
              step="0.01"
              inputMode="decimal"
              value={paid}
              aria-invalid={!(paid.trim() !== "" && paidNumber >= 0)}
              onChange={(e) => setPaid(e.target.value)}
            />
          </label>
          <label className="project-payments__field">
            <span>Outstanding (R)</span>
            <input
              className="pj-field project-payments__input"
              type="number"
              min="0"
              step="0.01"
              inputMode="decimal"
              value={owed}
              aria-invalid={!(owed.trim() !== "" && owedNumber >= 0)}
              onChange={(e) => setOwed(e.target.value)}
            />
          </label>
          {invalid && <p className="project-payments__error" role="alert">Amounts must be 0 or more.</p>}
          <div className="project-payments__actions">
            <button type="button" className="btn-secondary" onClick={() => setEditing(false)} disabled={busy}>
              Cancel
            </button>
            <button type="submit" className="add-button" disabled={busy || invalid} aria-busy={busy}>
              {busy && <span className="pj-spinner" aria-hidden="true" />}
              {busy ? "Saving..." : "Save payments"}
            </button>
          </div>
        </form>
      )}
    </section>
  );
}
