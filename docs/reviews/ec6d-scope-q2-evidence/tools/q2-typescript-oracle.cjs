// EC6D-SCOPE-Q2 independent TypeScript resolution oracle (review-only tool).
// Usage: node q2-typescript-oracle.cjs <path-to-typescript.js> <cases.json>
// Cases JSON shape: { "<case>": { "<relative-file>": "<source>", ... }, ... }
// Emits a JSON report (compiler version, diagnostics, resolved heritage
// declarations) on stdout. It reads sources only; it never writes to a
// repository other than its own scratch directory under the OS temp area.
'use strict';
const fs = require('fs');
const os = require('os');
const path = require('path');

if (process.argv.length !== 4) {
  console.error('usage: node q2-typescript-oracle.cjs <path-to-typescript.js> <cases.json>');
  process.exit(2);
}
const ts = require(process.argv[2]);
const cases = JSON.parse(fs.readFileSync(process.argv[3], 'utf8'));
const workDir = fs.mkdtempSync(path.join(os.tmpdir(), 'q2-ts-oracle-'));
const report = { compiler: ts.version, node: process.version, cases: {} };

for (const [caseName, files] of Object.entries(cases)) {
  const dir = path.join(workDir, caseName);
  for (const [file, source] of Object.entries(files)) {
    const full = path.join(dir, file);
    fs.mkdirSync(path.dirname(full), { recursive: true });
    fs.writeFileSync(full, source, 'utf8');
  }
  const program = ts.createProgram(Object.keys(files).map((f) => path.join(dir, f)), {
    noEmit: true,
    strict: true,
    target: ts.ScriptTarget.ES2022,
    module: ts.ModuleKind.CommonJS,
    moduleResolution: ts.ModuleResolutionKind.Node10,
    skipLibCheck: true
  });
  const checker = program.getTypeChecker();
  const diagnostics = ts.getPreEmitDiagnostics(program).map((d) => ({
    code: d.code,
    file: d.file ? path.basename(d.file.fileName) : null,
    message: ts.flattenDiagnosticMessageText(d.messageText, ' ')
  }));
  const heritage = [];
  for (const sf of program.getSourceFiles()) {
    if (!path.resolve(sf.fileName).startsWith(path.resolve(dir))) continue;
    const visit = (node) => {
      if (ts.isExpressionWithTypeArguments(node) && ts.isHeritageClause(node.parent)) {
        const symbol = checker.getSymbolAtLocation(node.expression);
        const target = symbol && (symbol.flags & ts.SymbolFlags.Alias) ? checker.getAliasedSymbol(symbol) : symbol;
        heritage.push({
          file: path.basename(sf.fileName),
          text: node.getText(sf),
          bindsTo: (target?.declarations || []).map((d) => ({
            file: path.basename(d.getSourceFile().fileName),
            kind: ts.SyntaxKind[d.kind],
            text: d.getText(d.getSourceFile()).slice(0, 140)
          }))
        });
      }
      ts.forEachChild(node, visit);
    };
    visit(sf);
  }
  report.cases[caseName] = { diagnostics, heritage };
}
console.log(JSON.stringify(report, null, 2));
