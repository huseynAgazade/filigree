# Sigma rules — LDAP module

Rules match against Filigree normalized events. Use
`filigree-ldap-pipeline.yml` when converting with sigma-cli so payload fields
resolve correctly:

    sigma convert -t <backend> -p detections/sigma/filigree-ldap-pipeline.yml detections/sigma/ldap/

## Rules

| Rule | Level | Detects |
|---|---|---|
| ldap_spn_enumeration | high | Kerberoasting reconnaissance |
| ldap_asrep_roastable_accounts | high | AS-REP roasting target discovery |
| ldap_delegation_enumeration | high | Delegation misconfiguration hunting |
| ldap_broad_directory_enumeration | medium | Bulk collection (SharpHound, ADExplorer) |
| ldap_search_from_unusual_path | medium | LDAP client in a user-writable location |

## Validation status

**All rules are `status: experimental` and have NOT been validated against real
attacker tooling.** They were written from published filter signatures, not from
observed traffic.

Validation requires an Active Directory domain controller. The development lab
uses AD LDS, which lacks the AD schema attributes these rules match
(`servicePrincipalName`, `samAccountType`, `msDS-AllowedToDelegateTo`), so
SharpHound and Rubeus cannot be run against it meaningfully.

Before promoting any rule to `stable`:

1. Run the tool against a real DC with Filigree collecting
2. Capture the .etl as a fixture under `tests/fixtures/ldap/`
3. Confirm the rule fires on that fixture
4. Record the observed filter string in this file

## Tuning

Every rule needs environment-specific tuning before deployment. Baseline your
own LDAP traffic first — `ldap_broad_directory_enumeration` in particular will
fire on legitimate IAM and inventory tooling in most estates.
## Getting the data into your SIEM

These rules match Filigree's normalized JSON. Sigma emits only the detection
clause — you supply the index, sourcetype, and output fields for your
environment.

1. Run Filigree with the `eventLog` or `jsonl` sink enabled.
2. Ship it. Winlogbeat or WEF for the `Filigree` Windows Event Log; any file
   shipper for the JSONL sink.
3. Parse the message body as JSON so nested fields such as `ldap.SearchFilter`
   become queryable.
4. Convert these rules and load them as saved searches or alert rules.

A complete Splunk search, with the Sigma-generated clause on the second line:

    index=wineventlog sourcetype=filigree
    ldap.SearchFilter="*servicePrincipalName=*"
    | table _time, Hostname, Image, User, ldap.SearchFilter

## Conversion

Verified with sigma-cli. Pass the pipeline so payload fields resolve correctly:

    sigma convert -t splunk -p detections/sigma/filigree-ldap-pipeline.yml detections/sigma/ldap/

Without the pipeline, rules reference `SearchFilter` rather than
`ldap.SearchFilter` and will silently match nothing.
