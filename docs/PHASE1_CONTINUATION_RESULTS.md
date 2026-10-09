# Fase 1 – videre målinger på ekte sang og native desktop

Utført 2026-10-07. Research/PoC, ikke produksjonskode. Maskinvaremodell, vertens OS-build, RAM-kapasitet og driverversjoner er utelatt. Tider gjelder disse kjøringene; uten offentlig hardwareprofil kan de ikke brukes som maskinsammenligning.

## Native playback og realistisk timeline

Windows-prøven åpnet et faktisk Avalonia-vindu og brukte miniaudios standard lydenhet, ikke headless playback. Den viste waveform fra ekte lyd, noter fra Vocaditos manuelle annotasjon og egen tekst «ord 1», «ord 2» osv. Disse plassholderne er ikke sangtekst og beviser ikke lyrics-layout.

Automatiseringen spilte fra begynnelsen, pauset/resumerte, søkte til 0,5 s, loopet området 1–2 s og endret viewport under playback. Playhead fulgte lydkildens native PCM-cursor. En separat Python-prosess kjørte først SwiftF0 og deretter ekte Open-Unmix-inferens med fire CPU-tråder samtidig; prosessen ble avsluttet ved prøvens slutt. Den avsluttende heavy-worker-prøven ga median UI-tick 16,70 ms, P95 26,34 ms, render-P95 9,63 ms, seks loops og bekreftet workerstopp på 85,21 ms. Relative sidecar-stier måtte gjøres absolutte ved prosessstart; denne feilrettingen ble prøvd fra pakkemappen.

Den separate no-device-testen renderte 256 000 stereo PCM-frames. Den målte fem wraps, cursor innenfor 48 000–96 000, stabil cursor ved pause og seek til 24 000 fulgt av cursor 24 512 etter 512 nye frames. Dette kontrollerer sample-grenser uten en fysisk enhet.

Den portable native-vindusprøven bestod også. Eksakte UI-/render-tider, renderantall og workeroutput finnes i [Windows-evidensen](../tools/phase1/results/windows-portable-native.json). Første native prøve ga median UI-tick 16,69 ms, P95 18,44 ms og render-P95 5,78 ms. Med ekte noter/tekst og andre samtidige prøver var P95 høyere; dette er ikke en 60 FPS-garanti.

Begrensninger: én Windows-lydenhet, automatisert prøve, ingen akustisk loopback, underrun-teller, målt output-latency, device-switch eller ferdig editor. PCM-input er 48 kHz; media normaliseres før sample-baserte seek-operasjoner. Linux no-device-testen er ikke bevis på fysisk Linux-lyd.

## Solovokal: pitch, noter og ASR

**Vocadito v3**, DOI [10.5281/zenodo.5578807](https://zenodo.org/records/5578807), oppgir CC-BY-4.0. Kilde: Rachel Bittner, Katherine Pasalo, Juan José Bosch, Gabriel Meseguer-Brocal og David Rubinstein. Hele settet med 40 korte ekte sangopptak ble brukt, totalt **817,13 s**, med manuelt annotert frame-F0 og noter fra to annotatorer. Lyd/lyrics distribueres ikke i repositoryet.

Begge pitch-kandidater fikk mono 16 kHz og frekvensområde 50–1200 Hz. pYIN: frame 2048, hop 256. SwiftF0: én tråd, spin=False, confidence 0,5. Nærmeste annotasjonstid ble brukt, uten interpolasjon. Raw pitch accuracy teller voiced referanseframes med feil under 50 cent; uteblitt voiced-prediksjon teller som feil. Tabellen viser **median per opptak**, ikke en duration-vektet corpus-score.

| Mål | SwiftF0 0.3.0 | pYIN / librosa 1.0.0 |
|---|---|---|
| Raw pitch accuracy, under 50 cent | 97,31% | 88,72% |
| Voicing recall | 98,58% | 99,62% |
| Voicing false alarm | 10,98% | 54,74% |
| Median absolutt centfeil når begge er voiced | 4,01 | 11,80 |
| Median real-time factor | 0,00613 | 0,02354 |

SwiftF0s foreslåtte noter ble matchet én-til-én mot hver annotator, med onsettoleranse 50 ms og pitchtoleranse 50 cent, uten offsetkrav. Gjennomsnittlig note-F1 over 40 opptak × to annotatorer var **0,7571**. Det er en basal tone-/onset-score, ikke korrekt stavelses- eller karaoke-segmentering.

På de 17 engelske opptakene kjørte pinnet faster-whisper-tiny, CPU int8/four threads. Macro-gjennomsnittlig WER var **25,25%**, median 16,67%. Ordnormalisering bruker små bokstaver og ordregex; ingen musikalsk lyrics-format-score. Tiny er en feasibility-modell, ikke et besluttet produksjonsvalg.

[Alle 97 målinger](../tools/phase1/results/vocadito-singing.json) og [aggregater](../tools/phase1/results/vocadito-summary.json). Solovokal uten backing er ikke representativt for alle separerte vokaler, duetter eller norsk sang.

## Kjent tekst: word alignment på ekte miksinger

Tre engelske innspillinger fra [JamendoLyrics MultiLang](https://huggingface.co/datasets/jamendolyrics/jamendolyrics), revisjon de188c963fd4539bc769b3feb83582e5a9595e36, ble valgt ut fra BY/BY-SA-lisensfelt: Lower Loveday – Is It Right?, Rxbyn – Bad Side og Cortez – Feel (Stripped). Dataset/annotasjoner: Simon Durand, Daniel Stoller og Sebastian Ewert. Metadata og lydens egne CC-vilkår vurderes separat.

Hver prøve tok 29 s fra første annoterte ord minus 0,5 s. Korrekt kjent tekst for vinduet ble gitt til tiny-Whispers DTW/alignment via faster-whispers private find_alignment-API. Dette er en **forced alignment-prøve**, ikke transkripsjon eller en ferdig global aligner. Alle 136 referanseord ble matchet sekvensielt i tokenizeroutput.

| Innspilling | Ord | Median onsetfeil | P95 onsetfeil | Innenfor 200 ms |
|---|---|---|---|---|
| Is It Right? | 44 | 114 ms | 580 ms | 70,5% |
| Bad Side | 47 | 119 ms | 830 ms | 80,9% |
| Feel (Stripped) | 45 | 140 ms | 875 ms | 77,8% |

Tolkning: rask lokal alignment er mulig, men tail-feil er for store til å love ferdig word-level karaoke. Kjent tekst, manuelt valgt vindu og første vokalområde er sterkere forhåndsinformasjon enn en generell full-song-pipeline. Offset, repetert tekst, melisma og norsk er ikke kvalitetsevaluert. Den private API-en må erstattes/innkapsles før produksjon.

[Alignment-evidens](../tools/phase1/results/singing-alignment.json). Lyrics og transkripsjoner er ikke lagret i den publiserte evidensen.

## Vokalseparasjon og hele sanger

Spleeter 2.4.2 / 2stems v1.4.0 ble undersøkt som lisensmessig dokumentert referanse. Normal installasjon på Windows feilet fordi tensorflow-io-gcs-filesystem 0.32.0 ikke hadde nødvendig wheel. Research-overstyring til 0.31.0 gav inferens, men pip check bestod ikke; denne ruten er derfor **ikke anbefalt standardpakke**.

På 20 s ekte Vocadito-vokal blandet med egen syntetisk backing endret Spleeter vocal SI-SDR fra 5,91 til 22,09 dB. Det er en kontrollert blanding, ikke musdb- eller kommersiell stem-kvalitet. På tre ekte 29 s miksinger tok inferens 2,45–3,38 s. Finite stems og liten sum-rekonstruksjonsfeil er ikke kvalitetsbevis uten referansestems.

**Open-Unmix 1.3.0 / umxhq 1.0.1** ble deretter prøvd med PyTorch/Torchaudio 2.10.0+cpu og Python 3.12. Dependencykontrollen bestod. Modellutgiverens [Zenodo-post](https://zenodo.org/records/3370489) oppgir MIT; fire checkpoints ble kontrollert mot utgiverens MD5 og fikk lokale SHA256-fingeravtrykk.

De samme tre 29 s miksingene tok ca. 1,87–1,94 s i første Windows-venv-prøve. Modellen kjørte også i CPython embeddable-pakken og i Linux med Docker-nettverk helt deaktivert. Tider på tvers av disse miljøene brukes ikke til å rangere OS-er.

Tre **komplette sanger** ble behandlet på CPU i sekvensielle, ikke-overlappende 30 s vinduer for å begrense minne:

| Sang | Varighet | Inferens | Real-time factor |
|---|---|---|---|
| Is It Right? | 180,06 s | 13,18 s | 0,0732 |
| Bad Side | 216,56 s | 14,75 s | 0,0681 |
| Feel (Stripped) | 240,56 s | 17,20 s | 0,0715 |

Dette er throughput med endelig output i hvert vindu, ikke en ferdig sømløs full-song-separator: vindusgrenser er ikke crossfadet, og referansestems mangler. Ingen kvalitetsrangering mellom Open-Unmix og Spleeter følger av tidsmålingene. Se [UMX-fullsong-evidens](../tools/phase1/results/umxhq-full-songs.json), [portable UMX](../tools/phase1/results/windows-portable-umxhq.json), [Linux UMX](../tools/phase1/results/linux-umxhq.json) og [Spleeter-referansen](../tools/phase1/results/real-mix-spleeter.json).

## BPM på ekte musikk

librosa beat_track kjørte på de tre 29 s miksingene: ca. 133,93, 144,23 og 75 BPM. Datasettet har ikke beat-/tempo-ground-truth. Tallene dokumenterer output og gjennomførbarhet, **ikke korrekt musikalsk tempo eller fase**. Halv-/dobbelt-tempo og manuell gridkorreksjon er fortsatt nødvendig. [Evidens](../tools/phase1/results/real-music-bpm.json).

## Windows-pakking og codec

Self-contained win-x64 .NET/Avalonia ble kjørt sammen med offisiell CPython 3.12.10 embeddable, SwiftF0/ONNX Runtime, Open-Unmix/PyTorch og faster-whisper/CTranslate2. Python/.NET-utviklingsverktøy var ikke i PATH under kjøring. Pakkens Pythondependencykrav ble kontrollert mot faktisk installert metadata.

PyAV 19.0.1 kunne ikke brukes med faster-whisper 1.2.1s file-decode-API: metadata_errors-argumentet var inkompatibelt. Den første portable researchruten ble derfor pinnet til **PyAV 18.0.0**, hvor decode-API og inferens på tre miksinger bestod. Den ble senere avvist; den avsluttende profilen nedenfor har ingen PyAV. Modellast/inferens avviste Python-socketconnect og brukte bare lokal modellcache; dette er ikke OS-nivå nettisolasjon.

PyAV 18-wheelen rapporterte LGPL-3.0-or-later for FFmpeg-libs uten GPL/nonfree. Dette er en egen codec-bundle, uavhengig av den minimale FFmpeg-prosessen. En dypere sourcekontroll fant at vendor-release 8.1.2-1 inkluderer x264/x265 og en patch som flytter dem fra GPL-listen til version3-listen. Den rapporterte LGPL-etiketten er derfor utilstrekkelig. **Stock PyAV-ruten avvises fra standardpakken**, og av/faster-whisper-pakken ble fjernet fra den avsluttende portable profilen. Ingen offentlig binærrelease er laget.

En **egen minimal Windows FFmpeg 8.1.3-build**, fra commit 330caae0c1acccd2222edc52a05940c574561ce5, ble krysskompilert og kjørt på Windows. Native -L oppgir LGPL-2.1-or-later, uten GPL/nonfree/version3. MP3 fra ekte sang ble dekodet til nøyaktig 29 s stereo PCM16 / 48 kHz. Full FFmpeg-source, configure-/build-oppskrift og LGPL-lisens ligger ved lokal prøvepakke. Compiler-runtime-obligasjoner vurderes separat.

Det brede BtbN LGPL3-shared-arkivet ble også hashkontrollert og testet, men avvises som standardpakke inntil alle enabled tredjepartsbibliotekers source/notices er samlet. Den allerede installerte GPL-FFmpeg ble bare brukt til private research-fixtures.

[Portable ASR/native codecmetadata](../tools/phase1/results/windows-portable-asr.json), [egen minimal codec](../tools/phase1/results/minimal-codec.json) og [artefakt-/lisensrapport](PHASE1_LICENSES.md).

Det lokale, ukomprimerte prøveinnholdet (app, Python, modeller, codec/source, ASR og notices) utgjorde omtrent 1405 MiB. Dette inkluderer modeller/kilde og debug-/researchinnhold; det er ikke størrelsen på en optimalisert installer.

Dette er samme Windows-vert, ikke en ren VM. Windows Sandbox var ikke tilgjengelig. Installer, signering, oppdatering, VC-runtime-forutsetninger på ren vert og total produktpakke er ikke verifisert av denne prøven.

## Avsluttende rute uten stock PyAV

whisper.cpp 1.9.5 ble bygget fra eksakt kilde med CUDA, Vulkan, OpenMP, BLAS, FFmpeg-linking og AVX/AVX2/FMA/F16C deaktivert i denne generiske CPU-prøven. Whisper tiny/GGML-modellens SHA256 er be07e048e1e599ad46341c8d2a135645097a538221678b7acdd1b1919c6e1b21, med pinnet modellrepo-revisjon. Kode og modell har dokumentert MIT-grunnlag.

På de tre 29 s miksingene bestod ASR og DTW-tokenoutput på Windows og Linux. Avsluttende isolert Windows-måling inkludert prosess/modelast tok 5,94 / 6,16 / 9,17 s; WER var 43,75% / 30% / 20%. Dette er andre dekoder-/presisjonsinnstillinger enn CTranslate2-int8; ingen generell engine-rangering. Token-DTW er ikke forced alignment av innhentet tekst. [Avsluttende portable ASR](../tools/phase1/results/windows-whisper-cpp.json), [Linux ASR](../tools/phase1/results/linux-whisper-cpp.json).

Kjent tekst ble også alignet direkte i CTranslate2 4.8.2 på PCM. To uendrede MIT-filer fra faster-whisper 1.2.1 gjenbrukes bare til mel-features/tokenizer; ingen faster-whisper-pakke eller PyAV importeres. 136 av 136 ord ble matchet. Windows-medianer ca. 122 / 119 / 154 ms, P95 ca. 583 / 985 / 875 ms. Dette bevarer rask offline alignment med samme tydelige kvalitetsbegrensning. [Portable direkte alignment](../tools/phase1/results/windows-direct-alignment.json), [Linux](../tools/phase1/results/linux-direct-alignment.json).

Den avsluttende portable kjøringen brukte bare kode/runtimes/modeller under prøvepakken, med fixture-data utenfor pakken, og startet fra pakkemappen med utviklingsverktøy ute av PATH. Stock av/faster-whisper var fraværende og dependencykontrollen bestod. [Kontroll](../tools/phase1/results/windows-portable-dependencies.json). Prøven løser redistribusjonsrisikoen ved å utelukke den berørte bundlen; den forsøker ikke å gi den en ny lisens.

## Linux smoke

Faktisk Ubuntu 24.04-container, pinnet .NET SDK-image, Linux-native miniaudio og Avalonia under Xvfb. Samme C#-kilde og Python-worker som på Windows. No-device PCM-loop/seek/pause og native X11-vindu med rendering og workeroutput bestod. UMX, whisper.cpp og direkte CTranslate2 kjørte med nettverk slått av. Den oppdaterte UI-prøven kjørte også UMX-belastning, bekreftet workerstopp og seks loops. Ingen fysisk Linux-enhet var tilkoblet.

[Linux native/GUI](../tools/phase1/results/linux-native.json), [PCM](../tools/phase1/results/linux-pcm.json) og [Linux-modell](../tools/phase1/results/linux-umxhq.json). Dette tester arkitektur/native binaries og lokal inferens, ikke fysisk audio device, Wayland eller en Linux-installasjonspakke.

## Gjenstående beslutnings- og releasegrenser

Researchen gir konkret grunnlag for [teknologianbefalingen](PHASE1_RECOMMENDATION.md). GPU, norsk sang, kilde-stem-kvalitet, ferdig full-song-alignment, fysisk Linux-audio og ren Windows-release er ikke utført. Native programmer/worker-minimum er prøvd; et fullstendig produkt og alle releaseartefakter eksisterer ennå ikke. Prosjekteieren godkjente evidensen og avgrensningene som tilstrekkelige 2026-10-08 og avsluttet Fase 1. ADR-0001 og ADR-0002 er akseptert. Ingen Fase 2 er startet; gjenstående kvalitets-/releasearbeid er ikke utført gjennom vedtaket.
