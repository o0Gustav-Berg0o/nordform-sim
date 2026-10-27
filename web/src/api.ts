import type { Layout, SimulationSnapshot } from "./types";

// Standardadressen när Nordform.Sim.Api körs lokalt med `dotnet run`.
export const API_BASE = import.meta.env.VITE_API_BASE ?? "http://localhost:5000";

async function getJson<T>(path: string): Promise<T> {
  const response = await fetch(`${API_BASE}${path}`);
  if (!response.ok) throw new Error(`${path}: ${response.status}`);
  return (await response.json()) as T;
}

export const listLayouts = () => getJson<string[]>("/layouts");
export const listProducts = () => getJson<string[]>("/products");
export const listScenarios = () => getJson<string[]>("/scenarios");
export const getLayout = (id: string) => getJson<Layout>(`/layouts/${id}`);

export interface StartSimulationRequest {
  layoutId: string;
  productsId: string;
  scenarioId: string;
  strategy: string;
  speed?: number;
}

export async function startSimulation(req: StartSimulationRequest): Promise<string> {
  const response = await fetch(`${API_BASE}/simulations`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(req),
  });
  if (!response.ok) throw new Error(`Kunde inte starta simuleringen (${response.status})`);
  const body = (await response.json()) as { sessionId: string };
  return body.sessionId;
}

export async function stopSimulation(sessionId: string): Promise<void> {
  await fetch(`${API_BASE}/simulations/${sessionId}/stop`, { method: "POST" });
}

export const getSnapshot = (sessionId: string) =>
  getJson<SimulationSnapshot>(`/simulations/${sessionId}`);
