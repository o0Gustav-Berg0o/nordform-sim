# Pull requests: dokumentation och mall

Det här dokumentet visar hur en pull request beskrivs i projektet. Det innehåller dels en mall att kopiera, dels en ifylld beskrivning av den första ändringen (PR-0001), som ett exempel på detaljnivå.

Koppling till tavlan i [README.md](../README.md#tavla-todo): ett ärende (`NF-nnn`) blir en pull request. Titeln börjar med ID:t.

## Regler

1. En pull request gör en sak och är liten nog att granska på en kvart.
2. Titel: `NF-nnn: kort beskrivning i presens`.
3. Beskriv varför ändringen behövs, inte bara vad som ändrades.
4. Ange hur ändringen testades, med kommandon som någon annan kan köra.
5. Ändrar den mätvärden eller simuleringens beteende ska före- och eftersiffror för samma seed anges.
6. Flytta ärendet på tavlan och kryssa i det när pull requesten är sammanfogad.

## Mall

````markdown
## NF-nnn: titel

### Sammanfattning
Två till tre meningar om vad ändringen gör.

### Motivering
Vilket problem löser den, och varför nu? Länka ärendet på tavlan.

### Ändringar
- Vad som lagts till, ändrats eller tagits bort, per område (core, data, test och så vidare)

### Så testar du
```bash
dotnet test
dotnet run --project src/Nordform.Sim.Cli
```

### Resultat
Före och efter för samma seed om mätvärden påverkas, annars "Ingen påverkan på resultat".

| Mått | Före | Efter |
|---|---|---|
| Tomkörning | | |
| Snittväntan | | |

### Risker och begränsningar
Vad kan gå fel, vad ingår inte, vad är känt men uppskjutet.

### Checklista
- [ ] Bygger utan varningar
- [ ] Alla tester passerar
- [ ] Nya tester för ny logik
- [ ] Samma seed ger samma resultat
- [ ] Tavlan i README är uppdaterad
- [ ] Ingen verklig kunddata i ändringen
````

---

## PR-0001: Initial lösningsstruktur

Ärenden på tavlan: NF-002, NF-003, NF-004.

### Sammanfattning

Skapar repostrukturen för simuleringen: en .NET-lösning med kärna, kommandorad, API och tester, dataformat för layout, produkter och scenarier, samt prototypen av kartan. Kärnan innehåller kortaste väg över korridornätet och tre utbytbara planeringsmetoder.

### Motivering

Prototypen av kartan bevisade att idén fungerar, men den ligger i en enda HTML-fil och går inte att testa eller utvidga. För att kunna jämföra planeringsmetoder reproducerbart och låta andra skriva egna metoder behövs en kärna som är skild från gränssnittet, med dataformat som kan bytas ut. Det här är grunden för resten av tavlan.

### Ändringar

**core**
- `Models.cs`: `Layout`, `Node`, `Edge`, `Station`, `Product`, `RoutingStep`, `Scenario`, `TransportJob`, `TruckView`.
- `PathFinder`: Dijkstra över korridornätet, med stöd för blockerade segment.
- `IDispatchStrategy` med `FifoStrategy`, `NearestStrategy` och `UrgentThenNearestStrategy`.
- `JsonData`: läser layout-, produkt- och scenariofiler.

**cli och api**
- `Nordform.Sim.Cli`: läser en layout och skriver ut grunddata.
- `Nordform.Sim.Api`: `/health` och `/strategies`.

**data**
- `data/layouts/furniture-line.json`: 13 stationer, korridornät med omvägar och parkering.
- `data/products/furniture-products.json`: stol, bord och obehandlad bokhylla med olika arbetsplaner.
- `data/scenarios/`: normal dag och hög belastning, båda med seed 4217.

**test**
- `PathFinderTests`: rak väg, omväg vid blockering, onåbar nod, okänd nod.
- `StrategyTests`: äldsta först, närmaste, brådskande först, inga uppdrag.
- `DataFileTests`: filerna går att läsa, hela nätet är sammanhängande, alla arbetsplaner pekar på stationstyper som finns, produktmixen summerar till 1.

**prototype**
- `prototype/nordform-map.html`: kartprototypen, oförändrad.

### Så testar du

```bash
dotnet build
dotnet test
dotnet run --project src/Nordform.Sim.Cli
```

Förväntad utskrift från kommandoraden: layoutens namn, 13 stationer och ett avstånd mellan `GODS` och `FARD`.

### Resultat

Ingen påverkan på resultat. Simuleringsmotorn finns ännu inte i kärnan (NF-010), så inga mätvärden räknas av .NET-koden.

### Risker och begränsningar

- **Inte byggd ännu.** Strukturen skrevs utan tillgång till .NET SDK, så `dotnet build` och `dotnet test` är inte körda. Rätta eventuella kompileringsfel i NF-005 innan något annat byggs ovanpå. Paketversionerna för testerna (`xunit` 2.9.3, `xunit.runner.visualstudio` 3.1.0, `Microsoft.NET.Test.Sdk` 17.14.1) bör kontrolleras mot aktuella versioner.
- Prototypens simulering (JavaScript) och den kommande .NET-motorn kan ge olika resultat. Värdena ska inte jämföras förrän NF-010 är klar.
- Layoutfilen har en enhet som motsvarar 0,1 m. Det är ett antagande och inte uppmätt.
- Planeringsstrategierna känner inte till trafik eller att flera truckar kan konkurrera om samma uppdrag. Det hanteras av motorn.

### Checklista

- [x] Bygger utan varningar (verifierat i NF-005)
- [x] Alla tester passerar (verifierat i NF-005)
- [x] Nya tester för ny logik
- [x] Samma seed ger samma resultat (gäller dataformaten, motorn kommer i NF-010)
- [x] Tavlan i README är uppdaterad
- [x] Ingen verklig kunddata i ändringen

---

## PR-0002: Simuleringsmotor i C#

Ärenden på tavlan: NF-010, NF-011, NF-012, NF-015.

### Sammanfattning

Porterar simuleringen från webbprototypen till kärnan i C#. Motorn är deterministisk, läser layout, produkter och scenario från JSON och använder en `IDispatchStrategy` för att fördela transportuppdrag. CLI:n kör nu alla tre strategier mot ett scenario och skriver en jämförelse.

### Motivering

Utan motor går inget att mäta i .NET-koden, och inga planeringsmetoder kan testas eller jämföras reproducerbart. Motorn är det som resten av tavlan (batchkörning, API, scenariotester, studenternas strategier) bygger på.

### Ändringar

**core**
- `SimulationEngine`: tidssteg, orderflöde ur produktmix och arbetsplan, val av station när flera av samma typ finns (minst last, lika last avgörs av slumpen), truckens tillståndsmaskin, mätvärden och händelselogg.
- `Rng`: mulberry32, samma talföljd som prototypen oavsett .NET-version.
- `Layout` har fått fältet `ParkingNodes`. Truckar parkerar efter 45 sekunder utan uppdrag.
- `EngineOptions`: truckhastighet, lastningstid, parkeringstid, tidssteg, andel brådskande order.

**data**
- `furniture-line.json`: `parkingNodes` (P0 till P4).

**cli**
- Kör `Fifo`, `Nearest` och `UrgentThenNearest` på samma scenario och skriver en tabell.

**test**
- `RngTests`: samma talföljd som prototypen för seed 4217, determinism, intervall.
- `EngineTests`: samma seed ger samma mätvärden, annan seed ger annat utfall, rimliga mätvärden, pallar bevaras och WIP summerar rätt, inga två truckar delar uppdrag, alla strategier levererar, blockerad korridor ger omväg och ändrad sträcka, saknad stationstyp ger tydligt fel, händelser loggas.

### Så testar du

```bash
dotnet build
dotnet test
dotnet run --project src/Nordform.Sim.Cli
dotnet run --project src/Nordform.Sim.Cli data data/scenarios/high-load.json
```

### Resultat

Motorn är ny, så det finns inga tidigare siffror att jämföra mot. Utskrift från `dotnet run --project src/Nordform.Sim.Cli` (scenario Normal dag, seed 4217, 30 min simulerad tid):

| Metod | Tomkörning | Snittväntan | Order | Sträcka |
|---|---|---|---|---|
| Äldsta uppdrag först | 66 % | 11 s | 25 | 18,00 km |
| Närmaste uppdrag | 65 % | 10 s | 26 | 18,15 km |
| Brådskande först | 65 % | 10 s | 26 | 18,15 km |

### Risker och begränsningar

- Resultaten är inte identiska med webbprototypen. Motorn drar ett extra slumptal för produktval och stationsval, så talföljden hamnar i annan ordning. Jämförelsen mot prototypen är NF-016.
- Trucken följer inga trafikregler och kolliderar inte med andra. Två truckar kan stå på samma nod.
- Stationer har obegränsad kapacitet och ingen omställningstid ännu (NF-013).
- En truck som är på väg till parkering och tilldelas ett uppdrag räknas från nästa nod på vägen i strategin, inte från sin exakta position.

### Checklista

- [x] Bygger utan varningar (verifierat i NF-005: `dotnet build`, 0 varningar, 0 fel)
- [x] Alla tester passerar (verifierat i NF-005: `dotnet test`, 25 av 25 gröna)
- [x] Nya tester för ny logik
- [x] Samma seed ger samma resultat (testat i `EngineTests`)
- [x] Tavlan i README är uppdaterad
- [x] Ingen verklig kunddata i ändringen

---

## PR-0003: Jämförelse motor mot webbprototyp (NF-016)

Ärenden på tavlan: NF-016.

### Sammanfattning

Jämför `SimulationEngine`:s mätvärden med webbprototypens, samma seed (4217), scenario (Normal dag) och planeringsmetoder. Ingen kodändring: det här är dokumentation av en jämförelse och en förklaring av varför talen inte är identiska.

### Motivering

NF-010 porterade motorn från prototypen, men de två implementationerna har aldrig körts sida vid sida. Innan motorn blir den källa som används för att jämföra planeringsmetoder måste skillnaden mot prototypen vara förklarad, så att den inte förväxlas med en bugg.

### Ändringar

- Ingen kod. Körresultat och förklaring tillagda här i docs/PR-README.md, tavlan i README.md uppdaterad.

### Så testar du

```bash
dotnet run --project src/Nordform.Sim.Cli
```

Öppna `prototype/nordform-map.html`, välj scenario "Normal dag" och klicka "Kör alla tre (30 min)" under "Jämför planering".

### Resultat

Seed 4217, scenario Normal dag, 30 min simulerad tid, samma tre strategier.

**Webbprototyp** (`prototype/nordform-map.html`):

| Metod | Tomkörning | Snittväntan | Order | Sträcka |
|---|---|---|---|---|
| Äldsta först | 63 % | 9 s | 26 | 17,92 km |
| Närmaste | 63 % | 9 s | 25 | 17,60 km |
| Brådskande först | 63 % | 9 s | 25 | 17,91 km |

**Motor** (`dotnet run --project src/Nordform.Sim.Cli`, se även PR-0002):

| Metod | Tomkörning | Snittväntan | Order | Sträcka |
|---|---|---|---|---|
| Äldsta uppdrag först | 66 % | 11 s | 25 | 18,00 km |
| Närmaste uppdrag | 65 % | 10 s | 26 | 18,15 km |
| Brådskande först | 65 % | 10 s | 26 | 18,15 km |

Talen ligger nära varandra (tomkörning inom 3 procentenheter, sträcka inom 2 %) men är inte identiska, trots samma seed. Det beror på att de två implementationerna drar slumptal i olika ordning:

- **En extra dragning per order.** Prototypen har bara ett implicit produktflöde och drar ett tal per skapad order (brådskande eller inte). Motorn stödjer flera produkter med egna arbetsplaner (`furniture-products.json`) och drar först ett tal för att välja produkt (`PickProduct`), sedan ett för brådskande. Den extra dragningen skjuter alla efterföljande tal i talföljden ett steg, för varje order som skapas under hela körningen.
- **Samma mekanik för stationsval vid lika last.** Både motor och prototyp drar ett tal när Montering A och B har lika last (`ChooseStation` respektive `pickMon`), men eftersom ordningen redan har glidit isär av punkten ovan hamnar även de dragningarna på olika ställen i talföljden.
- Nettoeffekten är att varje order kan bli brådskande eller inte, få en annan behandlingstid, och ett annat val mellan Montering A/B, jämfört med prototypen för "samma" order-nummer. Över en 30-minuterskörning med dussintals order blir det små men synliga skillnader i de sammanlagda mätvärdena, utan att någon av implementationerna är fel.
- Determinism håller inom varje implementation för sig: samma seed ger samma resultat i motorn (se `EngineTests`) och i prototypen.

### Risker och begränsningar

- Prototypens mätvärden lästes av manuellt i webbläsaren och är inte automatiserat testade. En framtida regressionsbaslinje (NF-032) bör utgå från motorns egna siffror, inte prototypens.
- Skillnaden i dragordning är inte buggfixad här. Att synka talföljderna exakt (t.ex. genom att alltid dra produktval även när bara en produkt finns) skulle göra jämförelsen enklare men är inte värt att låsa fast motorns design efter prototypens begränsningar.

### Checklista

- [x] Bygger utan varningar
- [x] Alla tester passerar
- [x] Nya tester för ny logik (ej tillämpligt, ingen kodändring)
- [x] Samma seed ger samma resultat
- [x] Tavlan i README är uppdaterad
- [x] Ingen verklig kunddata i ändringen

---

## PR-0004: Batchläge i CLI:n med CSV och JSON (NF-040)

Ärenden på tavlan: NF-040.

### Sammanfattning

Lägger till ett batchläge i CLI:n: `dotnet run --project src/Nordform.Sim.Cli -- batch [datamapp] [utmapp]` kör alla planeringsstrategier mot alla scenariofiler i datamappen, för tre fasta seeds (4217, 1000, 2000), och skriver resultatet till `batch-results.csv` och `batch-results.json` i utmappen. Det vanliga jämförelseläget (en tabell på konsolen för ett scenario) är oförändrat.

### Motivering

Jämförelsetabellen på konsolen räcker för en enda körning, men för att jämföra planeringsmetoder och scenarier systematiskt (och senare kunna bygga en regressionsbaslinje, NF-032) behövs resultat i ett format som går att läsa in i ett kalkylblad eller ett annat skript. CSV och JSON täcker båda användningarna.

### Ändringar

**cli**
- `BatchRunner.cs`: kör strategier × alla scenariofiler i `data/scenarios` × tre seeds, samlar resultatet i `BatchRow`-poster, och skriver CSV respektive JSON.
- `Program.cs`: nytt grenval `batch [datamapp] [utmapp]` före det befintliga jämförelseläget, som är oförändrat.

**test**
- Nytt testprojekt `Nordform.Sim.Cli.Tests`, tillagt i `Nordform.Sim.slnx`.
- `BatchRunnerTests`: rätt antal rader (scenarier × 3 seeds × 3 strategier), samma seed ger samma resultat igen, CSV och JSON går att skriva och läsa tillbaka.

### Så testar du

```bash
dotnet build
dotnet test
dotnet run --project src/Nordform.Sim.Cli -- batch data out
```

Förväntat resultat: `out/batch-results.csv` och `out/batch-results.json` skapas, en rad per kombination av scenario, seed och strategi (2 scenarier × 3 seeds × 3 strategier = 18 rader för datamappen i repot). `out/` är redan i `.gitignore`.

### Resultat

Ingen påverkan på simuleringens mätvärden. Det vanliga jämförelseläget ger samma utskrift som i PR-0002 och PR-0003 för seed 4217.

### Risker och begränsningar

- Seeds är hårdkodade (4217, 1000, 2000) i `BatchRunner`, inte ett CLI-argument. Räcker för att se spridning mellan seeds, men en framtida regressionsbaslinje (NF-032) kan behöva fler eller konfigurerbara seeds.
- Batchkörningen håller hela resultatet i minnet innan den skriver filerna. Inget problem vid den här skalan (scenarier × 3 seeds × 3 strategier), men kan behöva strömmas om antalet scenarier växer mycket (NF-070).
- Ingen progressutskrift under körningen. Vid många scenarier kan CLI:n se ut att hänga.

### Checklista

- [x] Bygger utan varningar
- [x] Alla tester passerar
- [x] Nya tester för ny logik
- [x] Samma seed ger samma resultat
- [x] Tavlan i README är uppdaterad
- [x] Ingen verklig kunddata i ändringen
