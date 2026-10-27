// Speglar JSON-formen från Nordform.Sim.Core (System.Text.Json med Web-standarder, dvs camelCase).

export interface LayoutNode {
  id: string;
  x: number;
  y: number;
}

export interface LayoutEdge {
  from: string;
  to: string;
  blocked: boolean;
}

export interface LayoutStation {
  id: string;
  name: string;
  kind: string;
  node: string;
  processSeconds: number;
  capacity: number | null;
  changeoverSeconds: number;
}

export interface Layout {
  name: string;
  nodes: LayoutNode[];
  edges: LayoutEdge[];
  stations: LayoutStation[];
  metersPerUnit: number;
  parkingNodes: string[] | null;
}

export type TruckState = "Idle" | "Parking" | "ToPickup" | "Loading" | "ToDrop" | "Unloading";

export interface TruckSnapshot {
  id: number;
  x: number;
  y: number;
  node: string;
  state: TruckState;
  jobId: number | null;
}

export interface SimulationMetrics {
  simulatedSeconds: number;
  emptyPercent: number;
  averageWaitSeconds: number;
  distanceMeters: number;
  shipped: number;
  completedTransports: number;
  queuedJobs: number;
  utilizationPercent: number;
}

export interface SimulationSnapshot {
  time: number;
  trucks: TruckSnapshot[];
  metrics: SimulationMetrics;
}

export const STRATEGIES = [
  { id: "fifo", label: "Äldsta först" },
  { id: "nearest", label: "Närmaste uppdrag" },
  { id: "urgent", label: "Brådskande först, sedan närmast" },
] as const;
