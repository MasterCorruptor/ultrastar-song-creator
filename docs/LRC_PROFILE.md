# Simple line-LRC profil v1

STATUS: IMPLEMENTED CHECKPOINT, compilable parser avventer SUPERVISOR-kodereview. Profilcheckpoint2fad741 ble committet før implementasjon. [Arbeidsordre](PHASE3_LYRICS_LRC_WORK_ORDER.md), [aktiv plan](exec-plans/active/phase3-lyrics-lrc-parser.md), [fundament](PHASE3_LYRICS_CONTRACT.md). Ingen universell LRC-støtte eller nett/parserframework.

## API og resultat

Pure LrcParser.Parse(string text) returnerer immutable LrcParseResult(Status, Document?, NormalizedLrc?, Metadata, Diagnostics). Status: Success/MalformedInput/TimingOutOfRange/TooLarge/UnsupportedFormat/NoLyrics. Bare Success har validert Line LyricsDocument med ID line-lrc, LyricsSourceSeconds, EndSeconds=null, Words=[]. Feil gir ingen skjult Plain eller instrumental. Null argument er programmerfeil/ArgumentNullException. Diagnostics har code og optional1-basert fysisk linjenummer, ingen payloadlogging. Metadata har Name/Value/LineNumber og bevarer rekkefølge/duplikater (unntatt offset, se nedenfor).

## Syntax, bevaring og tidsdomene

Leading ASCII [minutes:seconds], seconds1–2digits/0–59, minutes ASCII nonnegative checked integer og source-tid0–14400sekunder. Optional '.' eller ':' etter seconds etterfulgt av1–3ASCII fraksjonsdigits, base10/sekunder. Ingen comma/kultur/sign/NaN/infinity/klipping. Eksempel [1:02:03] betyr62,03s i denne profilen: kolonfraksjon er entydig definert som mm:ss:fff, ALDRI HH:MM:SS. Tre numeriske kolonfelter kan ikke i seg selv avsløre en avsenders hours-intensjon; faktisk hours-markør eller ekstra separator (f.eks.[01:02:03.456]) er unsupported. Dette syntaksoverlappet rapporteres eksplisitt, ikke som universell hours-deteksjon.

Gjentatte prefikser på fysisk linje ekspanderes uten dedup. Stable chronological sort bevarer fysisk linje/tagrekkefølge ved liktid. LyricsLine.Text er eksakt suffix etter alle tidsstemplene, inkludert spaces og tom tekst. Literalbrackets i suffix er tekst; ekstra leading numeric-timestamp som er malformed avvises. Bare komplett ASCII numeric timestampform i angle-tag med closing > er enhanced/unsupported; literalfragmenter beholdes eksakt (presisert nedenfor). Line-end/words/notes/alignment oppfinnes aldri.

Document.Text projiserer hver fysisk lyricslinjes suffix EN gang, i fysisk rekkefølge, med normalizedLF som separator. Ustemplete blanke fysiske linjer/spaces/trailinglinjer beholdes. Metadata-only linjer utelates fra tekstprojeksjon, men NormalizedLrc og Metadata bevares separat. Sorted/expanded timedlines er ikke Document.Texts fysiske orden. Baremetadata/empty/whitespace/kun tomme timedlyrics gir NoLyrics. En tom timedlinje kan bevares ved siden av faktisk lyric.

## Metadata/unsupported/offset

Kjente lowercase metadata-only tags: ar,ti,al,by,re,ve,au,length og offset. En tag per metadata-only fysisklinje; suffix utenfor tag kan kun være whitespace og beholdes i raw. Value bevares eksakt, ingen metadataoverstyring/queryendring. au:instrumental er vanlig metadata, aldri Instrumental. Unknownextensions, YAML/XML/unstemplet nonblank innhold er UnsupportedFormat med fysisklinjenummer, ikke stripping.

Manglende offset eller en offset:0 beholder source-sekunder; eksplisitt nulloffset gir diagnostic ExplicitZeroOffset og bevart metadata. Nonzero bounded signed Int32 offset er UnsupportedFormat. Malformed/overflow/dupliserte offsettags er MalformedInput. Ikke anvend fortegn eller Song.AudioOffsetSeconds. Nonzero offset krever senere kildebegrunnet review.

## Eksplisitte ressursgrenser

StrictUnicode og bare CRLF/CR→LF-normalisering. Rawinput før normalisering høyst2x256Ki UTF16, normalizedraw høyst256KiB strictUTF8. Fysiske linjer høyst20000, ekspanderte timedlines10000, metadata128 entries og value4096UTF8 bytes. Begrens prefixantall og beregn expansion/documentbudget FØR LyricsLine-records opprettes. Ingen regex/backtracking.

Documentbudget fra fundamentet:64+IDbytes+projectedTextbytes + sum(48+timedLineTextbytes), høyst256KiB. Resultbudget4MiB teller normalizedrawUTF8, samme documentbudget, metadata32+name/valuebytes perentry og diagnostic32 perentry (diagnosticcap64). Ved feil er bare bounded tidligere metadata/raw bevart; oversized/malformedUnicode normalisering kan gi nullraw. Ingen lydvarighet-clipping eller skjult truncering.

## Primærkilde og grenser

SUPERVISOR undersøkte [LRCLIB lyricsfile](https://raw.githubusercontent.com/tranxuanthang/lrclib/main/server/src/lyricsfile.rs) for gjentatte prefikser/minute:second/fraksjon og [API](https://lrclib.net/docs) for syncedLyrics som LRC. Upstream trimming/neste-line-end/instrumentalheuristikk adopteres ikke. Ingen upstreamkode/tekstfixtures kopieres. ADR-0002s skille mellom kode-/innholdsrettigheter består.

Kun egenskrevet offline tekst. Ingen HTTP/storage/GUI/Generation, nye pakker eller endring av reviewed Lyrics-fundament.


## Verifisert kodecheckpoint

LrcParser.cs/LrcParseResult.cs og egen LrcParserTests.cs implementerer profilen uten endringer i reviewed Lyrics-fundament.59 egne offline parser-tests består: dot/colon/no-fraction, multitag/stable ties/nodedup, fysisk Text-rolle/Unicode/LF/spaces/blanktimed/trailinglines, metadata/au/unknown/hours/enhanced/culture, zero/nonzero/duplicateoffset, overflow/timing0–14400 og input/document/expansion/metadata/physical-line-bounds. Nonzero offset avvises etter bounded scanning slik at dupliserte offsettags også gir konkret MalformedInput.

Windows SDK10.0.401 locked solution restore, Release build0warnings/errors og608/608 tester: Core540 (481 tidligere +59 parser),Audio17,Desktop51. Solution-format verify-no-changes består. Ingen Linux/native/live-provider/HTTP/storage/persistensprøve er utført. Fundamentets historiske549-tall endres ikke retroaktivt.

Kjent profiltradeoff: tre rene kolonfelter tolkes som minute:second:fraction; timerintensjon i samme tekst kan ikke oppdages automatisk. Synlig h/H-markør eller ekstra separator er UnsupportedFormat. Dette er eksplisitt reviewpunkt, ingen universell LRC-garanti eller felles API-endring. Senere HTTP/parser/persistenspakker krever egen ordre.


## P2 literalfragment-fix

Ved66fce6c ga Enhanced() false positives for prefix som Egen &lt;3: glede og &lt;1:2 apples. Før retting reproduserte seks positive tester feilen:5 feilet/1 bestod. Scanner gjenkjenner nå bare komplett angle-tag med ASCII minutes1+digits, colon, seconds1–2digits, optional dot/colon+fraction1–3digits, og umiddelbar closing &gt;. Ingen spaces/andre tegn i tagbody. Det gjøres ingen numeric/boundsvalidering eller wordparse: selv komplett &lt;99999:59.1&gt; eller &lt;00:60.1&gt; er UnsupportedFormat, aldri validert Word.

Ufullstendige/ikke-timestampformede literalfragmenter, inkludert &lt;3: glede, &lt;1:2 apples, &lt;3, &lt;00:01. og &lt;00:01.1234&gt;, bevares nøyaktig. Komplette colonfraction-tags avvises også; dette endrer ikke line-timestampens minute/colonfraction-regler eller innfører hours-støtte. Dette er spesifikk syntaxdeteksjon, ingen garanti om å klassifisere alle fremtidige enhanced/hour-formater. Scanner avanserer forbi allerede skannede digit-runs og ser fortsatt en ny nested &lt; ved stoppunktet; ingen quadratic rescan/regex.

Sluttendring:73/73 LrcParserTests,622/622 Windows-regresjoner (Core554,Audio17,Desktop51), Release build0warnings/errors og solution-format består.608/608 over er førfixhistorikk, ikke sluttendringens testtall.14 nye tilfeller tester literaltekst/Unicode/spaces, komplett wordform, out-of-bounds-form og literal+komplett tag i samme suffix. Ingen scope-/dependency-/sharedAPI-endring.
