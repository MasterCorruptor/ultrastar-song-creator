# Core- og testdependencies – Fase 2.1

Core har ingen PackageReference; .NET 10s standardbibliotek leverer ImmutableArray. Dette introduserer ingen GUI-, Python-, audio- eller modellruntime.

Testsettet kommer fra SDK-malen med coverage-pakken fjernet. Direkte pakker er xUnit 2.9.3, Visual Studio runner 3.1.4 og Microsoft.NET.Test.Sdk 17.14.1. Faktiske NuGet-arkivers lisensfelt/notices ble kontrollert 2026-10-08. xUnit har Apache-2.0 med medfølgende MIT-notices; VSTest-pakker er MIT. Disse testverktøyene er ikke produkt-runtimepakker.

| Pakke | Versjon | Nupkg-lisensfelt | SHA256 |
|---|---|---|---|
| Microsoft.NET.Test.Sdk | 17.14.1 | MIT | 9995330c5bc5a7bc759caadc9d17748616cdbb983c11062df122a74607614ff0 |
| xunit | 2.9.3 | Apache-2.0 | 04fae949b8e5201ecfa07fa8702baca8c0eb3198114194b3793789cc72bf23d7 |
| xunit.runner.visualstudio | 3.1.4 | Apache-2.0 | 5b54674b105a5f2cbd5e6e0e67956a1b67e082d94eaa7fddbc803110e6eff2d4 |
| Microsoft.CodeCoverage | 17.14.1 | MIT | 7fc432b46f06bd1a0fe3baced8a1269c32f1229c9eb1aab6e931630dd5b8d06b |
| Microsoft.TestPlatform.ObjectModel | 17.14.1 | MIT | 40c7fa3bec34213fb5e8caf3a3bc27f8ddb47f72f5fe60e1b3faa3984a35ad8b |
| Microsoft.TestPlatform.TestHost | 17.14.1 | MIT | d5cc4759cbc7443ee8ad0dc41043cfc55184913a71a26d7fce82020bd4889c9b |
| Newtonsoft.Json | 13.0.3 | MIT | 872fc189e638ab1056555b03aaa38f68bcb54286e221aa646eb1129babf63c77 |
| xunit.abstractions | 2.0.3 | Apache-2.0 (legacy xUnit publisher license URL) | d03d72fc2df8880448f7a81bddb00e1bcb5c18f323c7e7cc69b4cfa727469403 |
| xunit.analyzers | 1.18.0 | Apache-2.0 | 0ce81a98b9df8bd51ae480e6dc9566f4c68e15b4926e6ab997c8f634f3b7a9dd |
| xunit.assert | 2.9.3 | Apache-2.0 | bc760e75ef1b775d293a6afb89301836d3e5a87cec265e31dedd430ee61d0c20 |
| xunit.core | 2.9.3 | Apache-2.0 | aa4550f09c3f2d95a6c62ae43cec22af2edbbd99f722e691ceefeca761fc6a7c |
| xunit.extensibility.core | 2.9.3 | Apache-2.0 | 99ca555fe9b447b1747aff426819db6a2f60b6ee0655caa28c4b91a9ef3d0f48 |
| xunit.extensibility.execution | 2.9.3 | Apache-2.0 | dabc4cb360ede1c01c98e14cc0fe587774693f40679a22fc71794acac5d8d23c |

Lockfilene inneholder også NuGets contentHash og transitive versjoner. Bevar pakkens egne LICENSE/NOTICE ved eventuell redistribusjon; prosjektets MIT erstatter dem ikke. Ingen coverage-, mocking- eller andre assertion-pakker er introdusert.

Kilder: [xUnit 2.9.3-lisens](https://github.com/xunit/xunit/blob/v2-2.9.3/license.txt), [VSTest MIT](https://github.com/microsoft/vstest/blob/main/LICENSE) og de inventarførte nupkg-ene. xunit.abstractions 2.0.3 bruker et eldre licenseUrl-felt som peker på xUnits egen lisens; dette er merket som legacy i tabellen, ikke feilaktig som en innebygd lisensfil.

Fase 2.2 legger til Projects med ProjectReference til Core og ingen PackageReference. System.Text.Json og fil-I/O kommer fra .NET 10. Testsettets eksterne pakker og deres versjoner er uendret; prosjekt-referansen er registrert i oppdaterte lockfiler.
