import React from "react";
import "./JelloItem.css";
import { useNavigate } from "react-router-dom";

interface JelloItemProps {
    name: string;
    clientName: string;
    dueDate: string;
    workers: number;
    onClick?: () => void;
    /** Called on hover or focus, to load the project before it's opened */
    onPrefetch?: () => void;
}

const formatDueDate = (value: string) => {
    if (!value) return "No due date";
    const parsed = new Date(value);
    if (isNaN(parsed.getTime())) return value;
    return parsed.toLocaleDateString(undefined, {
        month: "short",
        day: "numeric",
        year: "numeric",
    });
};

export default function JelloItem({
    name,
    clientName,
    dueDate,
    workers,
    onClick,
    onPrefetch,
}: JelloItemProps) {
    const navigate = useNavigate();
    return (
        <div
            className="JelloItem clickable"
            onClick={onClick || (() => navigate("/single-view"))}
            onMouseEnter={onPrefetch}
            onFocus={onPrefetch}
            tabIndex={0}
            role="button"
            aria-label={`${name}, ${clientName}, due ${formatDueDate(dueDate)}, ${workers} ${workers === 1 ? "worker" : "workers"}`}
            onKeyDown={e => {
                if (e.key === "Enter" || e.key === " ") (onClick ? onClick() : navigate("/single-view"));
            }}
        >
            <div className="ItemNames">
                <h5>{name}</h5>
                <p className="Item_ClientName">{clientName}</p>
            </div>

            <div className="JelloInfo">
                <div className="Item_Date"> <h5>{formatDueDate(dueDate)}</h5> </div>
                <div className="Item_ActiveWorkers"> <h5>{workers} {workers === 1 ? "Worker" : "Workers"}</h5> </div>
            </div>
        </div>
    );
}