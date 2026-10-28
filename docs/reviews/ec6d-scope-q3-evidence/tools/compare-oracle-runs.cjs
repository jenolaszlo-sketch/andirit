// EC6D-SCOPE-Q3 review helper: compare the independent Q3 oracle run with the
// recorded R2 run (23 cases, tools/q2-typescript-oracle.cjs schema) and the
// recorded Q2 run (14 cases, ts-oracle.cjs style `declarations` schema).
// Usage: node compare-oracle-runs.cjs <q3-run.json> <r2-recorded.txt> <q2-recorded.json> <out.json>
'use strict';
const fs = require('fs');
const q3 = JSON.parse(fs.readFileSync(process.argv[2], 'utf8')).cases;
const r2 = JSON.parse(fs.readFileSync(process.argv[3], 'utf8')).cases;
const q2 = JSON.parse(fs.readFileSync(process.argv[4], 'utf8')).cases;

const diag = (d) => d.map((x) => [x.code, x.file]);
const targetsR2 = (h) => h.map((x) => ({ text: x.text, b: (x.bindsTo || []).map((y) => [y.file, y.kind]) }));
const targetsQ2 = (h) => h.map((x) => ({ text: x.text, b: (x.declarations || []).map((y) => [y.file, y.kind]) }));

function compare(a, b, targetsA, targetsB) {
  const result = { compared: 0, diffs: [] };
  for (const k of Object.keys(b)) {
    result.compared++;
    if (!a[k]) { result.diffs.push({ case: k, diff: 'missing in q3 run' }); continue; }
    const da = JSON.stringify(diag(a[k].diagnostics));
    const db = JSON.stringify(diag(b[k].diagnostics));
    const ha = JSON.stringify(targetsA(a[k].heritage));
    const hb = JSON.stringify(targetsB(b[k].heritage));
    if (da !== db || ha !== hb) {
      result.diffs.push({ case: k, mine: { diag: da, heritage: ha }, recorded: { diag: db, heritage: hb } });
    }
  }
  return result;
}

const r2Result = compare(q3, r2, targetsR2, targetsR2);
const q2Result = compare(q3, q2, targetsR2, targetsQ2);
console.log(JSON.stringify({
  q3Run: { compiler: '5.9.3', cases: Object.keys(q3).length },
  r2Recorded: { cases: r2Result.compared, diffs: r2Result.diffs.length, detail: r2Result.diffs },
  q2Recorded: { cases: q2Result.compared, diffs: q2Result.diffs.length, detail: q2Result.diffs }
}, null, 2));
