import collections,json,pathlib,re
E=pathlib.Path(__file__).resolve().parent
H=E.parent/'records/docs/reviews/CI1A-S2.5-WP-S25-4-QUAL-02-evidence'
def rows(path,pattern):
    return [[c.strip() for c in line.strip().strip('|').split('|')] for line in path.read_text(encoding='utf-8').splitlines() if re.match(pattern,line)]
matrix=rows(H/'matrix.md',r'^\| M\d')
assert len(matrix)==64 and len({r[0] for r in matrix})==64 and all(len(r)==11 for r in matrix)
out=['# QUAL-03 complete 64-row reconciliation','',
'Reviewed production: 25bc6e3b4305c7ce088deb36cfb000138dc76648. Historical dispositions are unchanged in QUAL-02. Current FAIL on M5.1/M5.8 is the newly demonstrated, pre-existing aggregate membership discrepancy Q03-F-01; it does not negate successful R2 missing-unit correction. Required separate Linux and M5.4 evidence remains blocked. Reused evidence is independent QUAL-02 evidence, not producer results. No row is credited solely from R2 producer execution.','',
'| Row | Governing requirement | QUAL-02 | Current production impact | Fresh or reused evidence | QUAL-03 | Residual limitations | Owner action |',
'| --- | --- | --- | --- | --- | --- | --- | --- |']
summary=collections.Counter(); hist=collections.Counter()
for r in matrix:
    ident,authority,fresh,fixture,requirement,expected,old1,old,basis,receipts,limit=r
    current=old; hist[old]+=1
    impact='No change to this governing path; protected-byte comparison and both implementation diffs preserve the accepted premise.'
    evidence=f'Reused independent [QUAL-02 row](../CI1A-S2.5-WP-S25-4-QUAL-02-evidence/matrix.md); fresh [regression verification](regression-verification.json).'
    owner='Retain existing qualified scope; no formal closure issued.'
    group=int(ident.split('.')[0][1:])
    if group in [1,2,3,4,5,6,7,8,9,10,11,13,15]:
        if group in [1,2,3] or ident in ['M4.3','M4.4','M6.1']:
            impact='R2 leaves identity formulas and live resolver unchanged; new extraction branch must preserve positive binding and ownership.'
            evidence='Fresh [50-site independent reconstruction](independent-b3-identities.json), b3/b3-mixed publish/reopen; control-provenance-publish; identity-discriminators. Reuse unchanged historical tuple/adversary basis.'
        if ident in ['M2.3','M2.4','M6.4','M6.5','M9.8']:
            impact='Merge/conflict implementation unchanged; zero-fact publication must preserve accepted contributor and rejection behavior.'
            evidence='Fresh inherited-harness b6 union/delete/reopen and b7 conflict/reopen controls; state-known publish/reopen. Reuse QUAL-02 real distinct-owner deletion proof (unchanged lifecycle).'
        if ident in ['M6.2','M6.3']:
            impact='R2 adds successful partial replacement; source-owner replacement/deletion implementation unchanged.'
            evidence='Fresh review-transition publish/reopen and b6 deletion; reuse QUAL-02 state-lifecycle-deletedreopen genuine distinct-owner proof.'
        if group==7:
            impact='New partial zero-fact unit uses unchanged sink, transaction, page/meta and restoration paths.'
            evidence='Fresh control-incomplete/declared/multiple/mixedmissing and review-transition publish/reopen; b3, b7, state-known/unknown receipts. Reuse accepted transaction/source audit.'
        if group==8:
            impact='R2 occurrence branches must not alter graph or native mirror results.'
            evidence='Fresh [D3 graph/mirror replay](probes/corrected/d3.json); source diff adds no graph representation.'
        if group==9 and ident not in ['M9.5','M9.8']:
            impact='Partial publication interacts with freshness, authority and recovery; existing reader ordering remains unchanged.'
            evidence='Fresh review-transition, state-known/unknown/supersession publish/reopen, d4-schedule and b3-reopen pagination; focused publication/native gates. Existing row-specific QUAL-02 scope retained.'
        if group==10 or group==13 or group==15:
            impact='Schema, namespace and codecs unchanged; partial path must not weaken refusals or authentic historical behavior.'
            evidence='Fresh d5-schema/namespace/page, review-none/refusal/transition, historical/sq04-pre-rem and sq04-graph-only, historical/sq04-integrity.json. Reuse immutable codec and writer-lineage audit.'
        if ident in ['M11.1','M11.2','M11.3']:
            impact='Changed producer work measured on the existing finite fixture; no new performance threshold.'
            evidence='Fresh d3.json publication/reopen samples and b3-reopen.json target-page timings/Examined; reuse accepted workload and no-optimization boundary.'
    if ident in ['M4.1','M4.2','M5.5']:
        current='PASS'; impact='F-S254-02 corrects bound unsupported-site counting; ordinary facts preserved.'
        evidence='Fresh independent control-ctor/property/reference plus constructors/properties/groups/overlap/mixed/unbound/ambiguous/external/unrelated publish/reopen; supplemental delegate overlap; independent B3 reconstruction.'
        limit+='; bounded site definitions, Windows only; unrelated Q03-F-01 complete membership issue assessed at M5.1/M5.8.'
        owner='Record technical correction qualification; formal finding disposition remains separate.'
    if ident=='M5.7':
        current='PASS'; impact='F-S254-03 retains missing declaration and publishes only zero-fact Partial coverage.'
        evidence='Fresh absent-project, missing-source, multiple and mixedmissing 3/2 or 4/2 receipts; review-transition/refusal/none publish and fresh-process reopen.'
        limit+='; complete-path membership discrepancy is separately Q03-F-01, not a missing-unit failure.'
        owner='Record bounded technical correction qualification; retain other coverage blockers.'
    if ident in ['M5.1','M5.8']:
        current='FAIL'; impact='Fresh complete/complete-empty boundary reveals pre-existing overlapping whole-universe counts, also present on R1.'
        evidence='Fresh [membership-counterexample.json](membership-counterexample.json): two actual members report 4/4; three report 9/9. Correct facts/Available distinction preserved.'
        limit+='; Q03-F-01 blocks correct complete-scope aggregate membership, not occurrence identity or false completeness.'
        owner='Authorize bounded count-accounting investigation/remediation and independent requalification; do not weaken merge or authority.'
    if ident=='M5.2':
        impact='R1 PE classification path unchanged; independently preserved against R2.'
        evidence='Fresh b3 and b3-mixed extraction/query/native/reopen: UnsupportedExternalEvidence, External=1, Unresolved=0 in PE-only control, no external fact; both R1 tests.'
    if ident=='M5.3':
        impact='Ambiguous binding remains conservative; no candidate promotion.'
        evidence='Fresh b3-mixed and multiple-definitions publish/reopen; live ambiguity is isolated defense and separately integrated ambiguous counters, as accepted previously.'
    if ident=='M5.6':
        impact='Bounded inactive scanner unchanged.'; evidence='Fresh control-inactive publish/reopen and extraction suite; preserve existing heuristic limits.'
    if ident=='M5.4':
        current='BLOCKED'; impact='No M5.4 code change. Exact continuous resolver Missing outcome not established.'
        evidence='Fresh bounded transitive inheritance/returned instance/interface and direct controls; [M5.4 analysis](m54-analysis.md); isolated defense replays separately labeled.'
        limit='Technical disposition D; no exhaustive compiler symbol-association proof and no valid continuous receipt. Ordinary unresolved counts are insufficient.'
        owner='Commission the narrowly specified compiler-association investigation or explicitly clarify/amend the requirement.'
    if ident in ['M11.4','M14.1']:
        current='BLOCKED'; impact='R1/R2 managed producer and coverage payload differ from accepted Ubuntu revision.'
        evidence='Reuse historical Ubuntu run only at its accepted revision; no corrected-revision Linux execution authorized or claimed.'
        limit='Corrected genuine-Linux delta and F-G2-01 remain outstanding.'; owner='Authorize pinned genuine Ubuntu combined Linux/coverage route after bounded functional resolution.'
    if ident=='M14.2':
        impact='Native library/platform mechanisms unchanged; historical documentation remains applicable within its stated scope.'
        evidence='Reused accepted SQ-05 locking/WAL/mapping documentation; source audit confirms no native/package change.'
        owner='Preserve historical platform qualifications; no claim of corrected-revision execution.'
    if current=='DEFERRED':
        impact='No Stage 6 implementation or expanded acceptance.'; evidence='Reused explicit Stage 6 owner deferral.'
        owner='Separate Stage 6 commission.'
    summary[current]+=1
    out.append('| '+' | '.join([ident,requirement,old,impact,evidence,current,limit,owner])+' |')
assert hist=={'PASS':42,'PASS WITH QUALIFICATION':10,'FAIL':4,'BLOCKED':3,'REUSED QUALIFIED':1,'DEFERRED':4},hist
out+=['','Counts: '+json.dumps(dict(summary),sort_keys=True)+'. Total 64, unique IDs 64.']
(E/'matrix.md').write_text('\n'.join(out)+'\n',encoding='utf-8')

obligations=rows(H/'obligations.md',r'^\| (?!Obligation\b|---)')
assert len(obligations)==78 and len({r[0] for r in obligations})==78 and all(len(r)==11 for r in obligations)
out=['# QUAL-03 complete 78-entry obligation reconciliation','',
'Historical entry and QUAL-02 classifications remain preserved in their original records. The six governing classification names are retained. Counts refer to qualification routing, not new owner closure. Finding overlays follow the table and do not create duplicate inventory entries.','',
'| Obligation | Governing source | QUAL-02 | QUAL-03 | Current evidence and impact | Inherited limitation | Owner action | Owner |',
'| --- | --- | --- | --- | --- | --- | --- | --- |']
osummary=collections.Counter()
for r in obligations:
    name,source,entry,q1,prior,old,evidence,impact,limitation,action,owner=r
    current=old
    impact='Reused independent QUAL-02 basis with original owner decision and limitation; implementation diff and execution-source verification establish unchanged applicable premise.'
    if name in ['F03','F04','NEW-04','IF-03']:
        current='BLOCKED'; impact='R2 missing-unit and unsupported corrections independently pass. Q03-F-01 complete membership counts remain incorrect; M5.4 continuity remains unresolved where applicable. See matrix, membership-counterexample and implementation audit.'
        action='Commission bounded complete-membership accounting correction; preserve accepted disjoint Merge semantics and resolve M5.4 separately.'
    elif name in ['F06','F16','I-01','Required evidence, fixture, runner or independence gap']:
        current='BLOCKED'; impact='Independence/integrity gates passed; two R2 corrections qualified. Remaining Q03-F-01, M5.4, corrected Linux, exact-blob formatter exit 2 versus checkout-profile exit 0, and coverage/owner prerequisites prevent acceptance.'
        action='Resolve named blockers and obtain separate owner acceptance; no qualification bypass.'
    elif name in ['F05','NEW-01','SQ-02','SQ-03','D4.1–D4.10']:
        impact='Fresh state-known/unknown/supersession and d4-schedule replays preserve managed recovery, quarantine, attribution and serialized freshness; review-transition adds successful partial replacement.'
    elif name in ['B3','S-01','F11','F12']:
        impact='Fresh independent 50-site B3 reconstruction, R1 metadata classification and actual captured reference inputs preserved. Host-scoped target boundary and canonical-form qualifications remain.'
    elif name in ['B6','B7','IF-01']:
        impact='Fresh constructed union/deletion/conflict replays and known-fault conflict controls pass; prior distinct-owner lifecycle evidence remains applicable.'
    elif name in ['IF-02','IF-04','F10','SQ-07','MD-02']:
        impact='Fresh B3 pagination/order/Examined, supersession and D3 workload timings preserve bounded behavior; existing optimization/performance limitations retained.'
    elif name in ['SQ-04','SQ-06','D5','IF-05','I-03','M-01','F15']:
        impact='Fresh schema/namespace/page refusals, none/partial/stale distinctions and authentic disposable-copy checks; master hashes unchanged. Prior amended SQ-04 scope retained.'
    elif name=='SQ-05':
        impact='Prior bounded Ubuntu acceptance preserved at 7457284451dff8ddb0417f83ecc19097103316fc only. R1/R2 delta requires genuine Ubuntu 52-test native floor plus classification/partial publication/reopen controls.'
        action='Separately authorize successor Linux delta; Windows is not Linux evidence.'
    elif name=='F-G2-01':
        current='OPEN-S2.5'; impact='Last independently measured 296/450=65.77% against 70%; R2 numerator/denominator not measured or assumed. No threshold/filter/CI change.'
        action='Separate bounded test-only coverage commission and existing-policy successor measurement; repository CI is not fully green.'
    elif name=='MD-01':
        impact='810 legacy manifest entries reverified, reviewed revision/source bindings checked, additive QUAL-03 self-excluding seal and commit/post-commit procedure supplied.'
    if current=='OPEN-STAGE6':
        impact='Explicit Stage 6 deferral preserved. Fresh managed fault and finite graph/platform evidence does not discharge cancellation, full D3/S-02, crash/power-loss or full platform certification.'
    if current=='AMENDMENT-SUBJECT':
        impact='Existing SQ-04 amendment and authentic-artifact discovery trigger preserved; no amendment made by this commission.'
    osummary[current]+=1
    out.append('| '+' | '.join([name,source,old,current,impact,limitation,action,owner])+' |')
out+=['','Counts: '+json.dumps(dict(osummary),sort_keys=True)+'. Total 78, unique entries 78.','',
'Finding overlays: F-S254-01 preservation independently verified, no formal closure. F-S254-02 and F-S254-03 SUPPORTED WITH QUALIFICATIONS for their bounded Windows corrections. Q03-F-01 OPEN (pre-existing complete-universe aggregate membership defect). M5.4 technical D and governance BLOCKED. Corrected Linux M11.4/M14.1 BLOCKED. F-G2-01 OPEN. Owner acceptance and all named Stage 6 routes remain separate.']
(E/'obligations.md').write_text('\n'.join(out)+'\n',encoding='utf-8')
(E/'reconciliation-summary.json').write_text(json.dumps(dict(matrixHistorical=dict(hist),matrixCurrent=dict(summary),obligations=dict(osummary),matrixRows=64,obligationEntries=78),indent=2)+'\n')
print(json.dumps(dict(matrix=dict(summary),obligations=dict(osummary))))
