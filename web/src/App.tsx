import { useEffect, useState } from "react";
import { FactoryMap } from "./FactoryMap";
import { useSimulationStream } from "./useSimulationStream";
import { getLayout, listLayouts, listProducts, listScenarios, startSimulation, stopSimulation } from "./api";
import { STRATEGIES, type Layout } from "./types";

export function App() {
  const [layoutIds, setLayoutIds] = useState<string[]>([]);
  const [productIds, setProductIds] = useState<string[]>([]);
  const [scenarioIds, setScenarioIds] = useState<string[]>([]);

  const [layoutId, setLayoutId] = useState("furniture-line");
  const [productsId, setProductsId] = useState("furniture-products");
  const [scenarioId, setScenarioId] = useState("normal-day");
  const [strategy, setStrategy] = useState<string>(STRATEGIES[1].id);
  const [speed, setSpeed] = useState(20);

  const [layout, setLayout] = useState<Layout | null>(null);
  const [sessionId, setSessionId] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);

  const { snapshot, ended } = useSimulationStream(sessionId);

  useEffect(() => {
    listLayouts().then(setLayoutIds).catch(() => setError("Kunde inte nå API:et. Kör det med `dotnet run --project src/Nordform.Sim.Api`."));
    listProducts().then(setProductIds).catch(() => {});
    listScenarios().then(setScenarioIds).catch(() => {});
  }, []);

  useEffect(() => {
    getLayout(layoutId).then(setLayout).catch(() => setLayout(null));
  }, [layoutId]);

  async function handleStart() {
    setError(null);
    try {
      const id = await startSimulation({ layoutId, productsId, scenarioId, strategy, speed });
      setSessionId(id);
    } catch (e) {
      setError(e instanceof Error ? e.message : "Okänt fel");
    }
  }

  async function handleStop() {
    if (sessionId) await stopSimulation(sessionId);
    setSessionId(null);
  }

  return (
    <div className="app">
      <header>
        <h1>Nordform Sim</h1>
        <p>Live karta över en körande simulering, strömmad från API:et via SignalR (NF-050/NF-060).</p>
      </header>

      <section className="controls">
        <label>
          Layout
          <select value={layoutId} onChange={(e) => setLayoutId(e.target.value)} disabled={!!sessionId}>
            {(layoutIds.length ? layoutIds : [layoutId]).map((id) => (
              <option key={id} value={id}>{id}</option>
            ))}
          </select>
        </label>
        <label>
          Produkter
          <select value={productsId} onChange={(e) => setProductsId(e.target.value)} disabled={!!sessionId}>
            {(productIds.length ? productIds : [productsId]).map((id) => (
              <option key={id} value={id}>{id}</option>
            ))}
          </select>
        </label>
        <label>
          Scenario
          <select value={scenarioId} onChange={(e) => setScenarioId(e.target.value)} disabled={!!sessionId}>
            {(scenarioIds.length ? scenarioIds : [scenarioId]).map((id) => (
              <option key={id} value={id}>{id}</option>
            ))}
          </select>
        </label>
        <label>
          Strategi
          <select value={strategy} onChange={(e) => setStrategy(e.target.value)} disabled={!!sessionId}>
            {STRATEGIES.map((s) => (
              <option key={s.id} value={s.id}>{s.label}</option>
            ))}
          </select>
        </label>
        <label>
          Hastighet ({speed}× realtid)
          <input
            type="range"
            min={1}
            max={200}
            value={speed}
            onChange={(e) => setSpeed(Number(e.target.value))}
            disabled={!!sessionId}
          />
        </label>

        {!sessionId ? (
          <button onClick={handleStart}>Starta</button>
        ) : (
          <button onClick={handleStop}>Stoppa</button>
        )}
      </section>

      {error && <p className="error">{error}</p>}
      {ended && <p className="info">Simuleringen är klar (scenariots varaktighet nådd).</p>}

      {layout && (
        <section className="map-and-metrics">
          <FactoryMap layout={layout} trucks={snapshot?.trucks ?? []} />
          <aside className="metrics">
            <h2>{layout.name}</h2>
            {snapshot ? (
              <dl>
                <dt>Simulerad tid</dt>
                <dd>{Math.round(snapshot.time)} s</dd>
                <dt>Levererade order</dt>
                <dd>{snapshot.metrics.shipped}</dd>
                <dt>Tomkörning</dt>
                <dd>{snapshot.metrics.emptyPercent.toFixed(0)} %</dd>
                <dt>Snittväntan</dt>
                <dd>{snapshot.metrics.averageWaitSeconds.toFixed(0)} s</dd>
                <dt>Utnyttjandegrad</dt>
                <dd>{snapshot.metrics.utilizationPercent.toFixed(0)} %</dd>
              </dl>
            ) : (
              <p>Starta en simulering för att se läget.</p>
            )}
          </aside>
        </section>
      )}
    </div>
  );
}
