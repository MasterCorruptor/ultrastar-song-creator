# ADR-0002 – Permissivt mål og lisensport per artefakt

Opprettet: 2026-10-07. **Status: akseptert av prosjekteieren 2026-10-08.** MIT er bekreftet for egen prosjektkode etter Fase 1-gjennomgangen, jf. masteren §18.2. Den eksisterende LICENSE-teksten er uendret.

## Problem

Komponenter kan ha permissiv kildekodelisens mens medfølgende weights, codecs, ordlister eller ferdige binærer har andre vilkår. En enkel «MIT»-kolonne for repositoryet er derfor ikke nok til å velge eller distribuere stacken.

## Alternativer

1. Beholde MIT for egen kode og velge individuelt vurderte tredjepartsartefakter, med eksplisitte notices og distribusjonsvilkår.
2. Kreve utelukkende permissive artefakter i hele installasjonspakken, selv hvis det begrenser media/GUI-funksjoner.
3. Godta GPL-/AGPL-/noncommercial-komponenter og endre prosjektets distribusjons-/lisenspremisser ved eget vedtak.

## Akseptert løsning

Bruk **MIT for prosjektets egen kode**, artefaktvise tredjepartslisenser og en separat dokumentert LGPL-codecprofil med nødvendige source/notices og utskiftbarhetskrav. Lisensstrategien er godkjent; hver faktisk distribuert dependency/modell/binær krever fortsatt manifest og artefaktspesifikk compliance. Dette er ikke automatisk godkjenning av enhver LGPL/GPL-kombinasjon.

Velg permissiv kilde/wheel-distribusjon der den er verifisert, for eksempel yt-dlp-wheel, fremfor å anta at standalone exe har samme lisens. Ikke kopier GPL-editor-/AGPL-analysekode inn i egen planlagt MIT-kodebase. Ikke innfør noncommercial eller gated weights som obligatorisk kjerne.

Uavklarte modellvilkår blokkerer det aktuelle modellvalget. Database-/providerlisens klarerer ikke copyright til song lyrics, media eller cover. Innhentet innhold beholdes utenfor Git med kildeinformasjon i brukerprosjektet når en senere implementering etablerer det.

## Begrunnelse

Evalueringen fant konkrete forskjeller: yt-dlp-kilde mot GPL-standalone, lokal FFmpeg GPL-build mot egen minimal LGPL-2.1-build; stock PyAV-wheels ble senere avvist fordi faktisk x264/x265 og en vendor-patch gjorde rapportert LGPL-3.0 utilstrekkelig, madmom/Open-Unmix-modellrestriksjoner og ufullstendig HTDemucs-modellkort. GUI-core og NuGet package-metadata alene klarerer heller ikke alle native/font-artefakter. Se [matrisen](../DEPENDENCIES.md) og [måleresultater](../PHASE1_RESULTS.md).

## Konsekvenser og releasekrav

- Et release-manifest må ha eksakte versjoner/hashes, kilder, lisens, notices og eventuelt tilsvarende source per binary/weight.
- Bruk den dokumenterte minimale FFmpeg-ruten som utgangspunkt; stock PyAV-bundlen er avvist. Verifiser nødvendige codec-funksjoner og dokumenterte vilkår i den faktiske releasepakken.
- HTDemucs/Roformer/umxl holdes utenfor standardpakken. Umxhq og Whisper har dokumentert MIT-grunnlag. Valgte fremtidige språkordbøker krever egen artefaktport.
- GPU-runtime må vurderes særskilt og kan ikke gjøres obligatorisk for CPU-ruten.
- Egen MIT-lisens og tredjepartsvilkår må beskrives tydelig i distribusjonen.
- Vesentlig andre krav enn den permissive modellen krever prosjekteierens eksplisitte vedtak etter masteren §18.2.

## Konkrete funn etter videre Fase 1

[Lisensrapporten](../PHASE1_LICENSES.md) og [anbefalingen](../PHASE1_RECOMMENDATION.md) angir gjeldende profil: whisper.cpp, direkte CTranslate2-alignment med to uendrede MIT-hjelpere, Open-Unmix umxhq, SwiftF0, medfølgende runtimes og egen minimal FFmpeg. Stock PyAV/faster-whisper-pakken er fjernet fra den avsluttende portable profilen. Compiler-runtime-exceptions og eventuelle MPL/andre filvilkår må følges; dette er ikke en pakke som kan merkes bare MIT.

## Vedtak

Prosjekteieren godkjente 2026-10-08 MIT for egen prosjektkode og separat dokumenterte LGPL-codecs med nødvendige source/notices/utskiftbarhetskrav. Dokumenterte compiler-runtime-exceptions og andre filvilkår må fortsatt følges. Open-Unmix med eksplisitt MIT-lisensiert umxhq er godkjent baseline; modellen skal ikke automatisk erstattes av noncommercial umxl.

Lokalt distribuerte runtimes og PyTorch-relatert pakkestørrelse er akseptert. GPU/CUDA er et valgfritt senere tillegg med separat artefaktvurdering. Uavklarte modellrettigheter og stock PyAV holdes fortsatt utenfor standardprofilen. Godkjenningen klarerer ingen fremtidig installasjonspakke automatisk og endrer ikke innhentet medias rettigheter.

Fase 1 er avsluttet med aksepterte begrensninger. Fase 2 er ikke startet.
