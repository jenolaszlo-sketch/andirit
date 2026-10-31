# F01 collision fixture (R2 adversarial, planning-only)

These sources reproduce the F01 documentation-ID collision that the R2 oracle
uses to prove that an externally bound call stays external even when an
unrelated *indexed* local declaration shares its documentation ID.

- `A/Shadow.cs` declares `System.Console.WriteLine(string)` in its own project
  `Shadow`. When `Shadow` is indexed, this is an eligible local definition whose
  canonical documentation ID is `M:System.Console.WriteLine(System.String)`.
- `Z/App.cs` calls the platform `System.Console.WriteLine("outside")` and does
  **not** reference `A/Shadow`. The call binds to
  `System.Console, Version=10.0.0.0, PublicKeyToken=b03f5f7f11d50a3a`, which has
  no source location.

The documentation IDs coincide; the definitions do not. The R2 oracle derives
`external` for the call from the bound target's containing assembly and source
locations, and never promotes it to `confirmed` from the coincident ID.

These files are inputs to `analysis/oracle/Program.cs`. They are not production
source and are never compiled into the product.
