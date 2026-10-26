"""Assertions about measured p9/compiler behavior, never future EC-6D behavior."""
import json, pathlib
p = pathlib.Path(__file__).parent
graph = json.loads((p/'p9-adversarial-results.json').read_text())
ts = json.loads((p/'typescript-results.json').read_text())
digest = json.loads((p/'digest-reconciliation.json').read_text())
checks = {}
def check(name, condition):
    checks[name] = bool(condition)
    assert condition, name
def nodes(case, name): return [n for n in graph[case]['nodes'] if n['Name']==name and n['symbol']]
def refs(case, source):
    ns = {n['id']:n for n in graph[case]['nodes']}
    return [e['properties']['module-reference'] for e in graph[case]['edges'] if e['kind']=='resolves-to' and ns[e['source']]['QualifiedName']==source]
check('original-working-bytes-reproduce-both-original-digests',digest['originalWorkingBytesMatchOriginalDigests'])
check('pure-lf-does-not-reproduce-original-digests',not digest['lfMatchesOriginalDigests'])
check('crlf-reproduces-r1-and-repeats',digest['crlfMatchesR1Digests'] and digest['repeatMatches'])
check('all-64-node-and-99-edge-identities-match',digest['identitiesEqual'])
check('all-corpus-byte-differences-are-line-endings',all(f['onlyCrlfDifference'] for f in digest['fileHashes']))
check('alias-collision-is-invalid-typescript',2440 in [d['code'] for d in ts['cases']['collision']['diagnostics']])
check('nested-shadow-is-valid-and-binds-local',not ts['cases']['nested-shadow']['diagnostics'] and ts['cases']['nested-shadow']['heritage'][0]['declarations'][0]['file']=='consumer.ts')
check('merged-interface-is-one-node-two-physical-declarations',len(nodes('merged','Widget'))==1 and len(nodes('merged','Widget')[0]['physicalDeclarations'])==2)
check('nested-export-has-only-nested-widget-node',len(nodes('nested-export','Widget'))==1 and nodes('nested-export','Widget')[0]['QualifiedName']=='Hidden.Widget')
check('nested-export-is-invalid-typescript',2304 in [d['code'] for d in ts['cases']['nested-export']['diagnostics']])
check('ordinary-import-has-resolves-to-but-is-not-export',refs('barrel','ordinary.ts')==['import'])
check('resolved-named-and-star-reexport-kinds-survive',refs('barrel','named.ts')==['export-from'] and refs('barrel','star.ts')==['export-star'])
check('unresolved-reexport-has-no-resolves-to',refs('barrel','missing.ts')==[])
check('idle-import-has-file-resolution-no-implements',refs('idle','consumer.ts')==['import-type'] and not any(e['kind']=='implements' for e in graph['idle']['edges']))
check('export-alias-is-valid-and-binds-internal',not ts['cases']['export-alias']['diagnostics'] and 'Internal' in ts['cases']['export-alias']['heritage'][0]['declarations'][0]['text'])
check('namespace-heritage-is-not-emitted-by-p9',not any(e['kind']=='implements' and e['source']==next(n['id'] for n in graph['unsupported']['nodes'] if n['Name']=='Namespaced') for e in graph['unsupported']['edges']))
check('type-value-distinct-candidates-are-visible',len(nodes('type-value','Widget'))==2)
check('same-file-positive-has-no-import-or-resolution-chain',any(e['kind']=='implements' for e in graph['same-file']['edges']) and not any(e['kind']=='resolves-to' for e in graph['same-file']['edges']))
result = {'classification':'Verified existing behavior / defects and language-oracle results. No GetInterfaceImpactV1 implementation tested.', 'checks':checks, 'count':len(checks)}
(p/'audit-results.json').write_text(json.dumps(result,indent=2)+'\n')
print(json.dumps(result,indent=2))
