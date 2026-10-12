# Fase 3.2 – innspillingssøk og eksplisitt valg

Bestilt 2026-10-12. Prosjekteieren valgte MusicBrainz/innspillingsvalg først, med mediesøk/nedlasting som neste deloppgave. Fase 3.1 lokal source er integrert. Autoritet: PROJECT_MASTER.md §3–6, §9–10, §15–18 og §22, aksepterte ADR-er og dagens Acquisition-kontrakt.

## Mål og scope

Artist, tittel eller begge skal gi konkrete recording-kandidater med artist-credit, varighet, disambiguation og utgivelseskontekst. Ingen første kandidat velges automatisk. Valgt recording og eventuell utgivelse kan knyttes til et lokalt sangutkast og gjenåpnes uten nettforespørsel eller overskriving av aktuelle manuelle sangdata.

- Utskiftbart IRecordingSearchProvider i Acquisition, med MusicBrainz-adapter.
- Begrenset read-only HTTP/JSON, identifiserende User-Agent, felles rategrense, cancellation/timeout og tydelig feil-/rate-limit-håndtering.
- Bokstavelige søkefelt med korrekt Lucene-/URL-escaping; eksplisitt limit/offset, ingen automatisk crawling.
- Immutable normaliserte recording/artist-credit/release-kandidater, original versjonsinformasjon uten oppdiktet klassifisering eller confidence.
- Eksplisitt recording-ID/valgfri release-ID; ingen title-dedup eller automatisk førstevalg.
- Varig valgt katalogproveniens i eksisterende lokal SourceDocument/prosjektformat v2. Aktuelle metadata, filer, noter/analyse/undo skal ikke muteres.
- Ingen ny dependency, Core-/Projects-/schema-/ADR-/masterendring.

Ingen media-download, lyrics/cover, automatisk lydmatching/versjonsgaranti, generation, GUI, installer eller full Fase 3-avslutning inngår.

## Akseptanse og verifikasjon

Tester skal dekke alle søkemoduser, escaping/Unicode, krediteringer/join phrases, ukjent varighet/video, version/release-kontekst, sider/tomme resultater, feil/timeout/cancellation/bodygrenser/rategrense og eksplisitt valg. Save/load/eksport skal bevare current song og original local-media-proveniens uten reprobe/HTTP.

Kjør locked restore, Release build/test/format på Windows og nettisolert Linux, lokale dokumentlenker, offentlig inventar/privacy, whitespace og beskyttede dokumenter. Gjør én avgrenset live MusicBrainz-prøve hvis tjenesten er tilgjengelig; nettfeil rapporteres som begrensning, ikke som bestått.

Bruk bare core-katalogfelter; supplementary tags/ratings/annotations og score videreføres ikke. Datavilkår og hosted-API-vilkår dokumenteres separat; provider er valgfri, lokal arbeidsflyt består.

## Leveranse og stopp

Avgrenset branch/PR, API-/provenienskontrakt og faktisk verifikasjon. Stopp ved uavklart rettighet, krav om obligatorisk konto/nøkkel, autoritativ konflikt eller nødvendig større offentlig modell-/schema-/scopeendring; avklar med prosjekteieren før slike valg.
