# Typed valgt lyrics og prosjektformat v3 – forslag

STATUS: LOCAL COMBINED CANDIDATE; standalone pakke1 er integrert/reviewet veddfbc33d, og Song/command/v3 er kodegodkjent fra lokal workerbd8addb. Remote/main har bare pakke1. [Lokal combined-verifikasjon](LYRICS_COMBINED_LOCAL_REVIEW.md) beskriver faktisk status; ingen publisering er autorisert her. Dette er stabilt kode/testgrunnlag; konkret PR/GitHub-historikk angir integrasjonsstatus. Retning/API ble reviewet og committet som03a6b00 før kode. Song-wiring/kommando/v3/storage er fortsatt kun senere forslag. [Arbeidsordre](PHASE3_LYRICS_STORAGE_WORK_ORDER.md), [aktiv plan](exec-plans/completed/phase3-lyrics-storage-contract-preparation.md). Autoritet: master§6/Fase3/Generation-grenser, aksepterte ADR-er og [formatv2](PROJECT_FORMAT_V2.md). Base94a1a806661e7c8e9093830c9be03b69f25979c4 etter integrert [PR16](https://github.com/MasterCorruptor/ultrastar-song-creator/pull/16); godkjent parserkode6c8a563 og fundament5492b44.

## Faktisk reposjekk og gap

Song har ingen SelectedLyrics. Note.Text/Phrase.Text og ImportedSource er egne autoriteter, ikke skjult lager. Core refererer ikke Acquisition. LyricsSelection inneholder Candidate med ALLE Documents og valgt Document; derfor må den ikke serialiseres direkte. ManualLyrics bruker eksisterende UTC lokal oppretting i RetrievedAtUtc og ingen artist/title/URL. LrcParseResult har rawLRC/Metadata/Diagnostics som IKKE finnes i LyricsDocument/candidate.

ProjectJson skriver v2 og deler SongDocument mellom historiskv1/v2. Decode ruter1 eksplisitt, deretter number==Version (2). Writerbytte til3 alene ville derfor miste v2-lesing. Store har64MiB samlet filgrense, strict JSON, duplicate/unknownproperties og validering før atomic temp/rename Save; Load er offline og rapporterer original SchemaVersion uten rewrite.

SongHistory.Execute registrerer bare endring når Apply returnerer et annet Song-objekt (ReferenceEquals). Undo/redo gjenbruker exact before/after snapshots uten ny Apply. ImmutableArray record-likhet er ikke generell strukturlikhet; no-op må løses i kommandoen.

## Foreslått separat Core-modell

Typed nullable Song.SelectedLyrics (defaultnull), immutable Core-records uten Acquisition-typeavhengighet. Navn er forslag som reviewes før kode:

| Objekt | Semantikk |
| --- | --- |
| SelectedLyrics | Kind Text/Instrumental, Document? og required Provenance. Null på Song betyr ingen valgt lyrics, IKKE instrumental. |
| SelectedLyricsDocument | Eksakt variant-ID/Text, Core SyncLevel Plain/Line/Word, source-sekunder, immutable line/word-hierarki. Bare valgt variant. |
| SelectedLyricsProvenance | Origin Manual/Provider, SourceId, CandidateId, SourceUrl?, CreatedAtUtc?/RetrievedAtUtc?, optional candidate artist/title/album/version/durationclaims. |
| Text | Nøyaktig ett validert document, ikke alle provider-varianter. |
| Instrumental | Eksplisitt kildeclaim med null Document; ingen inferens fra tom tekst/au-tag. |

Manual har SourceId manual, canonical lokal kandidat-ID fra eksisterende valg, SourceUrl=null, CreatedAtUtc satt fra det eksisterende Acquisition-tidspunktet og RetrievedAtUtc=null. Provider har SourceId provider-ID, opaque CandidateId, optional canonical HTTPSURL, RetrievedAtUtc fra eksisterende snapshot og CreatedAtUtc=null. Akkurat ett semantisk korrekt UTC-tidspunkt; ingen nye datoer/IDs genereres ved projection/undo/load. Manualmetadata hvis faktisk oppgitt er brukerclaims, ikke provider-verifisert eller currentSong-overstyring.

Missing/Unsupported/no-match er acquisitionresultater og kan ikke lagres som SelectedLyrics. Query/page/resultlister, uvalgte varianter, provider-capabilities/unsupported-formatnavn, ranking/matchscore og søkecache utelates. Ingen Note.Confidence/ASR/alignment-score.

## Acquisition→Core projection

Ny SelectedLyricsProjection.FromSelection i Acquisition refererer Core; aldri motsatt. Valider candidate og faktisk valgt variant før mapping, inkl status/ID/null/default/enums/grenser. For Text: finn akkurat ett member med Document.Id, krev eksakt strukturlikhet mellom valgt document og dette memberet, og projiser DET memberet. Et forged document med samme ID men endret tekst/tider skal avvises, ikke følge med til storage. Instrumental krever dokumentnull og instrumentalstatus/ingen candidatesdocs.

Proveniens er frosne kildeclaims, ikke kryptografisk bevis på en providerforespørsel eller rettighet/lydmatch. Mapperen kopierer bare valgt dokument og eksisterende proveniens til uavhengige Core-records; ingen fetch, normalisering eller metadataoverride.

Minimalt faktisk tilgjengelig grunnlag er candidate provider-ID/ID/URL/UTC-claim/metadata og document ID/Text/SyncLevel/Lines/Words. Raw LRC/metadata/diagnostics eller original file-offset kan ikke rekonstrueres fra dette valget. Ikke påstå rawformat-losslessness eller oppfinn offset; eventuell ekstra parserproveniens krever senere eksplisitt API-/formatreview.

## Bounds og validering

Foreslått frozen selected payload: dokumentets eksisterende logiske256KiB-budget, ID256/metadata4096UTF16/URL4096, source-tid0–14400sekunder, optional positiv duration, max10000lines/50000words og default/null/ukjente enums avvist. Samme nullend/startrekkefølge/word-medlemskap/samtidighet/overlap som reviewed fundament; ingen clipping mot kandidatvarighet eller Song.AudioOffset. LF/Unicode/spaces/repetisjon/trailinglinjer beholdes eksakt, ikke re-normalisert ved load.

Hele selected payload foreslås høyst512KiB logisk budget: chosen documentbudget + provenance stringbytes strictUTF8 +128bytes selected/provenance/timestampstruktur. Gjentatt line/word-text telles hver gang. Det er ikke actual JSON-wirelength eller CLR-memory; eksisterende samlet64MiB filgrense/MaxDepth64 består. V3 selectedpreflight må sjekke collectioncounts/tekstbudget før store DTO-arrayallokeringer. Ingen silent truncering.

Core-validator er provider-uavhengig. SongValidator tilføyer path-spesifikke InvalidSelectedLyrics-funn; ugyldig Save avvises som InvalidSong før målfilendring. DTO-struktur/ukjentenum/duplicate/unknown/missing property er InvalidJson/UnsupportedVersion etter eksisterende profil. Prosjektets innhold valideres offline uten provider- eller filoppslag; URLs er data.

## Ren kommando, clear og konkret no-op

SetSelectedLyrics(SelectedLyrics? value) implementerer ISongCommand og krever gyldig struktur/selectedpayload FØR Apply. Null er eksplisitt clear. Kun Song.SelectedLyrics erstattes; ImportedSource, media, offsets, metadata, phrases/notetekst og analysis bevares som eksisterende immutable referanser.

Foreslått no-op-strategi: en eksplisitt ordinal strukturlikhetsfunksjon for ALLE selected-fields, inklusive Kind/proveniens/IDs/UTC/duration/variantID/text/sync/source-time og ordnet line/word-content/nullends. Nullable double sammenlignes eksakt etter finitevalidering, ingen tolerant tidsavrunding. Like payloads returnerer det originale Song-objektet, selv om arrays ble bygd separat. Same tekst men ny faktisk proveniens/tidspunkt er en endring; ikke ignorér timestamp for å skjule nytt valg. Null→null er no-op.

Failure kaster EditRejectedException uten state/history/redoendring. Undo/redo gjenoppretter eksakte Song-/selectedsnapshots, ingen re-projection, factory, timestamp/ID eller nett. Referanselikhet alene eller vanlig record== brukes ikke som generell innholdssammenligning.

## Frozen v3 envelope og eksplisitt migrasjon

V3 root har akkurat format, schemaVersion3, song, importedSource og REQUIRED-NULLABLE selectedLyrics. SongDocument/legacy payload videreføres uendret; ikke legg selectedLyrics inne i shared SongDocument brukt avv1/v2. Ny frozen EnvelopeV3/SelectedLyricsV3Dto og uavhengige enum-stringmaps (text/instrumental, manual/provider, plain/line/word/source-sekunder), required nullable fields og strict JSON.

Reader switcher eksplisitt1/2/3; skriverversjon kan hete WriterVersion3 og må ikke erstatte number==Version uten eksplicitcase2. Historiske DTO-er, navnemaps og fixtures beholdes:

- v1→original Song med ImportedSource=null, SelectedLyrics=null.
- v2→bevart ImportedSource, SelectedLyrics=null.
- v3→begge felter etter frozen DTO/validering; selectedLyrics:null er gyldig, manglende felt er ugyldig.
- Load rapporterer original SchemaVersion og omskriver aldri filen. Explicit Save skriver3 uten nedgradering/automatisk inference.
- Aldri infer lyrics fra Note.Text/SourceHeaders/manuelle strings. Headerrekkefølge/casing/dupes/verdier forblir uendret, og currentSongmetadata har samme autoritet.

Ingen serializer binding til bevegelige Acquisition-records. Load/Save starter ingen parser/provider/FFprobe/model/media eller URL-fetch. AtomicProjectStore/cancellation/failed-save-bevaring videreføres.

## Foreslått filscope og arbeidsdeling etter review

En SEQUENTIAL domain/storageintegrator først: nye Core/SelectedLyrics.cs/SelectedLyricsValidation.cs/SetSelectedLyrics.cs, små nødvendige Song.cs/SongValidator.cs-endringer; ny Acquisition/Lyrics/SelectedLyricsProjection.cs; Projects/ProjectJson.cs og separat SelectedLyricsV3Dto.cs om praktisk; egne SelectedLyricsDomain/Projection/Editing/ProjectV3-tests og håndskrevne v2/v3 JSON-fixtures/current formatv3guide. Eksakte API/type-/filnavn fryses ved SUPERVISOR-review; parser/provider/GUI og gamleformatguiders historiske kontrakter bevares.

Trinn: (1) review typed/API/budget/no-op/DTO-kontrakt, (2) compilable Core+projection/command i sikkert checkpoint, (3) strictv3+legacy migrations/regresjoner, (4) samlet format/integritet/offline-rundtur og review. Ingen bred «hele Fase3»-ordre.

HTTP/parserarbeid kan senere være PARALLEL_SAFE mot storage når fileierskap er disjunkt og shared foundation/parser er stabile. Flere storageworkers krever reviewet compilable Core/projection/schema først; Song/Validator/ProjectJson/fellesfixtures/locks eies bare av integrator. Ustabil projection og schema implementeres ikke parallelt på separate antakelser.

## Offline acceptance og reviewporter

Egenskrevet plain/manual/line/word/instrumental/null, Unicode/spaces/repeated/trailinglines/nullends/samtidighet/overlap, bare valgtvariant/proveniens og forged/mismatched rejection. SourceHeaderrekkefølge/casing/dupes/values og hele eksisterende songpayload bevares gjennom command/history/storage.

Håndskrevne uavhengige v1/v2/v3 fixtures: v1/v2 gamle bytes uendret ved Load, eksplisitt v3 Save og reread full databevaring. Behold historiske v2-reader-tests; ikke mekanisk endre alle writerasserts2→3 uten legacy-kontroll. Ukjente/manglende/dupliserte fields, enum/timestamp/Unicode/timing/array/budgetfeil skal avvises. Failedsave beholder original bytes; new selection/clear/failure/no-op etterUndo skal verifisere redobevaring og exact snapshots.

Åpne reversible valg: endelige Core/DTO/proveniensnavn og budget/no-op implementasjonsdetaljer, selectedpreflight og tilgjengelig parserproveniens før v3 fryses. Ingen stor ownerdecision i seg selv; kontraktreview kreves. Dette var docsforberedelsen. Pakke1-testgrunnlaget står nedenfor; ingen v3/persistens eller live research.


## Reviewed pakke1 – pre-code kontrakt

Staging: første pakke oppretter KUN standalone Core-types/validation/equality og Acquisition projection. Ingen Song.SelectedLyrics, SongValidator, command/History/Projects/schemaendring. Ellers kunne dagens v2-writer stille miste et nytt Song-felt. Neste pakke må parre Song/command med faktisk v3-støtte eller eksplisitt avvisning av unsupported save; det implementeres ikke nå.

Frosne pakke1-navn i namespace UltraStar.SongCreator.Core: SelectedLyrics, SelectedLyricsDocument, SelectedLyricsLine/Word, SelectedLyricsProvenance; enums SelectedLyricsKind/Origin/SyncLevel/TimeReference. Provenance har SourceId/CandidateId/SourceUrl?, CreatedAtUtc?/RetrievedAtUtc?, optional Artist/Title/Album/Version/DurationSeconds. Document har Id/Text/SyncLevel/Lines/TimeReference, lines Text/StartSeconds/EndSeconds?/Words. Selection er Kind/Document?/Provenance. Null modell betyr intet valg.

SelectedLyricsValidation.Validate(SelectedLyrics?) og ValidateDocument(document) er standalone; ugyldig payload kaster SelectedLyricsValidationException med typed error InvalidSelection/InvalidDocument/InvalidProvenance/ResponseTooLarge. Null selection er gyldig. Validate bevarer data, ingen normalisering. Document256KiB og total512KiB/logisk formel ovenfor samt source/UTC/manual/provider-invariants fryses her; full v3-preflight er senere.

SelectedLyricsEquality.AreEqual(left?, right?) validerer BEGGE input før sammenligning; ugyldig input kaster samme typed exception selv ved samme reference. Ordinal tekst/scalar-proveniens og exact finite/null double/timestamps, ordnede recursive immutable arrays. Dokumenthelper DocumentAreEqual validerer dokumenter eksplisitt. Ingen Song/command no-op ennå.

Acquisition.Lyrics.SelectedLyricsProjection.FromSelection(LyricsSelection) validerer hele candidate og valgt association før mapping. For Text krav om akkurat ett matching-ID member med full strukturlikhet; projiser faktiske memberet, ikke et forged payload. Instrumental/null eksplisitt, missing/unsupported avvises. Mapper bruker eksplisitte Core/Acquisition namespacealiases/enum-maps, ingen Core→Acquisition reference, UtcNow/Guid-generator/fetch/parser/normalisering eller unselecteddata. Feil er eksisterende typed Acquisition-validationexception for association og Core typed validationexception for ugyldige projected proveniens/modelldata; ingen ny generic framework.

Manual SourceId manual, lowercase canonical nonempty GUID-D ID, URLnull, CreatedAtUtc fra existing candidate timestamp, RetrievedAtUtcnull. Provider source ikke manual, opaque bounded candidate-ID, optional HTTPSURL, RetrievedAtUtc fra candidate og CreatedAtUtcnull. Dermed bevares faktisk lokale creation/providerretrieval uten oppdiktede tider.

Filer: Core/SelectedLyrics.cs, SelectedLyricsValidation.cs, SelectedLyricsEquality.cs; Acquisition/Lyrics/SelectedLyricsProjection.cs; Core.Tests/SelectedLyricsDomainTests.cs og SelectedLyricsProjectionTests.cs. SDK-globs, ingen csproj/pakke/lock. Egne tests for normalisert tekst/timing/budget/invariants/null, strukturlike separate arrays/allefieldchanges, valgtvariant/proveniens og forgedchoice; eksisterende Song/source/storage bytes/API forblir uendret.


## Faktisk standalone kodecheckpoint

Core: SelectedLyrics.cs/SelectedLyricsValidation.cs/SelectedLyricsEquality.cs. Acquisition: Lyrics/SelectedLyricsProjection.cs med eksplisitte namespacealiases/enum-maps. Validering kaster typed standalone error; null valgtmodell er gyldig. Equality validerer begge input, bruker ordinal/exact scalar-proveniensequality og recursive ordnet dokumentinnhold. Projection validerer hele candidate og full valgtmember-association; bare det faktiske valgte dokumentet/proveniens kopieres, uten clocks/IDgenerering/nett/normalisering. Associationfeil har eksisterende Acquisition error, projected provenancefeil har Core typed error.

50 nye egne offline tests: egne manual/plain/line/word/instrumental/null data, Unicode/LF/budget/hierarki/nullends/overlap/UTC/kildeinvariants, alle relevante scalar/array equality-endringer, separate like arrays og deterministic selected-only projection/forgedchoice. En tidlig valid-fixture ble korrigert fordi en flyttet første wordstart brøt rekkefølgen; valideringen var korrekt og sluttfixture endrer en gyldig wordstart.

Windows SDK10.0.401 locked restore, Release0warnings/errors,672/672 (Core604=554+50,Audio17,Desktop51), solution-format verify-no-changes. Ingen ny Linux/native/HTTP/storage/v3test eller attachment/save-loadpåstand. Song/SongValidator/Editing/History/ProjectJson/Store og reviewed foundation/parser forblir byteidentiske. Staging beskytter mot stillev2datatap; senere Song/command/v3-pakke krever ny ordre etter API-review.

Sluttreview: uavhengig statisk review uten actionable funn, SUPERVISOR50/50 selected-tests og kontrollert672/672-sluttoutput. Ingen Song/command/v3/native/Linux/HTTP-akseptanse gjennom dette standalone reviewet.
