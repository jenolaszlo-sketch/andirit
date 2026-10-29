# Public API transition

Before inventory: [public-api-before.json](data/public-api-before.json). Exact intended mapping is Penghou.Andirit → Andirit in assembly/type/member signatures; all product short names and general-purpose signatures stay unchanged. After inventory must equal this mapping exactly, including all 139 exported types and declared public members. No added public feature or legacy wrapper layer is authorized.

This is a deliberate pre-1.0 source/binary break. Consumers update package IDs, using directives and fully qualified names, then rebuild all components. Type forwarding cannot preserve the old full names after namespace renaming without retaining a parallel legacy API. Penghou.Andirit packages were unpublished; historical Hetu package identities remain untouched. Data compatibility is qualified separately from CLR binding compatibility.
