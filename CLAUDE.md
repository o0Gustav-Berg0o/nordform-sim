# CLAUDE.md

Instruktioner för Claude Code i det här repot. Läs README.md för översikt och tavla, och docs/PR-README.md för hur pull requests beskrivs.

## Projektet

Nordform Sim är en deterministisk simulering av internlogistik (truckar som flyttar pallar mellan produktionssteg) i en fiktiv möbelfabrik. Det är en testbädd där planeringsmetoder (`IDispatchStrategy`) kan jämföras på samma scenarier. Underlag för ett examensarbete.

## Språk och stil

- Skriv svar, README, dokumentation, commit-meddelanden och kommentarer på svenska. Kod, typer och metodnamn på engelska.
- Direkt och kortfattat. Inga emojis.
- Markdown som fungerar i GitHub och Obsidian.

## Kommandon

```bash
dotnet build
dotnet test
dotnet run --project src/Nordform.Sim.Cli
dotnet run --project src/Nordform.Sim.Cli data data/scenarios/high-load.json
dotnet run --project src/Nordform.Sim.Api
```

Prototypen `prototype/nordform-map.html` körs direkt i webbläsaren, utan bygge.

## Regler för koden

- Tekniken är .NET 10 och C#. Kärnan (`Nordform.Sim.Core`) får inte känna till UI, API eller filsystem utöver `JsonData`.
- Allt slumpmässigt går genom `Rng` med scenariots seed. Använd aldrig `Random`, `Guid.NewGuid` eller `DateTime.Now` i kärnan. Samma seed ska alltid ge samma resultat.
- Nya planeringsmetoder läggs till genom att implementera `IDispatchStrategy`. Motorn ska inte behöva ändras.
- Layout, produkter och scenarier ligger i `data/` som JSON. Hårdkoda inga stationer eller arbetsplaner i koden.
- Arbetsplaner pekar på stationstyper (`saw`, `paint`, `assembly`), inte på enskilda stationer.
- Data i repot ska vara syntetisk. Lägg aldrig in verkliga kunduppgifter eller kundnamn.

## Arbetssätt

- Arbeta iterativt och verifiera varje steg: bygg och kör testerna efter varje ändring och rätta det som går sönder innan du går vidare.
- Nya funktioner ska ha tester. Ändringar som påverkar mätvärden ska redovisa före- och eftersiffror för samma seed.
- Tavlan i README.md är projektets issue-lista. Ett ärende (`NF-nnn`) blir en gren (`nf-010-simuleringsmotor`) och en pull request med ID i titeln. Flytta ärendet på tavlan och kryssa i det när det är klart.
- Beskriv pull requests enligt mallen i docs/PR-README.md. Lägg till ett avsnitt `PR-nnnn` där när en ny PR görs.
- Små pull requests som gör en sak.
- Fråga innan du pushar till GitHub eller gör något som inte går att ångra.

## Nuläge

- NF-005 är klart: `dotnet build` och `dotnet test` är gröna (0 fel, 0 varningar, 25 av 25 tester), CLI:n körd och resultattabellen i PR-0002 ifylld.
- NF-016 är klart: motorns resultat jämfört med webbprototypen för samma seed (4217, Normal dag). Talen ligger nära men är inte identiska eftersom motorn drar ett extra slumptal per order (produktval), vilket skjuter talföljden. Förklaringen finns i PR-0003.
- Nästa uppgift på tavlan är NF-040 (batchrunner för CSV/JSON).

## Känt och uppskjutet

- Truckar kolliderar inte och följer inga trafikregler.
- Stationer har obegränsad kapacitet och ingen omställningstid (NF-013).
- En truck på väg till parkering räknas i strategin från nästa nod på vägen, inte från sin exakta position.
