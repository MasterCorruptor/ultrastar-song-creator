# Fase 2.4 – eksportverifikasjon

Kjørt 2026-10-08. Syntetiske filer og ord, ingen innhentet sangtekst/musikk/cover. Private kilde-/outputmapper og binærer publiseres ikke.

## Automatiserte tester

| Kontroll | Windows | Linux, nettisolert |
| --- | --- | --- |
| SDK | 10.0.401 | 10.0.401 |
| Locked restore | bestod | bestod, eksisterende NuGet-cache |
| Release build | 0 warnings/errors | 0 warnings/errors |
| Tester | 263/263 | 263/263 |
| Formatkontroll | bestod | samme formatterte kilder |

38 nye writer-tester dekker uavhengig forventet tekst, begge profiler, import/export-import fra normal/v1/relativ kilde, gjeldende brukerendringer/metadata, originalproveniens/analysebevaring, midpoint/endpoint-avrunding og deltas, collapsed notes, grid/medley, Unicode/kultur/eksponentfri desimal, feil og filgrenser.

22 pakketester dekker main audio/video og cover/background/vocals/instrumental, checksums/byteidentitet, canonical path dedup, case-insensitive kollisjoner, lokale referansebaser/backslash, URL/missing-file-avvisning, bevaring av eksisterende mappe/fil, mid-copy cancellation, injected copy-stage failure, target-race, relocation og prosjekt-save/load/edit/export uten mutasjon. Reserved Windows-filenavn testes fra Linux; Windows kan ikke opprette den ugyldige kildefilen.

Linux bruker egne artifacts under .agent-local/phase2-export, samme bindede prosjektkilde og ingen nettverk. Audit ble deaktivert bare under offline restore; ordinær restore beholder audit. Ingen hosted CI påstås; eksisterende CI-mal er fortsatt ikke aktiv med dagens workflow-tilgang.

## Praktisk Windows-smoke

Kilder: 3,5 s mono PCM16 WAV ved 8000 Hz med syntetisk C4-tone, eget 1×1 PNG-bilde og 64×64 blå MPEG4-video ved 10 fps. MPEG4 ble generert med det eksisterende private FFmpeg 8.1 evalueringsverktøyet. Produktets exporter hverken bruker eller distribuerer FFmpeg.

V1 absolute package: to noter og tre assets. Tekst var UTF-8 uten BOM, bilder/audio/video hadde relative referanser, import ga teksten La la, og alle source-filer var bevart. FFmpeg kunne dekode de tre kopierte mediene uten feil.

| Relativ asset | Bytes | SHA-256 |
| --- | ---: | --- |
| media/tone.wav | 56044 | bf82b03e1f8152e7ccaef1424975eb834a16991f72294e2d0bb2354006a44d43 |
| media/color.mp4 | 1943 | 850c50695bea452c7584ffa22003a8df67bdeb6546006f61a3986e0b12a9fe04 |
| media/cover.png | 69 | fdf040aaee9270aef6cf03752d51615cf6025c72081820fe0945f0c9c9cbdd9c |

Begge noter ble avrundet ved GridBpm=120 (0,125 s/beat): første note fra start 0,07/varighet 0,44 til start 0,125/varighet 0,375; andre fra 1,02/0,48 til 1,0/0,5. GAP=250 ms, VIDEOGAP=0,25 s og cover/album ble bevart i generert pakke. Deltas var tilgjengelige i den strukturerte eksport-rapporten.

Smoke-runner/source/output ligger bare lokalt under .agent-local/phase2-export. Prøven verifiserer tekst, tidskonvertering, kopi, checksums og gyldig media-dekoding; en separat karaokeapp eller GUI er ikke kjørt. Ingen kvalitetsløfter om brukermedier/codecs følger av dette.

## Integritet og scope

Master, aksepterte ADR-er, Core, Projects og historisk v1-fixture er byteuendret fra Fase 2.3-baseline. Ingen dependency-/project-schema-endring, lyd/model/cachepublisering eller hardwareinformasjon. Full PR-whitespacekontroll, lokale dokumentlenker og public inventory/privacy er kontrollert før publisering. Se [eksportkontrakten](ULTRASTAR_EXPORT.md).
