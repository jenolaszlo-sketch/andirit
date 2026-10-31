using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

// CI-1-SCOPE-R2 "before" reproduction harness.
// The Audit function, helpers, target table and mutation set below are a faithful,
// pure-Roslyn transcription of the cited prior checker (Q1 oracle/Program.cs lines
// 20-53 and 117-153, copied unchanged into the Q2 oracle/challenge). Only the
// plugin-dependent *observation* sections (SameContextPositive, DeclarationConflicts,
// SignatureGaps, PublicExtractionAdversaries) are omitted here; they do not
// participate in Audit() and therefore do not affect the recorded F04 result.
// This file is planning-only validation, not production implementation.
var root = Path.GetFullPath(args[0]);
var output = Path.GetFullPath(args[1]);
var packet = Path.Combine(root, "docs/planning/ci1-scope-01");
var fixtures = Path.Combine(packet, "fixtures");
var references = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!).Split(Path.PathSeparator)
    .Distinct(StringComparer.OrdinalIgnoreCase).Order(StringComparer.OrdinalIgnoreCase)
    .Select(p => MetadataReference.CreateFromFile(p)).ToArray<MetadataReference>();
var hardTargets = new Dictionary<string,string> {
 ["Logger.Write(string)"]="M:Logger.Write(System.String)",
 ["CI1Corpus.Overloads.Hit(int)"]="M:CI1Corpus.Overloads.Hit(System.Int32)",
 ["CI1Corpus.Overloads.Hit(string)"]="M:CI1Corpus.Overloads.Hit(System.String)",
 ["CI1Corpus.GenericExtensions.Echo<T>(T)"]="M:CI1Corpus.GenericExtensions.Echo``1(``0)",
 ["CI1Corpus.GenericExtensions.Mark<T>(this T)"]="M:CI1Corpus.GenericExtensions.Mark``1(``0)",
 ["CI1Corpus.Constructors(int)"]="M:CI1Corpus.Constructors.#ctor(System.Int32)",
 ["CI1Corpus.ConstructorBase(int)"]="M:CI1Corpus.ConstructorBase.#ctor(System.Int32)",
 ["CI1Corpus.Constructors()"]="M:CI1Corpus.Constructors.#ctor",
 ["CI1Corpus.IWorker.Work()"]="M:CI1Corpus.IWorker.Work",
 ["CI1Corpus.Worker.Work()"]="M:CI1Corpus.Worker.Work",
 ["CI1Corpus.Target.Work()"]="M:CI1Corpus.Target.Work",
 ["System.Action.Invoke()"]="M:System.Action.Invoke",
 ["CI1Corpus.Target"]="T:CI1Corpus.Target",
 ["System.Console.WriteLine(string)"]="M:System.Console.WriteLine(System.String)",
 ["CI1Corpus.Partial.Work()"]="M:CI1Corpus.Partial.Work",
 ["CI1Cross.Library.Work() in Lib/Lib.csproj"]="M:CI1Cross.Library.Work"
};
var mainPaths = new[]{"Cases.cs","Partial.One.cs","Partial.Two.cs","Duplicate.One.cs","Duplicate.Two.cs"};
var main = Compile("acceptance", mainPaths.Select(p => Read("acceptance/"+p)));
var enabled = Compile("enabled", mainPaths.Select(p => Read("acceptance/"+p)), defines: ["CI1_EXTRA"]);
var repeated = Compile("repeated", new[]{Read("Repeated.cs"),Read("References.cs")});
var deleted = Compile("deleted", new[]{Read("acceptance/Repeated.AfterDelete.cs.txt")});
var shifted = Compile("shifted", new[]{Read("acceptance/Repeated.AfterShift.cs.txt")});
var lib = Compile("lib", new[]{Read("acceptance/CrossProject/Lib/Lib.cs")});
var app = Compile("app", new[]{Read("acceptance/CrossProject/App/App.cs")}, extra:[lib.ToMetadataReference()]);
var manifest = JsonNode.Parse(File.ReadAllText(Path.Combine(fixtures,"expected-sites.json")))!;
var rows = Audit(manifest);
var mutations = new List<object>();
Mutate("wrong-target", m => m["Sites"]![0]!["ExpectedTargetSourceSignature"]="CI1Corpus.Target.Work()");
Mutate("wrong-coordinate", m => m["Sites"]![0]!["Span"]![1]=10);
Mutate("duplicate-row", m => m["Sites"]!.AsArray().Add(m["Sites"]![0]!.DeepClone()));
Mutate("wrong-overload", m => m["Sites"]![3]!["ExpectedTargetSourceSignature"]="CI1Corpus.Overloads.Hit(string)");
Mutate("wrong-outcome", m => m["Sites"]![0]!["ExpectedOutcome"]="ambiguous-no-confirmation");
// Q2 independently added adversarial mutations (unchanged audit).
Mutate("wrong-end-line", m => m["Sites"]![0]!["Span"]![2]=99);
Mutate("wrong-end-column", m => m["Sites"]![0]!["Span"]![3]=31);
Mutate("wrong-start-line", m => m["Sites"]![0]!["Span"]![0]=6);
Mutate("outcome-confirmed-to-external", m => m["Sites"]![0]!["ExpectedOutcome"]="external-no-confirmed-local-target");
Mutate("outcome-confirmed-to-deferred", m => m["Sites"]![0]!["ExpectedOutcome"]="deferred-method-group");
Mutate("outcome-invalid-enum", m => m["Sites"]![0]!["ExpectedOutcome"]="invalid-q2-outcome");
Mutate("outcome-ambiguous-to-confirmed", m => m["Sites"]!.AsArray().First(s => (string?)s!["ExpectedOutcome"]=="ambiguous-no-confirmation")!["ExpectedOutcome"]="confirmed");
Mutate("outcome-external-to-confirmed", m => m["Sites"]!.AsArray().First(s => (string?)s!["Case"]=="C10")!["ExpectedOutcome"]="confirmed");
var externalCompilation=Compile("external-only",new[]{("App.cs","public class App { public void Run() { System.Console.WriteLine(\"outside\"); } }")});
var externalTree=externalCompilation.SyntaxTrees.Single();
var externalTarget=(IMethodSymbol)externalCompilation.GetSemanticModel(externalTree).GetSymbolInfo(externalTree.GetRoot().DescendantNodes().OfType<InvocationExpressionSyntax>().Single()).Symbol!;
var result=new {Reviewer="CI-1-SCOPE-R2 before-harness: faithful pure-Roslyn transcription of the cited prior checker",Sites=rows,MutationChecks=mutations,CollisionActualTarget=new{Doc=Doc(externalTarget),Assembly=externalTarget.ContainingAssembly.Identity.ToString(),SourceLocations=externalTarget.Locations.Count(l=>l.IsInSource)}};
File.WriteAllText(output,JsonSerializer.Serialize(result,new JsonSerializerOptions{WriteIndented=true})+"\n");
Console.WriteLine(JsonSerializer.Serialize(new{Sites=rows.Count,Failures=rows.Count(r=>r.Errors.Length>0),Mutations=mutations.Count,UndetectedMutations=mutations.Count(m=>!(bool)m.GetType().GetProperty("Detected")!.GetValue(m)!)}));
return rows.Any(r=>r.Errors.Length>0)?1:0;

(string Path,string Text) Read(string path)=>(path,File.ReadAllText(Path.Combine(fixtures,path)));
CSharpCompilation Compile(string name,IEnumerable<(string Path,string Text)> inputs,MetadataReference[]? extra=null,string[]? defines=null) => CSharpCompilation.Create(name,inputs.Select(p=>CSharpSyntaxTree.ParseText(p.Text,new CSharpParseOptions(LanguageVersion.Latest,preprocessorSymbols:defines),p.Path)),references.Concat(extra??[]),new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary,concurrentBuild:false));
static string? Doc(ISymbol? s)=> s is IMethodSymbol m ? (m.ReducedFrom??m).OriginalDefinition.GetDocumentationCommentId():s?.OriginalDefinition.GetDocumentationCommentId();
List<AuditRow> Audit(JsonNode data) {
 var answer=new List<AuditRow>();var keys=new HashSet<string>();
 foreach(var site in data["Sites"]!.AsArray()) {
  var p=(string)site!["FixturePath"]!;var k=(string)site["Case"]!;var lexeme=(string)site["Lexeme"]!;
  var span=site["Span"]!.AsArray().Select(v=>(int)v!).ToArray();var expected=(string)site["ExpectedTargetSourceSignature"]!;var outcome=(string)site["ExpectedOutcome"]!;
  var errors=new List<string>();var text=File.ReadAllText(Path.Combine(fixtures,p));var st=Microsoft.CodeAnalysis.Text.SourceText.From(text);
  int start=0,end=0;try {start=st.Lines[span[0]-1].Start+span[1]-1;end=st.Lines[span[2]-1].Start+span[3]-1;if(st.ToString(new(start,end-start))!=lexeme)errors.Add("coordinate/lexeme mismatch");}catch{errors.Add("invalid coordinate");}
  if(!keys.Add(p+":"+string.Join(',',span))) errors.Add("duplicate fixture site");
  var c=p=="Repeated.cs"?repeated:p.Contains("AfterDelete")?deleted:p.Contains("AfterShift")?shifted:p.Contains("CrossProject")?app:k=="C20"?enabled:main;
  var tree=c.SyntaxTrees.Single(t=>t.FilePath==p);var model=c.GetSemanticModel(tree,ignoreAccessibility:false);
  var node=tree.GetRoot().DescendantNodes().Where(n=>n.Span.Start==start&&n.Span.End==end).OrderBy(n=>n is InvocationExpressionSyntax?0:1).FirstOrDefault();
  node??=tree.GetRoot().DescendantNodes().OfType<ConstructorInitializerSyntax>().SingleOrDefault(n=>n.Span.Contains(start)&&n.Span.End==end);
  SymbolInfo info=default;ISymbol? target=null;
  if(node is TypeOfExpressionSyntax typeOf) target=model.GetTypeInfo(typeOf.Type).Type;
  else if(node is InvocationExpressionSyntax inv && inv.Expression.ToString()=="nameof") {info=model.GetSymbolInfo(inv.ArgumentList.Arguments[0].Expression);target=info.Symbol??model.GetMemberGroup(inv.ArgumentList.Arguments[0].Expression).SingleOrDefault();}
  else if(node is not null){info=model.GetSymbolInfo(node);target=info.Symbol;}
  var doc=Doc(target);var candidateDocs=info.CandidateSymbols.Select(Doc).ToArray();
  if(expected=="unknown-string-or-Exception-overload") {if(target is not null||candidateDocs.Length!=2||!candidateDocs.Contains("M:CI1Corpus.Ambiguous.Pick(System.String)")||!candidateDocs.Contains("M:CI1Corpus.Ambiguous.Pick(System.Exception)")) errors.Add("ambiguity mismatch");}
  else if(expected=="unknown") {if(target is not null||candidateDocs.Length!=0)errors.Add("unresolved mismatch");}
  else if(!hardTargets.TryGetValue(expected,out var expectedDoc)||doc!=expectedDoc)errors.Add("semantic target mismatch");
  if(node is null) errors.Add("syntax missing");
  var positive=outcome.StartsWith("confirmed")||k=="C20";
  if(positive && (target is not IMethodSymbol || node is not InvocationExpressionSyntax))errors.Add("positive must be bound method invocation");
  if(outcome=="ambiguous-no-confirmation"&&candidateDocs.Length!=2)errors.Add("outcome mismatch");
  if(outcome=="unresolved-no-confirmation"&&target is not null)errors.Add("outcome mismatch");
  if(k=="C20" && main.SyntaxTrees.Single(t=>t.FilePath==p).GetRoot().DescendantNodes().OfType<InvocationExpressionSyntax>().Any(n=>n.Span.Start==start))errors.Add("inactive branch unexpectedly active");
  var ownerNode=node?.Ancestors().FirstOrDefault(n=>n is BaseMethodDeclarationSyntax or LocalFunctionStatementSyntax);
  answer.Add(new(k,p,span,lexeme,doc,candidateDocs,info.CandidateReason.ToString(),ownerNode is null?null:Doc(model.GetDeclaredSymbol(ownerNode)),errors.ToArray()));
 }
 return answer;
}
void Mutate(string name,Action<JsonNode> change){var m=manifest.DeepClone();change(m);var failures=Audit(m).Count(r=>r.Errors.Length>0);mutations.Add(new{Name=name,Detected=failures>0,FailingRows=failures});}
sealed record AuditRow(string Case,string Path,int[] Span,string Lexeme,string? Target,string?[] CandidateTargets,string CandidateReason,string? Owner,string[] Errors);
