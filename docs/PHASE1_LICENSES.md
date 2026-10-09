# Fase 1 – modell-, codec- og distribusjonsvilkår

Kontrollert 2026-10-07. Rapporten skiller **lisensgrunnlag**, **teknisk verifikasjon** og **ferdig redistribusjonspakke**. Et permissivt pakkenavn er ikke en komplett klarering. MIT for egen prosjektkode er bekreftet av prosjekteieren 2026-10-08; ingen offentlig produktbinær eller modellpakke er publisert.

## Valgte evalueringsartefakter og anbefalt profil

| Artefakt | Dokumentert lisensgrunnlag | Konsekvens for anbefalingen |
|---|---|---|
| .NET 10-runtime | [MIT og tredjepartsnotices](https://github.com/dotnet/runtime/blob/main/LICENSE.TXT) | Medfølgende runtime er mulig. Ta med eksakt releases LICENSE/ThirdPartyNotices, ikke bare prosjektets MIT. |
| Avalonia 12.1.3 / managed dependencies | NuGet-nuspec/lock + [MIT-kilde](https://github.com/AvaloniaUI/Avalonia/blob/16572aeff14531c02deaac5ba1a7c132a9c985d0/licence.md) | Egnet desktopkandidat. Native assets har flere vilkår nedenfor. |
| Hexa.NET.MiniAudio 1.0.1 / HexaGen.Runtime 1.1.24 | MIT i faktisk nupkg; MiniAudio-pakkens source commit 03b58b7e807af64ac42135b0c2f8cde2d8c9cada | Bruk pakke/binaryhash og native miniaudio-lisens sammen. Eksakt inventar er publisert. |
| miniaudio 0.11.25 og bundled decoders | [MIT-0 eller Unlicense](https://github.com/mackron/miniaudio/blob/0.11.25/LICENSE) | Kandidat for native PCM/WAV/MP3/FLAC, ikke en komplett FFmpeg-erstatning. Velg og dokumenter MIT-0-alternativet og eventuelle separat aktiverte dekodere. |
| SkiaSharp/HarfBuzzSharp native | Nupkg LICENSE + THIRD-PARTY-NOTICES: wrapper-MIT, Skia-/HarfBuzz-/øvrige kilder har egne permissive vilkår | Native Win32/Linux-notices må følge pakke. Hele installasjonen skal ikke merkes bare MIT. |
| Avalonia ANGLE natives | Faktisk pakkelisens er BSD-lignende 3-clause | Bevar attribution og disclaimer; eget binaryinventar. |
| CPython 3.12.10 embeddable | Offisiell LICENSE.txt med PSF og bundled notices | Verifisert archive-SHA256: 4acbed6dd1c744b0376e3b1cf57ce906f9dc9e95e68824584c8099a63025a3c3. Medfølgende runtime prøvd; Windows-runtime-forutsetninger på ren vert gjenstår. |
| SwiftF0 0.3.0 + bundled ONNX | Faktisk wheel-METADATA/MIT/LICENSE og [pinnet repo](https://github.com/lars76/swift-f0/blob/d1ba77fe5331310b778ebe126b6cb3656d41d24f/LICENSE); ingen separat weight-exception identifisert | Egnet pitch-kandidat med modellhash i artefaktmanifest. |
| ONNX Runtime 1.30.0 | Wheelens MIT-LICENSE og omfattende ThirdPartyNotices.txt | Ta med begge. Ikke reduser native bundle til bare én MIT-rad. |
| Open-Unmix 1.3.0 / umxhq 1.0.1 | Kode [MIT](https://github.com/sigsep/open-unmix-pytorch/blob/fb672c9584997c2b05e148eeaa65b4c23ed4693b/LICENSE); modellutgiverens [Zenodo-record 3370489](https://zenodo.org/records/3370489) oppgir MIT for akkurat disse vektene | Anbefalt konservativ separasjonsbaseline. Fire checkpoints kontrollert mot recordens MD5; SHA256/proveniens dokumentert. |
| PyTorch/Torchaudio 2.10.0+cpu | Faktisk wheel LICENSE/NOTICE: BSD-lignende hovedlisens og mange bundled notices | CPU-wheel uten CUDA prøvd på begge OS-er og i embeddable-pakken. Pakkestørrelse er en kostnad. Native/transitive notices følger med lokalt. |
| NumPy 2.5.3 / SciPy 1.18.1 | BSD-lignende kode; wheelnotices dekker OpenBLAS/LAPACK og **GPL-3.0-or-later WITH GCC-exception-3.1** for statisk GCC-runtime | Runtime-exception er en egen tillatelse, ikke vanlig GPL-kode kopiert inn i prosjektet. Bevar notices og kontroller sourceobligasjoner. En absolutt «ingen GPL-navn noe sted»-profil vil kreve andre builds. |
| faster-whisper 1.2.1 / CTranslate2 4.8.2 | MIT-kode; tiny-modellkort oppgir MIT for revision d90ca5fe260221311c53c58e660288d3deb8d356 | Modell-SHA256 dcb76c6586fc06cbdac6dd21f14cfd129cc4cdd9dce19bf4ffa62e59cbe6e6d1. CPU/offline er prøvd. Kodevilkår klarerer ikke PyAV nedenfor. |
| PyAV 18.0.0 Windows-wheel | BSD-binding; native FFmpeg-libs rapporterer **LGPL-3.0-or-later**, uten GPL/nonfree i faktisk konfigurering | **Avvist som standardbundle.** Faktisk DLL-sett inkluderer x264/x265, og vendor-patchen endrer GPL-detekteringen. Rapportert LGPL/nonfree-flagg klarerer derfor ikke artefaktet. PyAV 19 gav dessuten en API-konflikt. |
| whisper.cpp 1.9.5 / GGML tiny | [MIT-kilde](https://github.com/ggml-org/whisper.cpp/blob/v1.9.5/LICENSE), modellkort MIT, og [OpenAIs eksplisitte MIT-grant for kode og modellvekter](https://github.com/openai/whisper/blob/main/README.md#license) | Anbefalt ASR-sidecar. Egen CPU-build uten GPU/FFmpeg-linking prøvd på Windows/Linux og i portable mappe. Eksakt source/modelhash publisert. |
| Egen minimal FFmpeg 8.1.3 | Eksakt source commit 330caae0c1acccd2222edc52a05940c574561ce5; runtime -L: **LGPL-2.1-or-later**, ingen GPL/nonfree/version3 | Faktisk bygget og kjørt på Windows. Egen recipe, sourcearchive og COPYING.LGPLv2.1 følger lokal prøve. Separate DLL-er/CLI gjør codec utskiftbar; compiler-runtime-notices/source må også følge endelig pakke. |

[Python-artefaktinventar](../tools/phase1/results/python-package-licenses.json) dekker det faktiske CPU-settet. Blant filvilkårene er certifis CA-data under MPL-2.0; bevar disse separat. Listen er ikke en påstand om at alle dependencies er MIT.

[NuGet-artefaktinventar](../tools/phase1/results/native-package-licenses.json) inneholder alle 26 lock-pakker, versjoner, pakke-SHA256, lisensfelt og kildecommits der pakken oppgir dem. [Modell-/runtimeproveniens](../tools/phase1/results/continuation-artifacts.json) registrerer konkrete hashes. Lokalt er notices samlet fra faktiske pakker; dette er ikke et ferdig release-SBOM for et produkt som ennå ikke finnes.

PyAV 18s [pinnede vendor-konfigurasjon](https://github.com/PyAV-Org/PyAV/blob/v18.0.0/scripts/ffmpeg-8.1.json) peker på pyav-ffmpeg 8.1.2-1. [Wheel-workflow](https://github.com/PyAV-Org/PyAV/blob/v18.0.0/.github/workflows/tests.yml) og faktisk binarykonfigurasjon må følge sourcematchen. PyAVs generelle build-deps-script aktiverer GPL; det skal **ikke** ukritisk brukes som oppskrift for den inspiserte LGPL-wheelen. Siste README i et build-repository er heller ikke bevis for eldre wheels. Den konkrete [vendor-patchen](https://github.com/PyAV-Org/pyav-ffmpeg/blob/8.1.2-1/patches/ffmpeg.patch) flytter x264/x265 ut av GPL-listen, mens [kildemanifestet](https://github.com/PyAV-Org/pyav-ffmpeg/blob/8.1.2-1/scripts/pkg.py) henter vanlige x264/x265-kilder. Ingen separat redistribusjonsgrant som gjør dette tilstrekkelig permissivt er dokumentert i denne evalueringen. Bundlen holdes derfor utenfor standardruten.

## Modeller/ruter som holdes utenfor standardpakken

| Kandidat | Funn | Avgjørelse i dette forslaget |
|---|---|---|
| HTDemucs / htdemucs_ft | Kode er MIT, men [modellkort](https://huggingface.co/facebook/htdemucs) og diskusjoner gir ikke en tilstrekkelig ubetinget redistribusjonsgrant for valgte weights. | Ikke obligatorisk eller distribuert standardmodell. Modellrettighet må dokumenteres før senere innføring. |
| KimberleyJSN/melbandroformer | Nåværende modelcard oppgir MIT; tidligere utgiverdiskusjon omtaler GPL, og det refererte kode-repositoryet manglet egen lisensfil ved kontroll. | Ikke standardmodell på grunnlag av én metadataetikett. Dokumenter eksakt weight-grant og kodeproveniens før bruk i produkt. |
| Open-Unmix umxl | [Offisiell README](https://github.com/sigsep/open-unmix-pytorch/blob/fb672c9584997c2b05e148eeaa65b4c23ed4693b/README.md) oppgir CC-BY-NC-SA-4.0 for default weights. | Avvist som obligatorisk kjerne. Bruk eksplisitt umxhq; ikke bibliotekets navngitte default. |
| Spleeter 2stems v1.4.0 | Utgivernes [JOSS-artikkel](https://www.theoj.org/joss-papers/joss.02154/10.21105.joss.02154.pdf) og [pinnet paper.md](https://github.com/deezer/spleeter/blob/c8854001ac8acad34a9bc2bd15f28475541828b1/paper.md) angir MIT for både kode og pre-trained models. Archive-SHA256 f3a90b39dd2874269e8b05a48a86745df897b848c61f3958efc80a39152bd692 samsvarte med utgiverens checksum. | Lisensmessig dokumentert researchreferanse. Avvist som foreslått standard på grunn av gammel TensorFlow-/Pythonflate og ødelagt normal Windows-dependencykombinasjon. |
| BtbN full LGPL-shared 8.1-build | Eksakt asset/digest kontrollert, -L/konfigurasjon undersøkt, dekoding bestod. Mange enabled tredjepartsbiblioteker og ingen komplett source-/noticespakke i arkivet. | Ikke kopier bred build direkte inn i release. Bruk minimal codecprofil eller full artefaktvis compliance. |
| Installert GPL-FFmpeg og standalone yt-dlp-exe | Avviker fra planlagt permissiv hovedkode-/pakketenkning. | Kun lokalt researchverktøy; ikke publisert/bundlet. yt-dlp-wheel vurderes separat. |
| GPU/CUDA runtimes og språkordbøker | Ingen valgt distributert CUDA-/språkpakke; ingen GPU-benchmark her. | Ikke del av bekreftet CPU-pakkeprofil. Avklar eksakte artefakter først ved konkret tillegg. |

At en uavklart modell **avvises** løser standardpakkens avhengighet til den; det er ikke en påstand om at dens rettigheter er avklart. Prosjekteierens samtykke kan ikke erstatte rettighetshaverens grant.

## Datasett og innhentet innhold

Vocadito-recorden oppgir CC-BY-4.0. JamendoLyrics er MIT for program-/annotasjonsdelen, med egne CC-vilkår per audio/lyrics i LicenseType. Bare BY/BY-SA-engelske innspillinger ble valgt til den lokale miksingsprøven. Kildene og opphavspersonene er kreditert i resultatrapporten. Datasetlisens må ikke omtales som en generell lisens til andre sangtekster, covers eller brukerens musikk.

Ingen modeller, sanger, checkpoints, lyrics eller transkripsjoner er committet. Publisert evidens består av målinger, versjoner, schema, lisensgrunnlag og provenance. Fetched sangtekst i corpus brukes bare lokalt til benchmarken.

## Konkret distribusjonsbeslutning

Anbefal **MIT for egen kode**, individuelt dokumenterte permissive weights/runtimes, og en separat LGPL-codecprofil med komplette forpliktelser. Prosjekteieren godkjente denne lisensstrategien 2026-10-08, uten å klarere fremtidige releaseartefakter automatisk. Den foreslåtte avsluttende profilen bruker whisper.cpp og CTranslate2 direkte på PCM og **utelukker stock PyAV**. De to uendrede MIT-hjelpemodulene er bevart med original LICENSE/proveniens under tools/phase1/vendor/.

Før noen faktisk produktbinær publiseres må den endelige, faktiske pakken ha: exact binary/model hashes, source/build-proveniens, alle notices og corresponding source for berørte native/codec/compiler-runtimes, mulighet til å erstatte LGPL-komponentene og korrekt lisensomtale i produkt/pakkedokumentasjon. Source til egen FFmpeg er allerede bevart lokalt; det klarerer ikke automatisk PyAVs separate FFmpeg-bundle.

**Håndtert kandidatport:** Stock PyAV er avvist og fjernet fra den anbefalte prøvepakken; en fungerende alternativ rute er verifisert. Ingen uklar modell er obligatorisk i forslaget. **Gjenstående releasearbeid:** samlet endelig installer-SBOM, compiler-/OS-runtime-forutsetninger og alle tilsvarende source/notices må følge den faktisk bygde produktpakken. Research-/kodepublisering er kontrollert separat. [ADR-0002](decisions/ADR-0002-licensing-and-reuse.md) er akseptert 2026-10-08.
