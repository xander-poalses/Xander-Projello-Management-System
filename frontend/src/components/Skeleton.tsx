import React from "react";
import "./Skeleton.css";

type SkeletonProps = {
  width?: number | string;
  height?: number | string;
  radius?: string;
  className?: string;
};

// One placeholder block. Compose these to match the layout that is loading.
export function Skeleton({ width = "100%", height = 16, radius = "var(--radius-control)", className = "" }: SkeletonProps) {
  return <span className={`pj-skeleton ${className}`.trim()} style={{ width, height, borderRadius: radius }} aria-hidden="true" />;
}

// Wraps a set of skeletons so screen readers hear one "Loading" instead of empty boxes
export function SkeletonGroup({ label, children, className = "" }: { label: string; children: React.ReactNode; className?: string }) {
  return (
    <div className={`pj-skeleton-group ${className}`.trim()} role="status" aria-live="polite" aria-busy="true">
      <span className="pj-visually-hidden">{label}</span>
      {children}
    </div>
  );
}

// Placeholder for a list of Jello Jobs style rows
export function SkeletonRows({ count = 3, height = 75, label = "Loading" }: { count?: number; height?: number; label?: string }) {
  return (
    <SkeletonGroup label={label} className="pj-skeleton-rows">
      {Array.from({ length: count }, (_, i) => (
        <Skeleton key={i} height={height} radius="var(--radius-card)" />
      ))}
    </SkeletonGroup>
  );
}

// Placeholder for a grid of cards (stat cards, worker cards)
export function SkeletonCards({ count = 4, height = 150, label = "Loading", columns = 4 }: { count?: number; height?: number; label?: string; columns?: number }) {
  return (
    <SkeletonGroup label={label} className="pj-skeleton-cards">
      <div className="pj-skeleton-cards__grid" style={{ gridTemplateColumns: `repeat(${columns}, minmax(0, 1fr))` }}>
        {Array.from({ length: count }, (_, i) => (
          <Skeleton key={i} height={height} radius="var(--radius-card)" />
        ))}
      </div>
    </SkeletonGroup>
  );
}
