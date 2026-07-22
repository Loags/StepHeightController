# Repository Guidelines

## Project Structure & Ownership
- Modify gameplay-related scripts, prefabs, materials, and assets inside `Assets/_Project` unless there is a clear reason to work elsewhere.
- Treat `Assets/Plugins/` and `Assets/ThirdPartyAssets/` as external/vendor code. Do not change files there unless the task explicitly requires it, and call out such changes first.
- Most work should stay within the `Assets/_Project` folder structure.
- Preserve Unity `.meta` files whenever scripts or assets are added, moved, or renamed.
- Don't modify `Assets/IX` unless the task explicitly requires it, and call out such changes first.
- For multiplayer work, keep `Assets/_Project/Documentation/Systems/Multiplayer_Networking_Rules.md` and `Assets/_Project/Documentation/Systems/Multiplayer_Documentation_References.md` as the baseline references.

## C# Style & Naming
- Use C# with 4-space indentation and Allman braces.
- Public methods, properties, events, and types use PascalCase.
- Private fields exposed in the Inspector via `[SerializeField]` use camelCase.
- Private fields not exposed in the Inspector use `_camelCase`.
- Prefer `[SerializeField] private` fields over public mutable fields for Inspector wiring.
- Use `[SerializeField, ReadOnly] private` for values that should be visible but not editable in the Inspector. This requires `using IX.CoreIX`.
- Keep comments sparse and useful. Do not add comments that only restate obvious code.
- Do not use the `global::` namespace qualifier in project code.
- Prefer `using` directives and standard namespace resolution.

Correct:
```csharp
[SerializeField] private Image icon;
[SerializeField] private TMP_Text label;
[SerializeField, ReadOnly] private int currentCount;

private bool _isInitialized;
private int _selectedIndex;

public int CurrentCount => currentCount;
public void RefreshView() => label.text = currentCount.ToString();
```

### Formatting
- Use inline (single-line) formatting by default for method and constructor argument lists, simple lambda expressions, and simple guard clauses.
- Keep simple expressions on one line when readability is not harmed.
- Do not split simple expressions across multiple lines.
- Prefer expression-bodied members for simple one-line methods, properties, and accessors.
- Do not convert members with multiple statements, branching logic, or meaningful side effects into expression-bodied form.

Correct:
```csharp
if (target == null) return;
DoSomething(a, b, c);
var result = items.Where(x => x.IsActive).ToList();
private int Count => _items.Count;
private string GetLabel() => _label.text;
```

## Component Design
- Keep `MonoBehaviour` responsibilities narrow and focused.
- Prefer composition over inheritance.
- Put pure logic in separate non-`MonoBehaviour` classes whenever practical.
- Reuse existing systems and patterns before introducing new abstractions.

## References & Initialization
- Prefer explicit references through:
  - Inspector-assigned serialized fields
  - ScriptableObjects where appropriate
  - One-time initialization wiring
- Avoid repeated runtime lookups.
- `GetComponent` is acceptable for same-object references in `Awake`, `Start`, or `OnValidate`.
- Avoid `GetComponent` for cross-object dependencies when a serialized reference is more appropriate.
- Do not use string-based hierarchy or scene lookups such as:
  - `GameObject.Find(...)`
  - `transform.Find(...)`
  - `FindObjectOfType(...)`

## Serialized Reference Policy
- Treat required serialized references as part of the prefab or scene contract.
- Do not add repetitive defensive null-check boilerplate for required serialized UI or prefab-wired references.
- For required same-object references, either:
  - assign/cache them once during initialization, or
  - rely on the prefab contract if that is the established pattern.
- Only add runtime guards for serialized references when:
  - the dependency is genuinely optional,
  - the dependency is cross-object and may legitimately be missing, or
  - the user explicitly asks for fail-safe handling.
- If validation is needed, prefer a single validation point in `Awake`, `Start`, or `OnValidate` instead of repeating guard clauses in every method.
- Do not add fallback UI text or recovery paths for missing localization/UI references unless explicitly requested.
- Do not add extra boolean state purely to suppress repeated logs unless explicitly requested.

## Logging & Error Handling
- Never create `TryResolveReference` functions or similar helper methods for required references.
- Never silently recover from missing required cross-object references.
- Do not add extra debug logging for required reference failures; the resulting exception is desired because it exposes the setup issue immediately.
- Use `LoggerIX.LogError` with `using IX.CoreIX` only for recoverable situations, not for mandatory reference wiring mistakes.
- Prefer straightforward happy-path code when required references are part of the expected prefab/setup contract.

## Localization
- Reuse existing localization helpers, key builders, and table conventions before creating new localization logic.
- Prefer shared helper utilities for repeated localization-key generation logic.
- Avoid hardcoded user-facing strings when the project already localizes the same concept.

## UI & Prefabs
- Do not create UI hierarchies or UI components from code unless explicitly requested.
- Build UI in prefabs and wire references through the Inspector.
- If an existing prefab or reusable UI element may already solve the task, check first and reuse it.
- When changing UI behavior, preserve the existing prefab structure and visual language unless the task explicitly calls for a redesign.

## Testing
- Only add tests when explicitly asked.
- Use `com.unity.test-framework`.
- Put EditMode tests under an `Editor/EditMode` folder and use `*EditorTests.cs`.
- Put PlayMode tests under an `Editor/PlayMode` folder and use `*PlayTests.cs`.
- If existing tests already cover a shared logic path, prefer reusing that path rather than creating duplicate production logic.

## Unity MCP Usage
- Prefer Unity MCP for Unity Editor inspection and validation when available.
- Use Unity MCP before and after Unity-facing changes when the task depends on Editor state, scene state, prefab wiring, asset import state, console errors, or play mode behavior.
- For read-only inspection, use MCP queries such as:
  - editor state, project root, selection, prefab stage, and play mode state
  - console logs and compile errors
  - active scene, scene hierarchy, and build settings
  - asset search, asset info, and component inspection
- Do not use Unity MCP commands that create, modify, delete, save, import, move, or rename assets during read-only tasks.
- Use `Unity_RunCommand` only when the built-in MCP tools cannot answer the question. Keep command scripts narrowly scoped, and avoid modifying scene objects or assets unless the user requested changes.
- After C# or Unity asset changes, use Unity MCP to check the Console for compilation errors when the Editor is available.
- Do not enter Play Mode unless the task requires runtime validation or the user asks for it.
- If Unity MCP is unavailable, mention that Unity Editor validation could not be performed and use filesystem/static checks instead.

## Batch Building vs Unity MCP
- Before requesting or running a Unity batch build, first use the Unity MCP connection to inspect the Editor state, including compile errors, play mode state, logs, and relevant scene or asset data when available.
- Do not request or run a Unity batch build unless the user explicitly asks for a batch build, release build, CI-style validation, or another task that clearly requires batch mode.
- If Unity MCP is unavailable or insufficient, explain what could not be checked and ask before falling back to a batch build.

## Commit Messages
- After completing code or asset changes, always include a suggested Conventional Commit message in the final response unless the user explicitly asks not to.

Summary
The Conventional Commits specification is a lightweight convention on top of commit messages. It provides an easy set of rules for creating an explicit commit history; which makes it easier to write automated tools on top of. This convention dovetails with SemVer, by describing the features, fixes, and breaking changes made in commit messages.

The commit message should be structured as follows:

- Always format the suggested commit message as a fenced text block, even when it only has a subject line.
<type>[scope]: <description>

[body]

[footer(s)] 

The commit contains the following structural elements, to communicate intent to the consumers of your library:

fix: a commit of the type fix patches a bug in your codebase (this correlates with PATCH in Semantic Versioning).
feat: a commit of the type feat introduces a new feature to the codebase (this correlates with MINOR in Semantic Versioning).
BREAKING CHANGE: a commit that has a footer BREAKING CHANGE:, or appends a ! after the type/scope, introduces a breaking API change (correlating with MAJOR in Semantic Versioning). A BREAKING CHANGE can be part of commits of any type.
types other than fix: and feat: are allowed, for example @commitlint/config-conventional (based on the Angular convention) recommends build:, chore:, ci:, docs:, style:, refactor:, perf:, test:, and others.
footers other than BREAKING CHANGE: <description> may be provided and follow a convention similar to git trailer format.
Additional types are not mandated by the Conventional Commits specification, and have no implicit effect in Semantic Versioning (unless they include a BREAKING CHANGE). A scope may be provided to a commit’s type, to provide additional contextual information and is contained within parenthesis, e.g., feat(parser): add ability to parse arrays.

Specification
The key words “MUST”, “MUST NOT”, “REQUIRED”, “SHALL”, “SHALL NOT”, “SHOULD”, “SHOULD NOT”, “RECOMMENDED”, “MAY”, and “OPTIONAL” in this document are to be interpreted as described in RFC 2119.

Commits MUST be prefixed with a type, which consists of a noun, feat, fix, etc., followed by the OPTIONAL scope, OPTIONAL !, and REQUIRED terminal colon and space.
The type feat MUST be used when a commit adds a new feature to your application or library.
The type fix MUST be used when a commit represents a bug fix for your application.
A scope MAY be provided after a type. A scope MUST consist of a noun describing a section of the codebase surrounded by parenthesis, e.g., fix(parser):
A description MUST immediately follow the colon and space after the type/scope prefix. The description is a short summary of the code changes, e.g., fix: array parsing issue when multiple spaces were contained in string.
A longer commit body MAY be provided after the short description, providing additional contextual information about the code changes. The body MUST begin one blank line after the description.
A commit body is free-form and MAY consist of any number of newline separated paragraphs.
One or more footers MAY be provided one blank line after the body. Each footer MUST consist of a word token, followed by either a :<space> or <space># separator, followed by a string value (this is inspired by the git trailer convention).
A footer’s token MUST use - in place of whitespace characters, e.g., Acked-by (this helps differentiate the footer section from a multi-paragraph body). An exception is made for BREAKING CHANGE, which MAY also be used as a token.
A footer’s value MAY contain spaces and newlines, and parsing MUST terminate when the next valid footer token/separator pair is observed.
Breaking changes MUST be indicated in the type/scope prefix of a commit, or as an entry in the footer.
If included as a footer, a breaking change MUST consist of the uppercase text BREAKING CHANGE, followed by a colon, space, and description, e.g., BREAKING CHANGE: environment variables now take precedence over config files.
If included in the type/scope prefix, breaking changes MUST be indicated by a ! immediately before the :. If ! is used, BREAKING CHANGE: MAY be omitted from the footer section, and the commit description SHALL be used to describe the breaking change.
Types other than feat and fix MAY be used in your commit messages, e.g., docs: update ref docs.
The units of information that make up Conventional Commits MUST NOT be treated as case-sensitive by implementors, with the exception of BREAKING CHANGE which MUST be uppercase.
BREAKING-CHANGE MUST be synonymous with BREAKING CHANGE, when used as a token in a footer.