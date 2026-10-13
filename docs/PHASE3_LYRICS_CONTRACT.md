# Fase 3 Lyrics Acquisition – kontraktforslag

STATUS: IMPLEMENTED CHECKPOINT, compilable fundament avventer SUPERVISOR-kodereview. Reviewet retning for avgrenset in-memory fundament. SUPERVISOR har reviewet repo-gap og separat providerresearch; adapter/parser/persistens er ikke del av fundamentet. [Arbeidsordre](PHASE3_LYRICS_WORK_ORDER.md), [aktiv fundamentplan](exec-plans/active/phase3-lyrics-foundation.md). Autoritet: master§6/Fase3, aksepterte ADR-er og [prosjektformatv2](PROJECT_FORMAT_V2.md).

## Faktisk gap ved main50815d8

Song.cs har ingen lyrics-document eller candidate-felt. Note.Text er redigert notetekst; Phrase.Text er avledet concat av noter. AnalysisArtifact har tids-/verdi-punkter og reference, ikke et egnet lyrics-payload eller tekstproveniensformat. Ingen av disse brukes som skjult lager for lyrics.

SourceDocument er original proveniens med ordnede headers, og RecordingChoices/DownloadedMediaDraft bruker eksplisitte frosne v1-profiler. RecordingChoices.Attach krever local-media-v1 og endrer bare proveniens. ProjectJson skriver root-schema2, bruker strenge DTO-er/enums, avviser ukjente properties og leser historiskv1 separat. ProjectStore laster uten medier/nett. Derfor gir eksisterende modeller ingen automatisk generell sangtekstpersistens.

Fase1 [audit](PHASE1_AUDIT.md), [resultater](PHASE1_RESULTS.md) og [lisensmatrise](DEPENDENCIES.md) har tidligere AMLL/LRCLIB/lyrics.ovh-kandidater og begrensede tilgjengelighetsprøver. Disse er historiske indikasjoner, ingen verifisering av dagens APIs, faktisk word-sync eller rettigheter til tekst. Ny offisiell research utføres separat; denne oppgaven dupliserer den ikke.

## Normalisert innhentingsresultat

Faktisk namespace UltraStar.SongCreator.Acquisition.Lyrics. Immutable resultater holdes utenfor Song/History i første fundament. Ingen audiofil/model er nødvendig.

| Objekt | Kontraktinnhold |
| --- | --- |
| LyricsQuery | Artist og Title, begge obligatoriske ikke-blanke; optional kjent varighet i sekunder/versionhint/recording-ID; Limit1–25. Ingen personlig nøkkel. |
| LyricsCandidate | ProviderId + opaque Id, artist/title, optional album/version/duration, optional canonical HTTPS SourceUrl (null for manual), UTC RetrievedAt, instrumental-indikasjon, tilgjengelige dokumentvarianter og matchindikasjoner. |
| LyricsDocument | Exact Unicode text (LF-linjeslutt, behold spaces/linjer/repetisjoner), SyncLevel Plain/Line/Word, immutable lines/words, tidsreferanse LyricsSourceSeconds. Ingen syllable-/note-generering. |
| LyricsLine/Word | Text, StartSeconds og optional EndSeconds. Word er immutable medlem av en bestemt LyricsLine.Words. Manglende endtid beholdes null, aldri oppdiktet fra neste start eller lyd. |
| LyricsPage | ProviderId, Query, immutable Candidates, RetrievedAtUtc, ingen pagination-antakelse; første LRCLIB-adapter vil ha maks20 uten paging. Tom side er no-match. |
| LyricsSelection | Eksakt candidate-identitet fra returnert page og eksplicit valgt dokumentvariant, inklusive frosset tekst/tider/proveniens. Aldri automatisk førstevalg. |
| LyricsMatchEvidence | Separate artist/title/version-indikasjoner, optional durationdelta-sekunder og tekstlikhetsindikasjon bare når en faktisk referansetekst foreligger. Manglende signal er Unknown/null. Ingen ASR/alignment-score oppfinnes. |

Provider-uavhengig interface er ILyricsSearchProvider.ProviderId og Task< LyricsPage > SearchAsync(LyricsQuery query, CancellationToken cancellationToken = default). Adaptere normaliserer sine faktiske lookup/search-forskjeller. Ikke følg lyricsfile/andre vilkårlige URL-er fra svar uten separat godkjent allowlist/transportpolicy. Ingen generell plugin-/fetchframework. SearchAsync er tilstrekkelig for planlagt LRCLIB search; ingen Fetch/lookup/pluginframework innføres.

En kandidat kan ha flere varianter (word/line/plain); tilgjengelig kvalitet er basert på validerte tidsdata, ikke bare et providerflagg. En instrumental-kandidat bevares eksplisitt med tomme dokumenter; ukjent instrumental-status er null. Tom lyric-tekst uten instrumental-bekreftelse er ikke en gyldig lyrics-document. Instrumental er ikke no-match/transportfeil og gir ikke tomt sangutkast automatisk.

## Bounds og tidskontrakt

Reviewede lokale ressursgrenser (ingen provider-SLA): artist/title/versionhint høyst4096 UTF-16 code units hver; provider/candidate-ID256, source-URL4096. Candidate-collection25 per page. Strict UTF-8 HTTP body høyst4MiB, og samlet normalisert page-payload4MiB; hvert dokument256KiB UTF-8. Høyst10000 lines og50000 words per dokument. Dypde32 for JSON/XML-struktur; DTD/external entities/nett-oppslag for XML er avvist. Ikke truncer skjult ved overskridelse; rapporter ResponseTooLarge/InvalidResponse.

Alle normaliserte tider/duration er finite sekunder, ikke millisekunder/ticks. Duration hvis oppgitt er positiv og høyst14400s. Start/end0–14400, end>=start; startrekkefølge er nondecreasing, samtidig/overlappende lyrics kan bevares (masterduet senere, ingen voice-modell nå). Line/word-document krever gyldige tidsdata i korrekt hierarki; malformed format nedgraderes ikke stille til synkronisert resultat. Plain variant kan bevares separat med tydelig timingdiagnostikk. Word er ikke stavelse eller note.

LyricsSourceSeconds beskriver providers teksttid for den kandidatinnspillingen. Eventuell filoffset normaliseres eksplisitt en gang av parseren og beholdes i proveniens/diagnostikk; parserspesifikk offsetgrense og tapsfri text/timing-policy fryses først ved formatreview. Song.AudioOffsetSeconds anvendes ikke på lyrics ved innhenting. Lydmatch/alignment i Generation avklarer senere mapping mot valgt media; lang/kort varighet er matchavvik, ikke automatisk klipping eller valideringsfeil.

## Søk, valg, feil og manuell fallback

Search er read-only og returnerer bounded immutable kandidater. Velg eksplisitt provider+candidate-ID+variant fra den mottatte siden; manglende/duplisert identitet avvises. Query/page/providerkonsistens valideres. Ingen metadata/media/manuell notetekst/proveniens overskrives av et søk eller valg.

Caller CancellationToken skal propagere OperationCanceledException, inkludert venting/streamread. Separate LyricsSearchError-verdier: NetworkFailure, TimedOut, HttpFailure, RateLimited (optional bounded RetryAfter/statuscode), Unavailable, ResponseTooLarge og InvalidResponse. Caller cancellation er ikke TimedOut. Ingen evig retry; bounded provider-specific timeout/ratepolicy reviewes før adapterkode. En providers feil må ikke skjule en andens resultater eller sperre manual.

ManualLyrics.Create(text) returnerer et eksplisitt LyricsSelection med en Plain-variant med provider manual og lokal kandidat-ID, ingen oppdiktet artist/source-URL/retrieval fra nettet. Brukerens eksplisitte artist/title kan inngå som brukeroppgitt metadata, ikke dokumentert provider-match. Samme tekstgrenser/Unicode-validering; manuell redigering er et nytt eksplisitt dokumentvalg. Ingen note-/phrase-/pitch-opprettelse.

Masterprioritet word→line→plain styrer hvilke tilgjengelige varianter som anbefales etter matchindikasjoner, ikke skjult autoritativ valg. Flere åpne kilder kommer i separate adaptere, manual er alltid tilgjengelig. Generelt nettsøk og lokal transkripsjon fra master er senere eksplisitte pakker, ikke silently dropped; transkripsjon/alignment krever Generation. Masterens confidence kan senere kombinere tekstmatch, ASR og alignment. Første kontrakt rapporterer matchgrunnlag uten ukalibrert sannsynlighet; dette er ikke Note.Confidence/ASR-confidence.

## Lagring – eksplisitt reviewport

Første minste pakke gir in-memory normalized objects/manual valg uten å påstå gjenåpning. Utvalgt tekst skal kunne lagres offline før end-to-end Lyrics Acquisition erklæres ferdig; hele søkecachen behøver ikke persisteres.

Anbefalt generell løsning: senere typed selected-lyrics-felt i Song og eksplicit prosjektformatv3, med frosne DTO-er og v1/v2-migrasjon til null lyrics. Dette bevarer importedSource, manuell lyrics-authoritet og alle sangtyper. Nye fields/enums innføres ikke stilltiende i v2.

Alternativ for et snevert local-media-only trinn: egen frossen lyrics.selection.v1 SourceHeader-profil, tilsvarende RecordingChoices. Rootv2 kan da bevares, men støtter ikke en generell uavhengig redigerbar Lyrics-modell og må ikke omskrive original UltraStar-proveniens. Denne begrensningen er ikke anbefalt som generell modell. Avgrenset in-memory fundament er godkjent; typed selected-lyrics/v3 skal utredes i egen senere ordre. Ingen storage i SourceHeaders/AnalysisArtifact/Note.Text implementeres her. Nye teknologier/lisenser eller vesentlig scope eskaleres til eier; normale eksplicitte migrasjonvalg kan godkjennes innen mandatet.

## Minste arbeidsrekkefølge og eierskap

1. Review dette kontraktforslaget sammen med separat offisiell API-/lisensresearch. Fastsett supported responseprofiler, time/offsetregler, ratepolicy og om interface trenger lookup/fetch. LRCLIB er reviewet som første senere plain/LRC-adapter; ingen adapterkode i fundamentet.
2. SEQUENTIAL felles fundament: nye Acquisition/Lyrics/LyricsContracts.cs, LyricsValidation.cs, ManualLyrics.cs, LyricsSelection.cs og tests/UltraStar.SongCreator.Core.Tests/LyricsContractTests.cs/ManualLyricsTests.cs. Håndskrevne egne lyrics-fixtures under tests/UltraStar.SongCreator.Core.Tests/Fixtures/Lyrics/, ingen hentet sangtekst. Eksisterende SDK-glob kan brukes, ingen pakker. Dette er filforslag, ingen klasser opprettet nå.
3. Etter compilable kontraktcommit/review kan to uavhengige workers tildeles nye egne adapter/parser-filer og tester; konkret provider-ID/filnavn avventer research. Eksempel: en reviewed line-formatparser kontra en HTTP-adapter, bare hvis de bygger uten hverandres ucommittede filer. Delt contracts/validation/fixture/testproject/locks eies bare av integrator. Ikke start parallelt fordi interface bare finnes i denne teksten.
4. Separat persistensordre etter model/schema-review, med offline save/load, klare selected-source-proveniens og undo/redo. Full lyrics-resultat skal ikke serialiseres direkte fra fremtidig bevegelig provider-model.
5. Aggregasjon/ranking og senere GUI-lyricsvalg/alignment bestilles etter egne reviews. GUI-02-funksjoner/master/ADRs bevares.

Fundamenttester: handwritten Unicode/LF/trailing spaces/repetisjoner, plain/manual, line/word gyldige/ugyldige/nullend/simultaneous data, end-/durationbounds, payloadgrenser, instrumental/no-match, duplicate-ID/select/no implicit choice, cancellation og separate failure fixtures. Ingen netttest, medier/modeller eller kvalitetspåstand i denne docs-forberedelsen.

UNRESOLVED: senere parser-offset/HTTP-policy og generell persistens/v3; live-provider/innholdsrettigheter og lydmatch loves ikke. Neste godkjente trinn er rent provider-uavhengig fundament, ingen Generation/GUI/adapter.


## Reviewed presiseringer 2026-10-13

Første senere adapter er LRCLIB plain+LRC line; manual er alltid tilgjengelig. AMLL TTML/lyrics.ovh og senere word-støtte er evaluert/utsatt, ikke fjernet fra master. Provider-reported capabilities lagres separat fra LyricsDocument.SyncLevel: hasWordSync beviser ikke gyldige word-tider. Unsupported-format registreres eksplisitt uten YAML/XML-dependency.

Candidate.TextStatus er Available, Instrumental, Missing eller Unsupported. Available krever minst ett validert dokument. Instrumental er et eksplisitt providerclaim og krever tom dokumentliste; kan velges med null documentId. Missing/Unsupported er resultater, men kan ikke velges som lyrics/instrumental. Tom page er no-match; transportfeil er separat. Vanlig valg krever eksisterende documentId, ingen automatisk variant. Manual krever bare tekst, ingen oppdiktede metadata.

Word-medlemskap er LyricsLine.Words. Line og words har nondecreasing startrekkefølge; word.start>=line.start, og ved kjent line.end skal word.start/end<=line.end. Uten line.end kreves bare global0–14400 og word.end>=word.start; ikke bruk neste linjes start som oppdiktet grense. Manglende sluttider, samtidige/overlappende linjer/ord bevares. Plain har tom Lines; Line har tom Words; Word krever minst ett word i dokumentet, men kan ha lines uten words. Ingen konkatenasjons-/stavelsesantakelse.

Page4MiB er et eksplisitt logisk normaliseringsbudsjett: summen av strictUTF8 bytes for query/proveniens/metadata/IDs/unsupported-formatnavn og alle dokument-/line-/word-tekstfelt, pluss faste strukturbidrag (page/query128, candidate128, capabilities/matchevidence64, document64, line48, word32 bytes). Gjentatt tekst og flere varianter telles hver gang; dokument256KiB-budgettet teller ID/hele teksthierarkiet og samme dokument/line/word-overhead. Høyst3 varianter per kandidat og16 unsupported-formatnavn. Dette er ikke en faktisk JSON-wirelength- eller CLR-minnegaranti; HTTP4MiB er egen senere bodygrense.

NormalizeText normaliserer kun CRLF/CR til LF, avviser ugyldige UTF16-surrogater og bevarer alle andre tegn/spaces/repetisjoner/trailinglinjer. Validerte normaliserte dokumenter krever LF; manuell factory anvender normaliseringen. Immutable arrays/records bevarer valgt kandidat/dokument/proveniens; null/default collections og ugyldige enums avvises.

## Separat SUPERVISOR-review av primærkilder

Dette er kildegrunnlag fra separat read-only research; ingen lyrics-API-kall eller pakker ble hentet i dette fundamentet.

- [LRCLIB API](https://lrclib.net/docs): anonym search, maks20 uten paging; get/get-id er andre operasjoner, duration-matching er ikke opptaks-/lydgaranti. Identifiserende User-Agent, sekvensielle requests/anbefalt200–500ms og429/503 Retry-After avklares i senere transportordre; feilbody kan være HTML.
- [LRCLIB lyricsfile](https://raw.githubusercontent.com/tranxuanthang/lrclib/main/server/src/lyricsfile.rs): millisekundfelter/offset i filformatet, hasWordSync fra ikke-tom wordsliste. [MIT serverkode](https://raw.githubusercontent.com/tranxuanthang/lrclib/main/LICENSE) klarerer ikke sangtekst.
- [lyrics.ovh](https://github.com/NTag/lyrics.ovh): anonym plain lookup, metadata-suggest er ikke lyrics-søk; upstream first-success og404-feilmapping kan skjule kilde/feil. [Implementasjon](https://raw.githubusercontent.com/NTag/lyrics.ovh/master/lyrics.js), [API](https://raw.githubusercontent.com/NTag/lyrics.ovh/master/index.js), [MIT](https://raw.githubusercontent.com/NTag/lyrics.ovh/master/LICENSE) gjelder kode, ikke blanket innholdsrettigheter.
- [AMLL TTML](https://github.com/amll-dev/amll-ttml-db), [formatwiki](https://github.com/amll-dev/amll-ttml-db/wiki/%E6%A0%BC%E5%BC%8F%E8%A7%84%E8%8C%83), [generering](https://raw.githubusercontent.com/amll-dev/amll-ttml-db/main/scripts/rebuild-folder/src/main.rs), [lisens](https://raw.githubusercontent.com/amll-dev/amll-ttml-db/main/LICENSE): community ID-/filbasert database, ikke generelt artist/title-search. Word/stavelse/oversettelse/stemmer og revisjon krever separat adapterproveniens; plattform-ID eller CC0-kode/data betyr ikke bevist lydmatch eller klarerte underliggende lyrics.

Egne syntetiske fixtures, ingen hentet sangtekst i Git/distribusjon. Kode-/innholdsrettighetsdistinksjon følger ADR-0002. Ingen provider default/kvalitetsløfte utover denne reviewed implementeringsrekkefølgen.


## Compilable API og verifisert fundament

Fire implementerte filer under src/UltraStar.SongCreator.Acquisition/Lyrics: LyricsContracts.cs, LyricsValidation.cs, ManualLyrics.cs og LyricsSelection.cs. Ingen nye prosjektfiler, referanser eller pakker.

- ILyricsSearchProvider: ProviderId, SearchAsync(query, cancellationToken). Ingen implementert HTTP-provider; caller-cancellation/OCE og transportpolicy er en kontrakt for senere adapter, ikke testet nettadferd her.
- LyricsValidation.Query/Document/Candidate/Page: validerer immutable kontrakter og logisk budget; feil er LyricsValidationException med LyricsValidationError. NormalizeText normaliserer kun linjeslutt og strict Unicode. Bounded raw input tillater opptil2x dokumentgrensen i UTF16 fordi CRLF kan krympe til LF; endelig UTF8/document-budget avgjør gyldighet.
- ManualLyrics.Create(text): brukerens direkte teksthandling gir Selected plain, generated lokal ID og UTC lokal opprettingstid i RetrievedAtUtc, ingen oppdiktet artist/title/URL, Song-endring eller nett. Metadataoverstyring for manual er ikke implementert i denne minste factory.
- LyricsSelections.Select(page, providerId, candidateId, documentId): validerer hele siden og velger eksakt ID/variant. Instrumental velges uttrykkelig med null documentId; Missing/Unsupported kan ikke velges. Resultatet deler bare immutable records/arrays med kilden; ingen muterbar kopi, oppslagsfetch eller førstevalg.
- LyricsSearchException/Error er typede kontrakter for senere transport. MatchEvidence har kun definerte indikasjoner og optional bounded signed durationdelta, ingen kalkulert score/ranking eller numerisk confidence.

59 nye offline tester, alle teksteksempler egenskrevne/inlined: normalizedUnicode/LF/spaces/repetisjoner/trailinglinjer, CRLF-tung grense, utf8/strukturbudget/allevarianter, word-membership/nullend/overlap/samtidighet/grenser, metadata/enum/null/defaultcollection, status/capabilities, duplicate/absent selection og immutable provenance. Ugyldige surrogater opprettes i testkroppen fordi xUnit argumentserialisering kan erstatte dem; dette gir faktisk malformed-input-test, ikke en erstatningstegn-fixture.

Windows SDK10.0.401: locked solution restore, Release solution build0warnings/errors,549/549 tester (Core481=422+59,Audio17,Desktop51) og solution-format verify-no-changes består. Ingen live lyrics/API, fysisk device/tone/CU eller ny Linux-suite. Eksisterende source/schema/locks og master/ADR beholdes.
