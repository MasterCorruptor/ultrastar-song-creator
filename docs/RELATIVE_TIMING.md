# Relativ legacy-tid – avklaring og kompatibilitetsprofil

Prosjekteieren bestilte avklaring og implementering 2026-10-08 etter draft-PR #4. Støtten gjelder unversionerte enkeltstemmer med eksplisitt RELATIVE:YES. Dette er formatkompatibilitet i eksisterende adapter, ikke en ny produksjonsstack eller duettmodell.

## Konklusjon og grunnlag

Den arkiverte unversionerte spesifikasjonen beskriver GAP i millisekunder, men initialiserer samtidig en beat-offset fra GAP uten enhetskonvertering. En bokstavelig implementering ville flytte note-beats og deretter legge lyd-GAP til på nytt.

USDX-kompatibilitet løser dette med initial beat-offset 0 og GAP som separat lydforskyvning. USDX-kilden initialiserer hver stemmes relative offset til 0, legger den til note-start og øker den med markørens andre felt. Tidskonverteringen bruker GAP/1000 separat, med fire ganger header-BPM. USDX avviser relativ modus fra format v1.

Vocaluxe bekrefter initial offset 0 og akkumulering med andre markørfelt for velformede tofeltsmarkører. Det har også reparasjonsregler for manglende felt/for tidlige markører; disse kopieres ikke. Performous endrer offset etter første note når denne starter senere enn 0 og har fallback for en markør med ett felt. Derfor loves ikke identisk relativ tolkning i alle karaokeprogrammer.

Vi velger den dokumenterte USDX-profilen for legacy-import fremfor den enhetsuklare formuleringen. Hver vellykket relativ import gir RelativeTimingCompatibility-warning ved RELATIVE-headeren. Formatkilden endres ikke; avviket gjøres eksplisitt.

## Regel og eksempel

Ved start er rel = 0 beats. For en note brukes absoluttBeat = rel + kildeStart. En relativ markør må ha nøyaktig to ikke-negative heltall: markørBeat og deltaBeat. Markørens tid beregnes fra gammel rel, deretter legges deltaBeat til rel for etterfølgende linjer. Offseten nullstilles ikke ved gjentatt P1. Tomme/gjentatte markører lager ingen frase, men deltaen anvendes fortsatt i filrekkefølge.

SekunderPerBeat = 60 / (4 × BPM). Sangtid = absoluttBeat × sekunderPerBeat; lydtid = sangtid + GAP/1000. GAP inngår ikke i rel. VIDEOGAP følger den ordinære domenekontrakten. Varighet, pitch, tekst og typer behandles som i absolutt modus. Usorterte noter vurderes etter akkumulerte absolutte beats.

Eksempel ved BPM=120 og GAP=1500 ms:

| Hendelse | rel før | Absolutt beat | Sangtid | Lydtid |
| --- | ---: | ---: | ---: | ---: |
| Note med start 4 | 0 | 4 | 0,5 s | 2,0 s |
| Markør med beat 10 og delta 16 | 0 | 10 | 1,25 s | 2,75 s |
| Note med start 0 | 16 | 16 | 2,0 s | 3,5 s |
| Markør med beat 10 og delta 12 | 16 | 26 | 3,25 s | 4,75 s |
| Note med start 0 | 28 | 28 | 3,5 s | 5,0 s |

Manglende RELATIVE, tom header eller NO velger absolutt modus. Ekstra offsetfelt tolkes aldri som en skjult modus. YES sammen med v1 gir UnsupportedRelativeTiming; v1-grammatikken utvides ikke. Manglende/tredje markørfelt, negative verdier og akkumulerte start/end/offsets utenfor den eksakte beatgrensen 2^53−1 avvises med linjenummer og null Song. Ikke-finite markørtider avvises også i absolutt modus.

Original RELATIVE-header og øvrig metadata lagres i prosjektformat v2 sammen med de ferdig konverterte sekundene. Reopen parser ikke kildefilen på nytt. Ingen ny schemaVersion eller runtimepakke kreves for denne støtten.

## Primære referanser og proveniens

Kildene er lest som format-/atferdsevidens. Ingen upstream-kode, GPL-bibliotek eller binærfil kopieres, portereres eller distribueres; egen parser og syntetiske fixtures er skrevet i prosjektet.

- [Arkivert format, Appendix A](https://github.com/UltraStar-Deluxe/format/blob/7328e4df4ad9b88cb7120d4c9d8177a87945a57c/The%20UltraStar%20File%20Format%20%28Unversioned%29.md#semantics), MIT. Commit/hash står i [importprofilen](ULTRASTAR_IMPORT.md).
- [USDX USong.pas](https://github.com/UltraStar-Deluxe/USDX/blob/c88370cc5c8e46f3cb685bd95aeb582fbd5dedb9/src/base/USong.pas): LoadSong/NewSentence/ReadTXTHeader, særlig linje 677–678, 828–838, 1217, 1354–1367 og 1681–1684. GPL-2.0-or-later kildeheader. SHA-256 49284ca9b221934227038c52616be18102215ddc1ea26ef582621f046c8e1296.
- [USDX UNote.pas](https://github.com/UltraStar-Deluxe/USDX/blob/c88370cc5c8e46f3cb685bd95aeb582fbd5dedb9/src/base/UNote.pas#L260): GetTimeFromBeat, linje 260–272. SHA-256 9b748a196c231b66c02d9a02fe3b028d7211e76d0449b78be46fe31596395b6e.
- [Vocaluxe CSongLoader.cs](https://github.com/Vocaluxe/Vocaluxe/blob/e7fc959defa93eca6429246661edbf99c6eea4cf/VocaluxeLib/Songs/CSongLoader.cs): linje 669, 806–809 og 858–872. GPL-3.0-or-later kildeheader. SHA-256 410afa91ad6ae1c1c94b3cea1e1066e4fee915e6399aaa6573c1f9633b797114.
- [Performous songparser-txt.cc](https://github.com/performous/performous/blob/947b1be42d2fc457a4f8016fdd1469be86ca44c2/game/songparser-txt.cc): særlig linje 204, 211–218 og 236. SHA-256 462a6e74b496ad94c320734762e71f1955523b9db7300a5e9382e8bbc81f298a.

## Verifikasjon og begrensning

23 nye tester dekker håndregnede forventningsverdier og ekvivalent absolutt fixture, ikke-null førstestart, flere deltas, tomme/gjentatte markører/P1, GAP/video, fractional comma-BPM/kultur/linjeslutt, errors/akkumulert overflow, v1-/duettavvisning og import → redigering → undo/redo → save/reopen etter at kildeteksten er slettet.

203/203 tester bestod på Windows og nettisolert Linux med SDK 10.0.401. Locked restore, Release build uten warnings og formatkontroll bestod. Dette er statisk avklaring mot pinnede primærkilder og kjørbare regresjonstester i vår adapter; eksterne spillbinærer er ikke kjørt som interop-benchmark. Testmaterialet er syntetisk.
