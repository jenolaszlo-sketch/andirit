// EC6D-SCOPE-Q3 review helper: build the exact oracle input manifest.
// Usage: node merge-cases.cjs <q2-cases.json> <r2-cases.json> <supplement.json> <out.json>
// The Q2 corpus is read from the Q2 review branch evidence
// (docs/reviews/ec6d-scope-q2-evidence/cases.json at 1bfcc84); the R2 corpus
// from docs/planning/ec6d/analysis/fixtures/r2/cases.json at 3ce7dd9.
'use strict';
const fs = require('fs');
const q2 = JSON.parse(fs.readFileSync(process.argv[2], 'utf8'));
const r2 = JSON.parse(fs.readFileSync(process.argv[3], 'utf8'));
const extra = JSON.parse(fs.readFileSync(process.argv[4], 'utf8'));
const merged = Object.assign({}, q2, r2, extra);
fs.writeFileSync(process.argv[5], JSON.stringify(merged, null, 2));
console.log('merged cases:', Object.keys(merged).length);
