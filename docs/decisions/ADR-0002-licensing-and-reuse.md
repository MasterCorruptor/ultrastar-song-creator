# ADR-0002 – Permissivt mål og lisensport per artefakt

Dato: 2026-10-07. **Status: foreslått; ikke akseptert.** Prosjektets eksisterende LICENSE er fortsatt foreløpig MIT etter masteren §18.2.

## Problem

Komponenter kan ha permissiv kildekodelisens mens medfølgende weights, codecs, ordlister eller ferdige binærer har andre vilkår. En enkel «MIT»-kolonne for repositoryet er derfor ikke nok til å velge eller distribuere stacken.

## Alternativer

1. Beholde MIT for egen kode og velge individuelt vurderte tredjepartsartefakter, med eksplisitte notices og distribusjonsvilkår.
2. Kreve utelukkende permissive artefakter i hele installasjonspakken, selv hvis det begrenser media/GUI-funksjoner.
3. Godta GPL-/AGPL-/noncommercial-komponenter og endre prosjektets distribusjons-/lisenspremisser ved eget vedtak.

## Foreslått løsning

Fortsett å sikte mot **MIT for prosjektets egen kode**. Foreta endelig bekreftelse først etter valgt hovedstack og konkret dependency-/modell-/binærmanifest. LGPL-komponenter kan vurderes etter artefaktspesifikk compliance; dette er ikke automatisk godkjenning av enhver LGPL/GPL-kombinasjon.

Velg permissiv kilde/wheel-distribusjon der den er verifisert, for eksempel yt-dlp-wheel, fremfor å anta at standalone exe har samme lisens. Ikke kopier GPL-editor-/AGPL-analysekode inn i egen planlagt MIT-kodebase. Ikke innfør noncommercial eller gated weights som obligatorisk kjerne.

Uavklarte modellvilkår blokkerer det aktuelle modellvalget. Database-/providerlisens klarerer ikke copyright til song lyrics, media eller cover. Innhentet innhold beholdes utenfor Git med kildeinformasjon i brukerprosjektet når en senere implementering etablerer det.

## Begrunnelse

Evalueringen fant konkrete forskjeller: yt-dlp-kilde mot GPL-standalone, lokal FFmpeg GPL-build mot testet PyAV-wheels LGPL-3.0-biblioteker, madmom/Open-Unmix-modellrestriksjoner og ufullstendig HTDemucs-modellkort. GUI-core og NuGet package-metadata alene klarerer heller ikke alle native/font-artefakter. Se [matrisen](../DEPENDENCIES.md) og [måleresultater](../PHASE1_RESULTS.md).

## Konsekvenser og porter før aksept

- Et release-manifest må ha eksakte versjoner/hashes, kilder, lisens, notices og eventuelt tilsvarende source per binary/weight.
- Velg eller bygg en konkret FFmpeg/PyAV-rute med nødvendige codec-funksjoner og dokumenterte vilkår før bundling.
- Klarer HTDemucs-/alignmentweights og valgte språkordbøker individuelt.
- GPU-runtime må vurderes særskilt og kan ikke gjøres obligatorisk for CPU-ruten.
- Egen MIT-lisens og tredjepartsvilkår må beskrives tydelig i distribusjonen.
- Vesentlig andre krav enn den permissive modellen krever prosjekteierens eksplisitte vedtak etter masteren §18.2.

## Vedtak

Ikke vedtatt. LICENSE er ikke endret, og ingen fremtidig installasjonspakke er lisensklarert gjennom denne ADR-en.
