// Independent language check; no EC-6D implementation. Pass typescript.js path.
const fs = require('fs');
const path = require('path');
const ts = require(process.argv[2]);
const cases = JSON.parse(fs.readFileSync(path.join(__dirname, 'cases.json'), 'utf8'));
const results = { version: ts.version, cases: {} };
for (const [name, files] of Object.entries(cases)) {
  const dir = path.join(__dirname, 'bin', 'fixtures', name);
  fs.mkdirSync(dir, {recursive:true});
  for (const [file, source] of Object.entries(files)) fs.writeFileSync(path.join(dir,file), source);
  const program = ts.createProgram(Object.keys(files).map(f=>path.join(dir,f)), {noEmit:true,strict:true,target:ts.ScriptTarget.ES2022,module:ts.ModuleKind.CommonJS,moduleResolution:ts.ModuleResolutionKind.Node10,skipLibCheck:true});
  const checker = program.getTypeChecker();
  const heritage = [];
  for (const sf of program.getSourceFiles().filter(f=>path.resolve(f.fileName).startsWith(dir))) {
    function visit(node) {
      if (ts.isExpressionWithTypeArguments(node) && ts.isHeritageClause(node.parent)) {
        const symbol = checker.getSymbolAtLocation(node.expression);
        const target = symbol && (symbol.flags & ts.SymbolFlags.Alias) ? checker.getAliasedSymbol(symbol) : symbol;
        heritage.push({file:path.basename(sf.fileName),text:node.getText(sf),declarations:(target?.declarations||[]).map(d=>({file:path.basename(d.getSourceFile().fileName),kind:ts.SyntaxKind[d.kind],text:d.getText().slice(0,160)}))});
      }
      ts.forEachChild(node,visit);
    }
    visit(sf);
  }
  results.cases[name] = {diagnostics:ts.getPreEmitDiagnostics(program).map(d=>({code:d.code,file:d.file&&path.basename(d.file.fileName),start:d.start,message:ts.flattenDiagnosticMessageText(d.messageText,' ')})),heritage};
}
fs.writeFileSync(path.join(__dirname,'typescript-results.json'),JSON.stringify(results,null,2)+'\n');
console.log(JSON.stringify(results,null,2));
