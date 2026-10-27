import type { Layout, TruckSnapshot } from "./types";

const TRUCK_COLORS: Record<TruckSnapshot["state"], string> = {
  Idle: "#94a3b8",
  Parking: "#94a3b8",
  ToPickup: "#f59e0b",
  Loading: "#f59e0b",
  ToDrop: "#2563eb",
  Unloading: "#2563eb",
};

interface Props {
  layout: Layout;
  trucks: TruckSnapshot[];
}

/// Ritar layoutens korridorer, stationer och truckar som SVG. Portat från prototypens
/// `prototype/nordform-map.html`, men läser koordinater direkt från layout-JSON:en istället
/// för att de är inbakade i sidan.
export function FactoryMap({ layout, trucks }: Props) {
  const nodeById = new Map(layout.nodes.map((n) => [n.id, n]));
  const xs = layout.nodes.map((n) => n.x);
  const ys = layout.nodes.map((n) => n.y);
  const pad = 40;
  const minX = Math.min(...xs) - pad;
  const minY = Math.min(...ys) - pad;
  const width = Math.max(...xs) - minX + pad;
  const height = Math.max(...ys) - minY + pad;

  return (
    <svg viewBox={`${minX} ${minY} ${width} ${height}`} className="factory-map" role="img" aria-label={layout.name}>
      {layout.edges.map((edge, i) => {
        const a = nodeById.get(edge.from);
        const b = nodeById.get(edge.to);
        if (!a || !b) return null;
        return (
          <line
            key={i}
            x1={a.x}
            y1={a.y}
            x2={b.x}
            y2={b.y}
            stroke={edge.blocked ? "#ef4444" : "#cbd5e1"}
            strokeWidth={edge.blocked ? 4 : 2}
            strokeDasharray={edge.blocked ? "6 4" : undefined}
          />
        );
      })}

      {layout.stations.map((station) => {
        const node = nodeById.get(station.node);
        if (!node) return null;
        return (
          <g key={station.id} transform={`translate(${node.x}, ${node.y})`}>
            <rect x={-18} y={-12} width={36} height={24} rx={4} fill="#1e293b" />
            <text y={4} textAnchor="middle" fontSize={9} fill="#f8fafc">
              {station.id}
            </text>
            <text y={22} textAnchor="middle" fontSize={7} fill="#475569">
              {station.kind}
            </text>
          </g>
        );
      })}

      {trucks.map((truck) => (
        <circle
          key={truck.id}
          cx={truck.x}
          cy={truck.y}
          r={7}
          fill={TRUCK_COLORS[truck.state]}
          stroke="#0f172a"
          strokeWidth={1.5}
        >
          <title>
            T{truck.id + 1}: {truck.state}
          </title>
        </circle>
      ))}
    </svg>
  );
}
