# Fixtures: Microsoft-Windows-LDAP-Client

## ldap-search-adlds-26200

- **Captured on:** Windows 10.0.26200
- **LDAP server:** AD LDS instance `ADAM_filigree`, localhost:389
- **Partition DN:** CN=filigree,DC=filigree,DC=local
- **Provider:** {099614a5-5dd7-4788-8bc9-e29f43db28fc}, keyword 0x1, level 0xff
- **Client:** System.DirectoryServices.Protocols via tools/capture/query-ldap.ps1

Contains 6 event-30 records. Filters used included the unique markers
FILIGREEMARKERALPHA and FILIGREEMARKERBRAVO so specific events can be located
in tests.

`.etl` is the fixture consumed by tests. `.xml` is a tracerpt -lr rendering
kept as a human-readable reference of the payload shape on this build.

## Known gaps

- `AttributeList` is empty in this capture; no search requested specific
  attributes. A second capture is needed to verify that field populated.
- Captured against AD LDS, not Active Directory. Sufficient for schema
  verification, not for realistic SharpHound/Rubeus detection fixtures.
