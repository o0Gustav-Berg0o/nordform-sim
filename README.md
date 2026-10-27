# Nordform Sim

Testbädd och simulering för effektivare internlogistik i tillverkande industri. Truckar flyttar pallar mellan produktionssteg (work in progress, WIP) i en **fiktiv möbelfabrik, Nordform Möbler**. All data är syntetisk, så ingenting här är kunddata.

Projektet är tänkt som underlag för ett examensarbete och som en testbädd där olika planeringsmetoder kan jämföras på samma scenarier.

Ägare: Gustav Berg, XLENT GävleDala

## Innehåll

- [Mål](#mål)
- [Snabbstart](#snabbstart)
- [Teknikstack](#teknikstack)
- [Struktur](#struktur)
- [Dataformat](#dataformat)
- [Tavla (todo)](#tavla-todo)
- [Arbetssätt](#arbetssätt)

## Mål

1. Simulera en fabrik med flera produktionssteg, där olika produkter har olika arbetsplaner.
2. Låta utbytbara planeringsmetoder (`IDispatchStrategy`) fördela transportuppdrag mellan truckar.
3. Mäta resultaten: körsträcka, tomkörning, väntetid, kö, utnyttjande och levererade order.
4. Köra deterministiskt: samma seed ger samma resultat, så att jämförelser och regressionstester går att lita på.
5. Visa det hela grafiskt på en karta.

## Snabbstart

Kräver [.NET 10 SDK](https://dotnet.microsoft.com/download).

```bash
dotnet build
dotnet test
dotnet run --project src/Nordform.Sim.Cli            # läser layouten och visar grunddata
dotnet run --project src/Nordform.Sim.Api            # /health och /strategies
```

Prototypen av kartan körs direkt i webbläsaren, utan bygge: öppna `prototype/nordform-map.html`.

> Status: strukturen är skapad utan tillgång till .NET SDK, så `dotnet build` och `dotnet test` är ännu inte körda. Det är första punkten på tavlan (NF-005).

## Teknikstack

| Lager | Teknik |
|---|---|
| Simuleringskärna | C# / .NET 10, class library utan UI-beroenden |
| Planeringsmetoder | Gränssnittet `IDispatchStrategy`, senare Google OR-Tools |
| Test | xUnit, senare FsCheck för egenskapstester och JSON-scenarier |
| API | ASP.NET Core minimal API, senare SignalR för live-ström |
| Frontend | React eller Angular med SVG eller Canvas (prototypen är ren SVG) |
| Lagring | SQLite eller SQL Server för scenarier och körresultat (senare) |
| CI och drift | Azure DevOps eller GitHub Actions, Azure Container Apps för demon |

## Struktur

```
Nordform.Sim.slnx
src/
  Nordform.Sim.Core/     Modeller, PathFinder, planeringsstrategier
  Nordform.Sim.Cli/      Kommandorad och batchkörning (kommer)
  Nordform.Sim.Api/      Web API och live-ström (kommer)
tests/
  Nordform.Sim.Core.Tests/
data/
  layouts/               Fabrikskartor (noder, korridorer, stationer)
  products/              Produkter och arbetsplaner
  scenarios/             Orderflöde, flotta, seed
prototype/               Webbprototyp av kartan (ren HTML, JavaScript och SVG)
docs/
  PR-README.md           PR-liknande dokumentation och mall
```

Kärnan känner inte till UI eller API. Webbgränssnittet är bara en visare ovanpå samma kärna.

## Dataformat

Tre filer beskriver en körning, så att layout, produkter och scenario kan kombineras fritt.

| Fil | Innehåll |
|---|---|
| `data/layouts/*.json` | Noder, korridorsegment, stationer med typ, dockningsnod och processtid |
| `data/products/*.json` | Produkter med arbetsplan, uttryckt som stationstyper (`saw`, `paint`, `assembly` och så vidare) |
| `data/scenarios/*.json` | Seed, antal truckar, orderintervall, varaktighet och produktmix (summerar till 1) |

Arbetsplaner pekar på stationstyper och inte på enskilda stationer. Då kan "montering" lösas av både Montering A och B, och valet blir en del av planeringen. Tester i `DataFileTests` kontrollerar att filerna går att läsa och hänger ihop.

## Tavla (todo)

Den här listan är projektets issue-tavla. Varje rad är ett ärende.

**Så fungerar den**
- ID: `NF-nnn`. Etiketter i hakparentes visar område: `core`, `strategy`, `test`, `cli`, `api`, `web`, `data`, `ci`, `docs`.
- Prioritet: P1 (nästa), P2 (snart), P3 (senare). Storlek: S (under en halv dag), M (1 till 2 dagar), L (3 dagar eller mer).
- Flytta raden mellan sektionerna när status ändras. Kryssa i rutan när den är klar och flytta den till **Klart**.
- Ett ärende ska bli en pull request. Beskriv den enligt [docs/PR-README.md](docs/PR-README.md) och ange ID i titeln, till exempel `NF-010: Simuleringsmotor`.
- Beroenden anges som `beror på NF-nnn`.

### Pågår

_Inget pågår just nu._

### Nästa (P1)

- [ ] **NF-005** `[ci]` Kör `dotnet build` och `dotnet test` och rätta det som inte kompilerar. (S)
- [ ] **NF-010** `[core]` Simuleringsmotor med fast tidssteg och deterministisk slumpgenerator, portad från prototypen. (L)
- [ ] **NF-011** `[core]` Truckens tillståndsmaskin: ledig, till hämtning, lastar, levererar, lossar, parkerar. (M, beror på NF-010)
- [ ] **NF-012** `[core]` Skapa order ur produktmix och arbetsplan, med val mellan alternativa stationer (Montering A eller B). (M, beror på NF-010)
- [ ] **NF-015** `[core]` Mätvärden: tomkörning, snittväntan, sträcka, kö, utnyttjande, levererade order. (M, beror på NF-010)

### Backlog (P2)

- [ ] **NF-013** `[core]` Stationer med kapacitet och omställningstid mellan produkter. (M, beror på NF-012)
- [ ] **NF-014** `[core]` Störningar: blockerad korridor, truckhaveri, maskinstopp. (M, beror på NF-010)
- [ ] **NF-030** `[test]` Scenariotester: situation i JSON med förväntat beteende. (M, beror på NF-010)
- [ ] **NF-031** `[test]` Egenskapstester med FsCheck, till exempel att ingen truck har två uppdrag samtidigt. (M, beror på NF-010)
- [ ] **NF-032** `[test]` Regressionsbaslinje: spara mätvärden per seed och larma vid försämring. (M, beror på NF-015)
- [ ] **NF-040** `[cli]` Batchrunner som kör strategier × scenarier × seeds och skriver CSV och JSON. (M, beror på NF-015)
- [ ] **NF-050** `[api]` SignalR-ström av simuleringstillstånd. (M, beror på NF-010)
- [ ] **NF-051** `[api]` Endpoints för att lista och läsa layouter, produkter och scenarier. (S)
- [ ] **NF-060** `[web]` Visare i React eller Angular som ritar kartan från layoutfilen. (L, beror på NF-050)
- [ ] **NF-070** `[data]` Fler kartor: cellbaserad, job shop, hub och spokes, dålig layout med avsikt. (M)
- [ ] **NF-080** `[ci]` Pipeline som bygger och kör alla tester vid varje ändring. (S, beror på NF-005)

### Senare (P3)

- [ ] **NF-020** `[strategy]` Strategi baserad på Google OR-Tools. (L, beror på NF-010)
- [ ] **NF-021** `[strategy]` Strategi som buntar uppdrag på samma väg. (M, beror på NF-010)
- [ ] **NF-061** `[web]` Layoutredigerare: flytta stationer och korridorer i webbläsaren. (L, beror på NF-060)
- [ ] **NF-062** `[web]` Jämförelsevy för körningar, med diagram över mätvärden. (M, beror på NF-040)
- [ ] **NF-071** `[data]` JSON-schema och validering av layout, produkt och scenario. (S)
- [ ] **NF-072** `[data]` Flervåningsfabrik med hissar som flaskhals. (L)
- [ ] **NF-073** `[data]` Olika trucktyper: gaffeltruck, AGV, dragtruck. (M)
- [ ] **NF-081** `[ci]` Publicera demon på Azure Container Apps. (M, beror på NF-060)
- [ ] **NF-090** `[docs]` Definiera mätvärdena formellt så att de går att använda i en uppsats. (S)
- [ ] **NF-091** `[docs]` Skript som skapar GitHub-ärenden ur den här tavlan. (S)

### Klart

- [x] **NF-001** `[web]` Webbprototyp av fabrikskartan med animerade truckar och jämförelse av tre strategier (`prototype/nordform-map.html`).
- [x] **NF-002** `[docs]` Repostruktur, README och PR-dokumentation.
- [x] **NF-003** `[data]` Första versionen av layout-, produkt- och scenarioformat för möbelfabriken.
- [x] **NF-004** `[core]` `PathFinder` (kortaste väg, blockerade segment), `IDispatchStrategy` och tre strategier, med tester.

## Arbetssätt

- En gren per ärende, med ID i namnet: `nf-010-simuleringsmotor`.
- Små pull requests som beskrivs enligt [docs/PR-README.md](docs/PR-README.md).
- Nya planeringsmetoder läggs till genom att implementera `IDispatchStrategy`. Kärnan ska inte behöva ändras.
- Allt som påverkar resultaten ska gå att reproducera med en seed.
- Data i repot ska vara syntetisk. Lägg aldrig in verkliga kunduppgifter.
