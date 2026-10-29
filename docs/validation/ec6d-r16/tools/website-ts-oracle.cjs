'use strict';

// EC6D-R16 website TypeScript oracle (read-only).
//
// Runs the repository-local TypeScript compiler (5.9.3 pinned by the website
// checkout) over the supported ECMAScript source inventory of a pinned source
// snapshot and records the compiler's view of:
//   - declared interfaces and classes, with resolved heritage clauses
//   - named imports with imported/local names, type-only status and resolved
//     relative module targets
//   - semantic references to the selected interface declarations
//
// Usage:
//   node website-ts-oracle.cjs <source-root> <typescript-module-path>
//
// The script only reads files. It does not modify the source snapshot and it
// emits JSON on stdout. The compiler context differs from Andirit's
// tree-sitter profile: the oracle represents TypeScript's semantic view for
// the supported files only; .astro sources are outside TypeScript's program.

const fs = require('fs');
const path = require('path');

const root = path.resolve(process.argv[2] || '.');
const tsPath = process.argv[3];
if (!tsPath) {
  console.error('Usage: node website-ts-oracle.cjs <source-root> <typescript-module-path>');
  process.exit(2);
}
const ts = require(tsPath);

const excludedDirectories = new Set([
  '.git', 'bin', 'obj', 'node_modules', 'dist', 'packages', 'vendor',
]);
const supportedExtensions = new Set([
  '.ts', '.mts', '.cts', '.tsx', '.js', '.mjs', '.cjs', '.jsx',
]);

const files = [];
(function walk(directory, relative) {
  for (const entry of fs.readdirSync(directory, { withFileTypes: true })) {
    const entryRelative = relative ? `${relative}/${entry.name}` : entry.name;
    if (entry.isDirectory()) {
      if (!excludedDirectories.has(entry.name)) {
        walk(path.join(directory, entry.name), entryRelative);
      }
    } else if (supportedExtensions.has(path.extname(entry.name).toLowerCase())) {
      files.push(entryRelative);
    }
  }
})(root, '');
files.sort();

const options = {
  allowJs: true,
  checkJs: false,
  noEmit: true,
  target: ts.ScriptTarget.ES2022,
  module: ts.ModuleKind.ESNext,
  moduleResolution: ts.ModuleResolutionKind.Bundler,
  skipLibCheck: true,
};
const host = ts.createCompilerHost(options);
const program = ts.createProgram(files.map((file) => path.join(root, file)), options, host);
const checker = program.getTypeChecker();

const sourceFiles = program.getSourceFiles()
  .filter((source) => !source.isDeclarationFile || files.some((file) =>
    path.resolve(root, file) === path.resolve(source.fileName)))
  .filter((source) => files.some((file) => path.resolve(root, file) === path.resolve(source.fileName)));
const sourceFileByRelative = new Map(sourceFiles.map((source) => [
  path.relative(root, source.fileName).replace(/\\/g, '/'),
  source,
]));

function relativeName(fileName) {
  const value = path.relative(root, fileName).replace(/\\/g, '/');
  return value.startsWith('..') ? fileName.replace(/\\/g, '/') : value;
}

function lineOf(node) {
  return sourceFileFor(node).getLineAndCharacterOfPosition(node.getStart(sourceFileFor(node))).line + 1;
}

function sourceFileFor(node) {
  let current = node;
  while (current && !ts.isSourceFile(current)) {
    current = current.parent;
  }
  return current;
}

function resolveSymbolAt(node) {
  let symbol = checker.getSymbolAtLocation(node);
  if (symbol && symbol.flags & ts.SymbolFlags.Alias) {
    symbol = checker.getAliasedSymbol(symbol);
  }
  return symbol;
}

function describeTarget(node) {
  const symbol = resolveSymbolAt(node);
  if (!symbol || !symbol.declarations || symbol.declarations.length === 0) {
    return { resolved: false, text: node.getText(sourceFileFor(node)) };
  }
  const declaration = symbol.declarations[0];
  const file = sourceFileFor(declaration);
  return {
    resolved: true,
    text: node.getText(sourceFileFor(node)),
    name: symbol.name,
    declaredIn: file ? relativeName(file.fileName) : null,
    declarationKind: ts.SyntaxKind[declaration.kind],
  };
}

function heritageOf(node) {
  const clauses = [];
  for (const clause of node.heritageClauses || []) {
    const kind = clause.token === ts.SyntaxKind.ImplementsKeyword ? 'implements' : 'extends';
    for (const type of clause.types) {
      clauses.push({ kind, expression: type.getText(sourceFileFor(type)), target: describeTarget(type.expression) });
    }
  }
  return clauses;
}

function resolvedModuleOf(specifier, containingFile) {
  if (!specifier.startsWith('.')) {
    return { external: true, specifier };
  }
  const result = ts.resolveModuleName(specifier, containingFile, options, host);
  const resolved = result.resolvedModule;
  return resolved
    ? { external: false, specifier, resolvedFile: relativeName(resolved.resolvedFileName), extension: resolved.extension }
    : { external: false, specifier, resolvedFile: null };
}

const interfaces = [];
const classes = [];
const imports = [];

for (const [relative, source] of sourceFileByRelative) {
  function visit(node) {
    if (ts.isInterfaceDeclaration(node)) {
      interfaces.push({
        file: relative,
        line: lineOf(node.name),
        name: node.name.text,
        exported: Boolean(node.modifiers && node.modifiers.some((m) => m.kind === ts.SyntaxKind.ExportKeyword)),
        heritage: heritageOf(node),
      });
    } else if (ts.isClassDeclaration(node)) {
      classes.push({
        file: relative,
        line: node.name ? lineOf(node.name) : lineOf(node),
        name: node.name ? node.name.text : null,
        heritage: heritageOf(node),
      });
    } else if (ts.isImportDeclaration(node)) {
      const specifier = ts.isStringLiteral(node.moduleSpecifier) ? node.moduleSpecifier.text : null;
      const clause = node.importClause;
      const entries = [];
      if (clause) {
        if (clause.name) {
          entries.push({
            form: 'default',
            importedName: 'default',
            localName: clause.name.text,
            typeOnly: clause.isTypeOnly,
          });
        }
        if (clause.namedBindings && ts.isNamedImports(clause.namedBindings)) {
          for (const element of clause.namedBindings.elements) {
            entries.push({
              form: element.propertyName ? 'named-aliased' : 'named',
              importedName: (element.propertyName || element.name).text,
              localName: element.name.text,
              typeOnly: clause.isTypeOnly || element.isTypeOnly,
            });
          }
        } else if (clause.namedBindings) {
          entries.push({ form: 'namespace', importedName: null, localName: clause.namedBindings.name.text, typeOnly: clause.isTypeOnly });
        }
      } else {
        entries.push({ form: 'side-effect', importedName: null, localName: null, typeOnly: false });
      }
      imports.push({
        file: relative,
        line: lineOf(node),
        specifier,
        module: specifier === null ? null : resolvedModuleOf(specifier, source.fileName),
        entries,
      });
    }
    ts.forEachChild(node, visit);
  }
  visit(source);
}

const selectedNames = [
  'StatusDefinition', 'ProjectRef', 'SupportingPrimitive', 'RuntimeLayer',
  'Category', 'ProjectSchemaInput', 'BlogPostSchemaInput', 'BreadcrumbItem',
  'NavItem', 'FounderProfile',
];

const interfaceReferences = [];
for (const name of selectedNames) {
  const declaration = interfaces.find((item) => item.name === name);
  const source = sourceFileByRelative.get(declaration.file);
  let declarationNode = null;
  function findDeclaration(node) {
    if (ts.isInterfaceDeclaration(node) && node.name.text === name) {
      declarationNode = node;
      return;
    }
    ts.forEachChild(node, findDeclaration);
  }
  findDeclaration(source);
  const declarationSymbol = checker.getSymbolAtLocation(declarationNode.name);

  const references = [];
  for (const [relative, file] of sourceFileByRelative) {
    function visit(node) {
      if (ts.isIdentifier(node) && node.text === name) {
        let symbol = checker.getSymbolAtLocation(node);
        if (symbol && symbol.flags & ts.SymbolFlags.Alias) {
          symbol = checker.getAliasedSymbol(symbol);
        }
        if (symbol === declarationSymbol) {
          let category = 'type-or-value-reference';
          if (node === declarationNode.name) {
            category = 'declaration-name';
          } else {
            let ancestor = node.parent;
            for (let depth = 0; ancestor && depth < 4; depth += 1, ancestor = ancestor.parent) {
              if (ts.isHeritageClause(ancestor)) {
                category = 'heritage-clause';
                break;
              }
            }
          }
          references.push({ file: relative, line: lineOf(node), category });
        }
      }
      ts.forEachChild(node, visit);
    }
    visit(file);
  }
  interfaceReferences.push({
    name,
    declaredIn: declaration.file,
    declaredAtLine: declaration.line,
    exported: declaration.exported,
    references,
  });
}

const heritageClauses = [...interfaces.map((item) => ({ ownerKind: 'interface', ...item })), ...classes.map((item) => ({ ownerKind: 'class', ...item }))]
  .filter((item) => item.heritage.length > 0);

const report = {
  sourceRoot: root,
  typescriptVersion: ts.version,
  files,
  interfaces,
  classes,
  heritageClauses,
  classesWithImplementsClauses: classes.filter((item) => item.heritage.some((clause) => clause.kind === 'implements')).length,
  imports,
  interfaceReferences,
};

console.log(JSON.stringify(report, null, 2));
