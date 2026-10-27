import { useEffect, useRef, useState } from "react";
import * as signalR from "@microsoft/signalr";
import { API_BASE, getSnapshot } from "./api";
import type { SimulationSnapshot } from "./types";

/// Ansluter till /hub/simulation (NF-050) och håller det senaste ögonblicket för en session.
/// Läser också ett ögonblick direkt via HTTP innan strömmen hinner skicka något, så kartan
/// inte står tom under de första sekunderna.
export function useSimulationStream(sessionId: string | null) {
  const [snapshot, setSnapshot] = useState<SimulationSnapshot | null>(null);
  const [ended, setEnded] = useState(false);
  const connectionRef = useRef<signalR.HubConnection | null>(null);

  useEffect(() => {
    setSnapshot(null);
    setEnded(false);
    if (!sessionId) return;

    getSnapshot(sessionId)
      .then(setSnapshot)
      .catch(() => {
        /* sessionen kan redan ha avslutats innan första läsningen hinner fram */
      });

    const connection = new signalR.HubConnectionBuilder()
      .withUrl(`${API_BASE}/hub/simulation`)
      .withAutomaticReconnect()
      .build();

    connection.on("state", (next: SimulationSnapshot) => setSnapshot(next));
    connection.on("ended", () => setEnded(true));

    connection
      .start()
      .then(() => connection.invoke("JoinSession", sessionId))
      .catch((err) => console.error("Kunde inte ansluta till simuleringsströmmen", err));

    connectionRef.current = connection;
    return () => {
      connection.stop();
      connectionRef.current = null;
    };
  }, [sessionId]);

  return { snapshot, ended };
}
