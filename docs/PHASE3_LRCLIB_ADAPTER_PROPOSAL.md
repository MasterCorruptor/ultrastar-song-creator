# LRCLIB first search adapter – proposal

STATUS: HTTP implementation db51699 is accepted at da3c163 (70 new / 892 total Windows tests). Decoder is integrated in mainc9dc7e4 through PR19. [Current accepted transport profile](LRCLIB_TRANSPORT_PROFILE.md) governs the published work branch; final PR-head review/order precedes the next explicitly owner-approved main integration.

Historical opening status from pre-code preparation: STATUS: HTTP CONTRACT ACCEPTED; HTTP not implemented. Integrated main42eb9f6 contains shared diagnostics/project v3; decoder ce5d84f/2c8498f is locally accepted with 49/822 evidence, publication newly owner-authorized, publication/integration approved after document review. The [local transport pre-code proposal](LRCLIB_TRANSPORT_PROFILE.md) is the accepted bounded pre-code contract; its completed plan preserves the preparation evidence. No HTTP code; standing publication authorization is documented in SUPERVISOR_POLICY.md.

Historisk status fra tidligere adapterforberedelse (følgende seam-/v3-forberedelsesbeskrivelser og suitetall er historiske; gjeldende lokale status/profiler står ovenfor): STATUS: HTTP PROPOSED; shared diagnostics/provider-results seam er kodegodkjent vedf596c4a og inngår i lokal combined review. Samlet773/773-pakke er integrert via [PR18](https://github.com/MasterCorruptor/ultrastar-song-creator/pull/18) som42eb9f6. Sharedseam og v3 er gjeldende baseline; [puredecoderprofil](LRCLIB_RESPONSE_PROFILE.md) er neste avgrensede kodeordre, HTTPtransport fortsattpending. Ingen HTTPkode. Basebe209abd662b7654fb09171aff70479c1698b530 etter [PR17](https://github.com/MasterCorruptor/ultrastar-song-creator/pull/17); reviewed standalone codedfbc33d og parser6c8a563. [Arbeidsordre](PHASE3_LRCLIB_WORK_ORDER.md), [aktiv plan](exec-plans/completed/phase3-lrclib-contract-preparation.md), [lyricsfundament](PHASE3_LYRICS_CONTRACT.md), [LRC-profil](LRC_PROFILE.md). Master/ADR-0002s kode-/innholdsrettighetsgrense består; ingen hentet sangtekst/fixtures/lisenser legges til.

## Separat read-only research: fakta og usikkerhet

SUPERVISOR overførte fullført primærkildereview; AGENT dupliserer ikke providerresearch eller API-kall. [API-docs](https://lrclib.net/docs), [searchroute](https://github.com/tranxuanthang/lrclib/blob/main/server/src/routes/search_lyrics.rs), [arkitektur](https://github.com/tranxuanthang/lrclib/blob/main/ARCHITECTURE.md), [searchindex](https://github.com/tranxuanthang/lrclib/blob/main/server/src/search_index.rs), [errors](https://github.com/tranxuanthang/lrclib/blob/main/server/src/errors.rs).

Dokumentert anonymous GET/api/search: q,track_name,artist_name,album_name; q har forrang, q eller track_name kreves; maks20 uten paging. Identifiserende User-Agent, sekvensielle fullførte requests/anbefalt200–500ms og Retry-After må respekteres. Edge429 kan ha ikke-JSON-feilbody. [] er search-no-match; sourcecode-overload503 har Retry-After1 og search-not-ready kan være500.

Inspektert sourceform er camelCase med id:i64; nullable name/trackName/artistName/albumName/duration/plainLyrics/syncedLyrics/lyricsfile, instrumental/hasWordSync booleans. lyricsfile er INLINE raw YAML, ikke URL. Koden tillater null selv om docs omtaler felt på alle records. Deployed revision og eksakt edge-rategrense er ikke bekreftet; mutable main-links er ikke en pinned produksjonsgaranti. Ingen live API, pakker eller testlyrics ble hentet.

Nedenfor er LOKAL FORESLÅTT policy, ikke LRCLIB-SLA/garanti om opptaksversjon eller lydmatch.

## Minste request/normaliseringsprofil

Valider original LyricsQuery, send kun escaped track_name=Title og artist_name=Artist. Ingen q-fallback/albumgjetting/duration/version/recordingid/limit/offset eller /get-kall. Originalquery/providerorden bevares. Etter validering av ALLE records returneres høyst min(query.Limit,20); malformed tails/duplicate IDs får ikke slippe gjennom limit1.

Response200 er array maks20. Duplicate properties ved alle relevante JSON-nivåer/IDs avvises før selection/limit. Require positiv INTEGER Int64 ID og bool instrumental, ingen coercion. Optional legacy metadata/lyrics/duration kan mangle/være null; hasWordSync absent/null=Unknown, ellers bool. Unknown properties ignoreres innen body/depthbounds. Non-null name/trackName må være ordinal likt; bruk trackName ellers name. Ingen metadata-trimming/artist-title override. Version=null. Kjent duration må være finite positiv0–14400s.

ProviderId lrclib, invariant decimal ID, locally constructed HTTPS https://lrclib.net/api/get/{id} er inert kildeproveniens, ikke en Fetch-kommando. Ett eksisterende UTC receipt-tidspunkt for page/candidates, ingen ny timestamp ved valgt-Core-projection. Matchindikasjoner er Unknown først; kjent durationdelta kan beregnes separat, ingen numeric confidence/ranking/ASR.

## Varianter, status og providerflagg

Nonblank plainLyrics normaliseres LF/strictUnicode og valideres som egen plain-variant; syncdata hentes bare fra reviewed LrcParser. Include line-lrc kun ved Success, aldri inventPlain av feilende LRC. Valid Plain overlever malformed/out-of-range/unsupported LRC med SYNLIG per-variantdiagnostikk. Any TooLarge (input/document/page) er ResponseTooLarge, selv om annen variant er gyldig.

Uten valid document:
- absent/blank legacytekst eller LRC NoLyrics gir Missing, ikke Instrumental.
- eksplisitt unsupported LRC eller nonempty inlineYAML gir Unsupported med korrekt formatnavn.
- malformed/timinginvalid LRC uten valid plain gir InvalidResponse; ingen stille downgrade.
- hasWordSync:true alene kan ikke bevise tilgjengelig/validert tekst eller Word. Missing kan eksistere med reportedWordSync=true.

Instrumental=true krever ingen nonblank legacylyrics og ingen affirmative wordflag; contradictions er InvalidResponse. Nonempty YAML alene er ikke instrumentalcontradiction. Instrumental dokumentliste er tom og velges explicit/null gjennom existing fundament.

hasWordSync lagres bare som reported capability, ingen Word-document. Nonempty lyricsfile registrerer unsupported lyricsfile-yaml uten YAML-parsing/persistens/URL-following, og kan sameksistere med valid Plain/Line. Malformed JSONfeltstyper/Unicode er InvalidResponse, ikke manglende tekst. Råtekst/YAML/payload logges aldri.

## Nødvendig shared-seam review FØR adapter

Faktisk LyricsCandidate/Page har ingen variantdiagnostic-list; LrcParseResult.Diagnostics forsvinner fra ILyricsSearchProvider.Page hvis den ikke projiseres. UnsupportedFormats er capabilities, ikke errorlager. Ingen hidden sidechannel eller skjult parserfeil.

Minste forslag: LyricsCandidate.VariantDiagnostics med default initialized immutable emptyarray. Ny sealed LyricsVariantDiagnostic(FormatId, VariantId?, Code, Severity, PhysicalLineNumber?), enums Severity Info/Warning og Code InvalidSyntax/TimingOutOfRange/UnsupportedFormat/NoLyrics/ExplicitZeroOffset. FormatID tildeles av appen (plain,lrc-simple-line-v1,lyricsfile-yaml), required bounded256UTF16 code units med strictUTF8 bytecount; optional variantID256UTF16 kan beskrive MISLYKKET variant og trenger ikke finnes i Documents. PhysicalLineNumber optional1–20000; null når ukjent. Ingen payload/raw snippets/URLs/fri errorbody/message/timestamps.

Maks64diagnostics per kandidat, ingen null/default/ukjente enums. Pagebudget utvides med32bytes struktur + strictUTF8 format/variantstrings perdiagnostic, tell ALLE entries; dokument256KiB uendret. Presis LrcStatus/code-mapping reviewes: information ExplicitZeroOffset, warning NoLyrics/unsupported/malformed/timing. Failed variant ved gyldig plain er warning, fordi dokument ikke er valgt/validert. Candidate.TextStatus fortsatt Available hvis noe gyldig; diagnostic betyr ikke ASR-confidence.

Existing constructors/defaults beholdes kompatible, egne diagnosticsregresjoner og immutable copying. Selected Core/projection/fremtidig v3 skal IKKE lagre transientdiagnostics, capabilities eller searchcache. Projection validerer riktignok hele candidate gjennom oppdatert validation. Ingen seamkode i denne ordren.

Andre minste felles behov: validation av all20 normalized records FØR requestedlimit uten å forfalske originalquery.Limit. Foreslå internal LyricsValidation.ValidateProviderResults(providerId, originalQuery, allCandidates, providerMaximum=20), som gjenbruker samme private candidate/query-budget/duplicate checks men ikke requested pagecount; normal Page-validering anvendes på returnedsubset. Dette unngår duplisert budsjettformel eller omdøpt query for å komme rundt limit. Root review/fryser begge små seams før dependent adapterpakker.

## Gate, deadline, Retry-After og eierskap

Ny dedikert process-wide LRCLIB-gate, serial lease gjennom headers/bodyread og terminalresponse-disposal. Neste request tidligst500ms etter completion/abort, ikke etter start. Linked30s totaldeadline etableres FØR gate og gjelder cooldown/HTTP/read/bounded parse; caller OCE prioriteres, intern deadline=TimedOut. Tokenchecks før/etter synchronous parsing og mellom records; ingen løfte om hard preemption inne i pure parser. Ingen automatisk retry.

429/503 Retry-After forlenger MAX outstanding sharedcooldown, forkorter aldri eksisterende. Delta/date støttes, pastdate=0. Representable lange cooldowns beholdes i sin helhet, med bounded waitchunks og fortsatt30s callerdeadline; ingen silent downcap. Manglende/invalid header:lokal1s fallback og exposed RetryAfter=null.

Ekstrempolicy til review: syntaktisk valid nonnegative delta som overstiger TimeSpan-representasjon giver ingen1s-downcap; failclosed provider-gate markeres unavailable inntil eksplisitt reset/review, og opprinnelig429/503 beholder RateLimited/Unavailable med null retrymetadata og statisk forklaring uten rawheader. Andre providers/manual forblir tilgjengelige. Invalid syntax/negative/overflowdate bruker deklarert invalid-headerfallback. Test forskjellen; gate må kunne holde langt remaining duration uten overflow ved timestamp-addition. Dette er åpen reviewed-local-policy, ingen providerfaktum.

Production constructor oppretter kontrollert owned HttpClient/handler med redirects=false,cookies=false, normalTLS og ingen personlig auth/key. Det finnes IKKE public bare-borrowed-HttpClient overload i første profil, fordi vilkårlig handler kan følge redirects usynlig. Borrowed testclient tillates kun internal friend-testconstructor med terminalfixturehandler som ikke selv følger redirects; aldri disposed av provider. Test3xx gir HttpFailure og akkurat ett send, ikke targetURI-follow. Owned handlerflags kontrolleres direkte; borrowed policy er testinjection, ingen ukontrollert produksjonsgaranti. Senere public injection krever egen kontrollert pipeline-contractreview.

MusicBrainz-gaten er faktisk startbasert og deadline starter etter EnterAsync; den kopieres ikke uendret som LRCLIB-transport. Handler/client/request/response/body/gatelease cleanup og completiontimestamp skjer også ved cancellation/readfailure; ingen global mutable testbackend. TimeProvider/terminal handlers gir offline deterministiske tests uten nye pakker.

## Body/status/errorpolicy

4MiB bodygrense sjekkes både Content-Length og streamedbytes, strictUTF8, JSONdepth16 og20records. Bare identity/no Content-Encoding i første profil; unsupported encoding/media/schema/depth giver InvalidResponse. Missing Content-Type kan passere strictJSON; explicit non-JSON media-type avvises. Bounded parse/normalisering/variants/diagnostics teller document256KiB/page4MiB, og tailbudget må sjekkes før requestedlimit.

Classify unsuccessful status FØR errorbody, som aldri logges eller kreves JSON.200[] gir tom valid page.404 SEARCH er HttpFailure, IKKE no-match.400/3xx/andre feilstatus HttpFailure,429 RateLimited,503 Unavailable, HTTP/readIO NetworkFailure. Caller cancellation OCE, linkeddeadline TimedOut. Statisk sanitized error/status/retrymetadata, ingen lyrics/privateURLs/bodyexceptiontekst.

## Senere avgrenset rekkefølge/filgrenser

1. SUPERVISOR reviewer denne proposal/diagnostics+all-results seam/transport ekstrempolicy/injection.
2. Integrator eier compilable reviewed LyricsContracts/LyricsValidation-seam og seamtests; commit/review før providerworker.
3. HTTPpakke egne Acquisition/Lyrics/LrclibProvider.cs/LrclibRequestGate.cs/LrclibJson.cs og egne Core.Tests/LrclibProviderTests.cs/LrclibRequestGateTests.cs, egne syntetiske HTTPfixtures. Ingen Song/Projects/v3/Coreselected/parserendring.
4. Song/command/Projects/v3 kan senere være disjunkt workerpakke efter eget stable-core/schema-review. Felles foundation/validation/fixtures/locks eies integrator. Ikke to implementasjoner mot ustabile diagnostics/schema.

Offline acceptance: encoded query/ingen guessedparams; absent/null/invalid metadata/bools/i64/duration/duplicate/20cap/tailsbeforelimit; Plain+LRC success/errors/zerooffsetdiags/YAMLflag/wordreported/statuscontradictions; no payloadlogging/fetch; non-JSONerrorbody/status404/429/503; streamlimit/depth/UTF8/encoding; completion-spacing/concurrentserialization/caller/deadline/read/gatecooldown/long/extremeRetry-After; borrowedownership/ownedflags/no redirects og selectedprojection utendiagnosticpersistens. Ingen faktisk test/APIrequest er utført i denne proposal.

UNRESOLVED: reviewet diagnostic/allresults API og fallback/extremcooldown/injectiondetaljer; ingen eierbeslutning i seg selv. HTTPkode/v3/mediumarbeid venter på ny ordre og faktisk kvotegating etter reset.


## Reviewed shared-seam pakke – 2026-10-14

Denne pakken implementerer kun Acquisition shared diagnostics/provider-results validation før HTTP. Frosne navn: LyricsVariantDiagnostic, LyricsVariantDiagnosticCode og LyricsVariantDiagnosticSeverity i Acquisition.Lyrics; Candidate.VariantDiagnostics default initializedempty ImmutableArray. Recordfields FormatId, VariantId?, Code, Severity, PhysicalLineNumber?. Code: InvalidSyntax/TimingOutOfRange/UnsupportedFormat/NoLyrics/ExplicitZeroOffset; severityInfo/Warning.

FormatId requirednonblank og optional VariantId nonblank hvis oppgitt, høyst256 UTF16 CODE UNITS hver; strictUTF8 BYTE-count er separat budget (opptilflere bytes per tegn). Optional physical line1–20000, maks64 per candidate, null/defaultentry/array og unknownenum avvises som eksisterende InvalidCandidate. Failed variant trenger ikke finnes i Documents. Count32+UTF8FormatId+UTF8VariantId perdiagnostic i candidate/page, uendret256KiB document/4MiB page. Ingen free-form payload/errorbody/messages/URL felter.

Internal LyricsValidation.ValidateProviderResults(providerId, originalQuery, allCandidates, providerMaximum=20) validerer max1–25, query/provider/wholecollection/candidate/duplicate/provider/budget FØR requestedlimit. Shared privatcollectionvalidator brukes også av Page, der originalQuery.Limit og UTC pageinstant fortsatt gjelder. Error: InvalidQuery for query, InvalidPage for provider/collection/max/mismatch, Ambiguous for duplicatecandidate-ID, eksisterende candidate/document/ResponseTooLarge ellers. Ingen fakequery, HTTP-bodyparse eller genericframework.

Eksisterende Acquisition.csproj har InternalsVisibleTo Core.Tests; ingen ny AssemblyInfo/projektendring trengs. Nye egne LyricsVariantDiagnosticTests.cs og LyricsProviderResultValidationTests.cs. Core selected/projection/koder/schema berøres ikke; projection validerer candidate gjennom endret validator og utelater diagnostics fra selectedpayload.

HTTPekstremRetry-After/permanentfailclosed/resetAPI/controlledclientinjection er FORTSATT PENDING transportreview. Ingen slik gate eller transportpolicy er vedtatt/implementert gjennom denne seamoppgaven. Primærkilder og deployed-versionusikkerhet bevares. V3-worker eier isolerte Core/Song/Projects/testfiler, som ikke endres her.


## Faktisk shared-kodecheckpoint

Pre-code seamcommit73bb7bf, deretter Acquisition-only LyricsVariantDiagnostic.cs, Candidate.VariantDiagnostics initializer og centralized LyricsValidation-collection/budget. Existing InternalsVisibleTo brukes uten csproj/AssemblyInfo/pakker. ValidateProviderResults er INTERNAL og returnerer void; original query endres ikke. Page bruker samme privat validator med requestedLimit og eksisterende UTC pageinstant, batchhelper bruker validert providerMaximum1–25 uten lokal truncering.

24 nye egne directcontractcases i LyricsVariantDiagnosticTests.cs/LyricsProviderResultValidationTests.cs dekker defaultcompatibility/immutablevalg/failedvariant/UTF16 og UTF8/enums/null/default/count/line, bytebudget med allevarianter og med diagnostics UTEN documents, malformedtail/duplicate/wrongprovider/count21 ved Limit1, returnedsubset/originalquery og samme Coreselectedpayload med/uten diagnostics. Invalid metadata stopper validering; ingendiagnostics persisteres av projection.

Windows SDK10.0.401 lockedrestore, Release0warnings/errors,696/696 (Core628=604+24,Audio17,Desktop51), solution-format verify-no-changes. Tidlig compile-fixture brukte tvetydig ImmutableArray.Create(null), korrigert med typednull før test; produktseam trengte ingen scopeendring. Ingen ny Linux/native/live-provider/HTTP/v3-påstand. Alle eksisterende filer utenfor shared Contracts/Validation og avgrensede docs bevares, inkl worker-owned Core/Projects/tests/parser/sharedselected/projection/locks/master/ADRs. Ingen v3-worktree/filer er pollet eller integrert.

Sharedseam er ennå ikke reviewgodkjent som dependency for HTTP; faktisk nesteordre kommer fra SUPERVISOR. Ingen permanent failclosed transportgate/resetAPI eller publicclientinjection godkjennes av dette checkpointet.
