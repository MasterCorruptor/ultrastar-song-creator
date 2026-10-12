# Prosjektformat v2 – bevart kildemetadata

v2 viderefører hele song-payloaden og validerings-/filoperasjonskontrakten fra [v1](PROJECT_FORMAT.md). Root har format, schemaVersion (2), song og importedSource. Alle fire felt er påkrevd; importedSource kan være null for sanger uten importkilde. Nye felt blir ikke lagt inn i v1.

importedSource inneholder formatId (påkrevd, ikke tom/whitespace), fileReference (påkrevd, nullable string) og headers (påkrevd array, ikke null). Hver header har name (påkrevd, ikke tom/whitespace) og value (påkrevd string, kan være tom). Rekkefølge, navnenes casing, verdier og duplikater bevares. Parseren bestemmer headeravgrensning; prosjektlageret normaliserer ikke dataene. Ukjente eller manglende felt, null header-elementer og dupliserte JSON-properties avvises.

Kildemetadata er original proveniens. Aktuelle Song.Metadata, Media, offsets og redigerte noter er redigeringsmodellens sannhet. Originale headers overskriver ikke disse ved gjenåpning. Album, cover, creator og ukjente metadata forblir tilgjengelige uten ny innhenting. Kildens dokumentreferanse er kun informasjon; load åpner ingen sang-, bilde- eller mediefil.

For importerte sanger beholdes originale mediereferanser. Relative referanser gjelder original sangfils mappe når importedSource.fileReference finnes; uten slik kilde følger de den ordinære prosjektmappekontrakten. En fremtidig flytte-/pakkeoperasjon må rebasing eller kopiere refererte filer eksplisitt. Absolutte kildereferanser er ikke portable.

## Eksplisitt migrasjon

Leseren velger den strenge DTO-en for deklarert schemaVersion. v1 konverteres i minnet til dagens Song med ImportedSource = null, uten oppdiktet metadata. LoadedProject.SchemaVersion rapporterer original filversjon. Load skriver aldri om originalfilen. Ved eksplisitt Save skrives v2, også etter v1-load; ingen nedgradering til v1 støttes.

Guid-ID-er, song-data og arrayrekkefølge forblir uendret gjennom migrasjonen. Den uavhengige v1-fixturen og v1-DTO-en beholdes. Prosjektets versjon er adskilt fra UltraStar-sangfilens versjon: schemaVersion 2 betyr ikke støtte for UltraStar v2.

## Lokal medieproveniens – Fase 3.1

Eksisterende generisk importedSource kan også ha formatId local-media-v1 etter [source-kontrakten](LOCAL_MEDIA.md). Tekniske felt, tags og eksplisitt stream-valg lagres uten schemaendring. Aktuelle manuelle Song.Metadata-verdier beholder forrang. Load åpner ikke originalmediet eller starter FFprobe; absolutte originalreferanser må fortsatt finnes ved faktisk bruk/eksport. Historiske v1/v2-felter og migrasjon er uendret.

Fase 3.2 kan legge catalog.recording.choice.v1 til den lokale source-profilen, med et eget version=1 og frosne DTO-er etter [katalogkontrakten](RECORDING_SEARCH.md). Dette endrer ikke prosjektets root-schema, opprinnelige lokale tags eller current song. Gjenåpning leser valget offline; provider-metadata overskriver ikke manuelle felt.
