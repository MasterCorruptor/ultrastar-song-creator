# Fase 3.1 – lokal mediekilde og metadata

Bestilt 2026-10-11. Autoritet: PROJECT_MASTER.md §3–5, §9–10, §15–18 og §22, aksepterte ADR-0001/0002 og prosjekteierens avklaringer.

## Mål og kontekst

Legg til den minste sammenhengende Acquisition-leveransen: inspeksjon av en lokal lyd-/videofil og et normalisert Song-utkast med varig kildemetadata. Eksisterende Core, Projects og UltraStar skal fortsatt fungere. Produktet har ennå ikke bruker-GUI; Fase 1-prober er tekniske demonstrasjoner.

Prosjekteieren valgte referanse til originalfilen, filmetadata og eksplisitt manuell overstyring. Ingen kopiering til en prosjektmediemappe inngår; originalfilen må fortsatt finnes.

## Scope og kontrakt

- Separat Acquisition-modul med utskiftbart IMediaSource og IMediaProbe.
- FFprobe som eksplisitt konfigurert separat prosess, basert på allerede evaluert minimal codecprofil; ingen PATH-gjetting eller automatisk nedlasting.
- Lokal filinspeksjon: container, audio/video-streams, varighet og tilgjengelige tags. Kontrollert prosessgrense med argumentliste, timeout, cancellation og outputgrenser.
- Song-utkast med sekunder, tomme noter/analyse og aktuelle artist/tittel/språk. Manglende metadata gir et redigerbart utkast; navn/artist utledes ikke fra filnavnet.
- Entydig stream-valg eller eksplisitt indeks; ved flere spor velges ingen lyd/versjon stille.
- Opprinnelig teknisk/tag-proveniens lagres gjennom eksisterende generisk SourceDocument og prosjektformat v2, uten schemaendring. Gjeldende metadata har forrang ved gjenåpning.
- Eksport kjenner denne dokumenterte lokale source-profilen, beholder proveniens i prosjektet og bruker gjeldende sangdata/eksplisitt grid ved eksport.
- Kort dokumentasjon om dagens testmuligheter og fravær av en alpha/beta-app.

Ingen nettsøk/download, providerkonto, lyrics/cover-ekstraksjon, dekoding/transcoding, waveform/playback, genererte noter, GUI, installer eller neste deloppgave inngår.

## Krav og akseptanse

Originalfilen endres ikke. Nettverksprotokoller/playlistformater benyttes ikke. Prosess/JSON-feil skal gi tydelige feil; cancellation avslutter prosessen. Metadata og bruker-overrides bevares ved save/load uten nye probes. Lokalt sangutkast kan etter senere noteredigering eksporteres med valgt grid. Core og historiske prosjektkontrakter/fixtures, master og aksepterte ADR-er bevares.

## Verifikasjon og leveranse

Locked restore, Release build, meningsfulle tester for metadata/streams/proveniens, save/load/eksport, prosessfeil/timeout/output/cancellation, Windows og nettisolert Linux, format/whitespace/inventar/lenker. Praktisk FFprobe-smoke på egen syntetisk lyd/video på Windows. Dokumenter nøyaktig hva som er testet; ingen UI-/codec-/releasegaranti.

Avgrenset branch/PR med kontrakt og gjennomført plan. Stopp ved uavklart lisens, autoritetskonflikt, nødvendig større schema-/teknologivalg eller scopeutvidelse; rapporter og be om innspill før slike valg.
