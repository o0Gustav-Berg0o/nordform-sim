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

- [ ] Bygger utan varningar (ej verifierat, se NF-005)
- [ ] Alla tester passerar (ej verifierat, se NF-005)
- [x] Nya tester för ny logik
- [x] Samma seed ger samma resultat (gäller dataformaten, motorn kommer i NF-010)
- [x] Tavlan i README är uppdaterad
- [x] Ingen verklig kunddata i ändringen
