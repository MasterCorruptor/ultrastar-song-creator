# Fase 3 Lyrics Acquisition – kontraktforslag

STATUS: PROPOSED, avventer SUPERVISOR og ekstern providerreview. Dette er forberedelse, ingen compilable API/implementasjon. [Arbeidsordre](PHASE3_LYRICS_WORK_ORDER.md), [aktiv plan](exec-plans/active/phase3-lyrics-contract-preparation.md). Autoritet: master§6/Fase3, aksepterte ADR-er og [prosjektformatv2](PROJECT_FORMAT_V2.md).

## Faktisk gap ved main50815d8

Song.cs har ingen lyrics-document eller candidate-felt. Note.Text er redigert notetekst; Phrase.Text er avledet concat av noter. AnalysisArtifact har tids-/verdi-punkter og reference, ikke et egnet lyrics-payload eller tekstproveniensformat. Ingen av disse brukes som skjult lager for lyrics.

SourceDocument er original proveniens med ordnede headers, og RecordingChoices/DownloadedMediaDraft bruker eksplisitte frosne v1-profiler. RecordingChoices.Attach krever local-media-v1 og endrer bare proveniens. ProjectJson skriver root-schema2, bruker strenge DTO-er/enums, avviser ukjente properties og leser historiskv1 separat. ProjectStore laster uten medier/nett. Derfor gir eksisterende modeller ingen automatisk generell sangtekstpersistens.

Fase1 [audit](PHASE1_AUDIT.md), [resultater](PHASE1_RESULTS.md) og [lisensmatrise](DEPENDENCIES.md) har tidligere AMLL/LRCLIB/lyrics.ovh-kandidater og begrensede tilgjengelighetsprøver. Disse er historiske indikasjoner, ingen verifisering av dagens APIs, faktisk word-sync eller rettigheter til tekst. Ny offisiell research utføres separat; denne oppgaven dupliserer den ikke.

## Normalisert innhentingsresultat

Foreslått namespace UltraStar.SongCreator.Acquisition.Lyrics. Immutable resultater holdes utenfor Song/History i første fundament. Ingen audiofil/model er nødvendig.

| Objekt | Foreslått innhold |
| --- | --- |
| LyricsQuery | Artist og Title, begge obligatoriske ikke-blanke; optional kjent varighet i sekunder/versionhint/recording-ID; Limit1–25. Ingen personlig nøkkel. |
| LyricsCandidate | ProviderId + opaque Id, artist/title, optional album/version/duration, canonical SourceUrl eller null for manual, UTC RetrievedAt, instrumental-indikasjon, tilgjengelige dokumentvarianter og matchindikasjoner. |
| LyricsDocument | Exact Unicode text (LF-linjeslutt, behold spaces/linjer/repetisjoner), SyncLevel Plain/Line/Word, immutable lines/words, tidsreferanse LyricsSourceSeconds. Ingen syllable-/note-generering. |
| LyricsLine/Word | Text, StartSeconds og optional EndSeconds. Word har line-identitet/index. Manglende endtid beholdes null, aldri oppdiktet fra neste start eller lyd. |
| LyricsPage | ProviderId, Query, immutable Candidates, RetrievedAtUtc, optional provider-pagination/hasMore. Ukjent totalcount er null. Tom side er no-match. |
| LyricsSelection | Eksakt candidate-identitet fra returnert page og eksplicit valgt dokumentvariant, inklusive frosset tekst/tider/proveniens. Aldri automatisk førstevalg. |
| LyricsMatchEvidence | Separate artist/title/version-indikasjoner, optional durationdelta-sekunder og tekstlikhetsindikasjon bare når en faktisk referansetekst foreligger. Manglende signal er Unknown/null. Ingen ASR/alignment-score oppfinnes. |

Minimal provider-uavhengig interface foreslås som ILyricsSearchProvider.ProviderId og Task< LyricsPage > SearchAsync(LyricsQuery query, CancellationToken cancellationToken = default). Adaptere normaliserer sine faktiske lookup/search-forskjeller. Ikke følg lyricsfile/andre vilkårlige URL-er fra svar uten separat godkjent allowlist/transportpolicy. Ingen generell plugin-/fetchframework. Eventuelt behov for separat FetchAsync avklares mot den eksterne providerreviewen før API fryses.

En kandidat kan ha flere varianter (word/line/plain); tilgjengelig kvalitet er basert på validerte tidsdata, ikke bare et providerflagg. En instrumental-kandidat bevares eksplisitt med tomme dokumenter; ukjent instrumental-status er null. Tom lyric-tekst uten instrumental-bekreftelse er ikke en gyldig lyrics-document. Instrumental er ikke no-match/transportfeil og gir ikke tomt sangutkast automatisk.

## Bounds og tidskontrakt

Foreslåtte eksplisitte basisgrenser, reviewes før kode: artist/title/versionhint høyst4096 UTF-16 code units hver; provider/candidate-ID256, source-URL4096. Candidate-collection25 per page. Strict UTF-8 HTTP body høyst4MiB, og samlet normalisert page-payload4MiB; hvert dokument256KiB UTF-8. Høyst10000 lines og50000 words per dokument. Dypde32 for JSON/XML-struktur; DTD/external entities/nett-oppslag for XML er avvist. Ikke truncer skjult ved overskridelse; rapporter ResponseTooLarge/InvalidResponse.

Alle normaliserte tider/duration er finite sekunder, ikke millisekunder/ticks. Duration hvis oppgitt er positiv og høyst14400s. Start/end0–14400, end>=start; startrekkefølge er nondecreasing, samtidig/overlappende lyrics kan bevares (masterduet senere, ingen voice-modell nå). Line/word-document krever gyldige tidsdata i korrekt hierarki; malformed format nedgraderes ikke stille til synkronisert resultat. Plain variant kan bevares separat med tydelig timingdiagnostikk. Word er ikke stavelse eller note.

LyricsSourceSeconds beskriver providers teksttid for den kandidatinnspillingen. Eventuell filoffset normaliseres eksplisitt en gang av parseren og beholdes i proveniens/diagnostikk; parserspesifikk offsetgrense og tapsfri text/timing-policy fryses først ved formatreview. Song.AudioOffsetSeconds anvendes ikke på lyrics ved innhenting. Lydmatch/alignment i Generation avklarer senere mapping mot valgt media; lang/kort varighet er matchavvik, ikke automatisk klipping eller valideringsfeil.

## Søk, valg, feil og manuell fallback

Search er read-only og returnerer bounded immutable kandidater. Velg eksplisitt provider+candidate-ID+variant fra den mottatte siden; manglende/duplisert identitet avvises. Query/page/providerkonsistens valideres. Ingen metadata/media/manuell notetekst/proveniens overskrives av et søk eller valg.

Caller CancellationToken skal propagere OperationCanceledException, inkludert venting/streamread. Separate LyricsSearchError-verdier: NetworkFailure, TimedOut, HttpFailure, RateLimited (optional bounded RetryAfter/statuscode), Unavailable, ResponseTooLarge og InvalidResponse. Caller cancellation er ikke TimedOut. Ingen evig retry; bounded provider-specific timeout/ratepolicy reviewes før adapterkode. En providers feil må ikke skjule en andens resultater eller sperre manual.

ManualLyrics.Create(text) oppretter en Plain-variant med provider manual og lokal kandidat-ID, ingen oppdiktet artist/source-URL/retrieval fra nettet. Brukerens eksplisitte artist/title kan indgå som brukeroppgitt metadata, ikke dokumenteret provider-match. Samme tekstgrenser/Unicode-validering; manuell redigering er et nytt eksplisitt dokumentvalg. Ingen note-/phrase-/pitch-opprettelse.

Masterprioritet word→line→plain styrer hvilke tilgjengelige varianter som anbefales etter matchindikasjoner, ikke skjult autoritativ valg. Flere åpne kilder kommer i separate adaptere, manual er alltid tilgjengelig. Generelt nettsøk og lokal transkripsjon fra master er senere eksplisitte pakker, ikke silently dropped; transkripsjon/alignment krever Generation. Masterens confidence kan senere kombinere tekstmatch, ASR og alignment. Første kontrakt rapporterer matchgrunnlag uten ukalibrert sannsynlighet; dette er ikke Note.Confidence/ASR-confidence.

## Lagring – eksplisitt reviewport

Første minste pakke gir in-memory normalized objects/manual valg uten å påstå gjenåpning. Utvalgt tekst skal kunne lagres offline før end-to-end Lyrics Acquisition erklæres ferdig; hele søkecachen behøver ikke persisteres.

Anbefalt generell løsning: senere typed selected-lyrics-felt i Song og eksplicit prosjektformatv3, med frosne DTO-er og v1/v2-migrasjon til null lyrics. Dette bevarer importedSource, manuell lyrics-authoritet og alle sangtyper. Nye fields/enums innføres ikke stilltiende i v2.

Alternativ for et snevert local-media-only trinn: egen frossen lyrics.selection.v1 SourceHeader-profil, tilsvarende RecordingChoices. Rootv2 kan da bevares, men støtter ikke en generell uavhengig redigerbar Lyrics-modell og må ikke omskrive original UltraStar-proveniens. Denne begrensningen er ikke anbefalt som generell modell. Ingen av alternativene implementeres eller vedtas her; SUPERVISOR reviewer schema/module-scope før bestilling. Nye teknologier/lisenser eller vesentlig scope eskaleres til eier; normale eksplicitte migrasjonvalg kan godkjennes innen mandatet.

## Minste arbeidsrekkefølge og eierskap

1. Review dette kontraktforslaget sammen med separat offisiell API-/lisensresearch. Fastsett supported responseprofiler, time/offsetregler, ratepolicy og om interface trenger lookup/fetch. Ingen provider er vedtatt i dette checkpointet.
2. SEQUENTIAL felles fundament: nye Acquisition/Lyrics/LyricsContracts.cs, LyricsValidation.cs, ManualLyrics.cs, LyricsSelection.cs og tests/UltraStar.SongCreator.Core.Tests/LyricsContractTests.cs/ManualLyricsTests.cs. Håndskrevne egne lyrics-fixtures under tests/UltraStar.SongCreator.Core.Tests/Fixtures/Lyrics/, ingen hentet sangtekst. Eksisterende SDK-glob kan brukes, ingen pakker. Dette er filforslag, ingen klasser opprettet nå.
3. Etter compilable kontraktcommit/review kan to uavhengige workers tildeles nye egne adapter/parser-filer og tester; konkret provider-ID/filnavn avventer research. Eksempel: en reviewed line-formatparser kontra en HTTP-adapter, bare hvis de bygger uten hverandres ucommittede filer. Delt contracts/validation/fixture/testproject/locks eies bare av integrator. Ikke start parallelt fordi interface bare finnes i denne teksten.
4. Separat persistensordre etter model/schema-review, med offline save/load, klare selected-source-proveniens og undo/redo. Full lyrics-resultat skal ikke serialiseres direkte fra fremtidig bevegelig provider-model.
5. Aggregasjon/ranking og senere GUI-lyricsvalg/alignment bestilles etter egne reviews. GUI-02-funksjoner/master/ADRs bevares.

Fundamenttester: handwritten Unicode/LF/trailing spaces/repetisjoner, plain/manual, line/word gyldige/ugyldige/nullend/simultaneous data, end-/durationbounds, payloadgrenser, instrumental/no-match, duplicate-ID/select/no implicit choice, cancellation og separate failure fixtures. Ingen netttest, medier/modeller eller kvalitetspåstand i denne docs-forberedelsen.

UNRESOLVED: gjeldende provider-API/contentlicense/word-format, eksakt parser-/transportprofiler og eksplisitt valgt persistensscope. Neste ordre kan være rent provider-uavhengig fundament etter review; ingen Generation/GUI/providerkode før godkjent kontrakt.
