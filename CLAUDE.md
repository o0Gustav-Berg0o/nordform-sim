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
dotnet run --project src/Nordform.Sim.Cli -- batch data out
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
- NF-040 är klart: CLI:n har ett batchläge (`dotnet run --project src/Nordform.Sim.Cli -- batch`) som skriver CSV och JSON för alla scenarier × tre seeds × tre strategier. Beskrivet i PR-0004.
- NF-051 är klart: `Nordform.Sim.Api` har `/layouts`, `/products` och `/scenarios` (lista och läsa enskilt id), med skydd mot path traversal. Beskrivet i PR-0005.
- NF-080 är klart: `.github/workflows/ci.yml` kör `dotnet build` och `dotnet test` på push och pull request mot main. Beskrivet i PR-0006.
- NF-013/NF-014 är klara: `Station` kan ha `Capacity` och `ChangeoverSeconds` (default obegränsat/0, bakåtkompatibelt), och motorn har `SetStationDown` och `SetTruckBroken` utöver den sedan tidigare befintliga `SetCorridorBlocked`. Beskrivet i PR-0007. Inga datafiler i `data/` använder ännu de nya fälten.
- Tavlans P1-kö är tom. Kvar på P2 utan designval: NF-030, NF-031 (kräver NuGet-paketet FsCheck, inte installerat än), NF-032, NF-070. Kräver designval: SignalR-ström (NF-050), webbvisare (NF-060, beror på NF-050). P3 kräver i stort sett alla designval (OR-Tools, layouteditor, m.m.). Fråga användaren innan du väljer något av dessa.

## Känt och uppskjutet

- Truckar kolliderar inte och följer inga trafikregler.
- En truck på väg till parkering räknas i strategin från nästa nod på vägen, inte från sin exakta position.
- En order som aldrig får plats på en full/avstängd station köar obegränsat (inget dödlägesskydd).
