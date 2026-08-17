"""
AST-based audit checks for the App Template.

Optional add-on to `audit.py`, enabled with `--ast`. When the tree-sitter Python
bindings are installed (alongside `tree_sitter_c_sharp` +
`tree_sitter_typescript`), audit.py loads this module and runs the four checks
below — each one needs real syntax-tree context that a regex cannot supply.

  C# checks:
    - cs/missing-authorize  — public methods in a `*Controller` class with no
      [Authorize] / [RequireAccessFunction] / [AllowAnonymous] at method OR
      class level
    - cs/unbounded-take     — `.Take(N)` with an integer literal above 100,
      which usually means the page size did not come from PagedSearchDto

  TS checks:
    - ts/as-any             — `as any` casts
    - ts/unvalidated-json   — a `.json()` call with no parse / type guard in the
      surrounding source

If the bindings are not installed, audit.py logs a one-line warning and falls
back to its built-in regex checks. Install:

    pip install tree_sitter tree_sitter_c_sharp tree_sitter_typescript

This module is import-safe: importing it without the libs raises ImportError,
which audit.py handles.
"""
from __future__ import annotations

from dataclasses import dataclass
from pathlib import Path

import tree_sitter
import tree_sitter_c_sharp
import tree_sitter_typescript

CS_LANGUAGE = tree_sitter.Language(tree_sitter_c_sharp.language())
TS_LANGUAGE = tree_sitter.Language(tree_sitter_typescript.language_typescript())


@dataclass
class Finding:
    file: str
    line: int
    column: int
    rule: str
    message: str

    def __str__(self) -> str:
        return f"{self.file}:{self.line}: [{self.rule}] {self.message}"


# ---------------------------------------------------------------------------
# C# helpers
# ---------------------------------------------------------------------------

# Attributes that mark a controller method as "auth was thought about here":
# Authorize / RequireAccessFunction enforce, AllowAnonymous explicitly opts out.
# Either way the developer made a deliberate choice — that's what we want.
ATTRIBUTE_NAMES_AUTH = {"Authorize", "RequireAccessFunction", "AllowAnonymous"}


def _node_text(src: bytes, node: tree_sitter.Node) -> str:
    return src[node.start_byte:node.end_byte].decode("utf-8", errors="replace")


def _walk(node: tree_sitter.Node):
    yield node
    for c in node.children:
        yield from _walk(c)


def _attribute_names(src: bytes, attribute_list_node: tree_sitter.Node) -> set[str]:
    """Given an `attribute_list` node, return the set of attribute names (without args)."""
    out: set[str] = set()
    for c in _walk(attribute_list_node):
        if c.type == "attribute":
            # First child is usually the name (identifier or qualified_name)
            name_node = c.child_by_field_name("name") or (c.children[0] if c.children else None)
            if name_node is not None:
                name = _node_text(src, name_node).strip().split("(")[0]
                # Trim possible namespace qualification (Microsoft.AspNetCore.Authorization.AuthorizeAttribute)
                short = name.rsplit(".", 1)[-1].removesuffix("Attribute")
                out.add(short)
    return out


def _siblings_attribute_lists(node: tree_sitter.Node) -> list[tree_sitter.Node]:
    """Return attribute_list nodes that decorate `node` (immediate siblings preceding it)."""
    out: list[tree_sitter.Node] = []
    parent = node.parent
    if parent is None:
        return out
    seen_target = False
    children = list(parent.children)
    target_idx = next((i for i, c in enumerate(children) if c.id == node.id), -1)
    if target_idx < 0:
        return out
    # Walk backwards collecting attribute_list nodes
    for i in range(target_idx - 1, -1, -1):
        if children[i].type == "attribute_list":
            out.append(children[i])
        else:
            break
    # Also consider direct children of the node itself (some grammars attach attributes inside)
    for c in node.children:
        if c.type == "attribute_list":
            out.append(c)
    return out


def _direct_children_attribute_lists(node: tree_sitter.Node) -> list[tree_sitter.Node]:
    return [c for c in node.children if c.type == "attribute_list"]


def _has_auth_attribute(src: bytes, node: tree_sitter.Node) -> bool:
    """Check `node` and its predecessor attribute_list siblings for an auth attribute."""
    candidate_lists = _siblings_attribute_lists(node) + _direct_children_attribute_lists(node)
    for al in candidate_lists:
        names = _attribute_names(src, al)
        if names & ATTRIBUTE_NAMES_AUTH:
            return True
    return False


# ---------------------------------------------------------------------------
# C# checks
# ---------------------------------------------------------------------------

def check_csharp_controller_authz(file: Path, src: bytes,
                                  tree: tree_sitter.Tree) -> list[Finding]:
    """Find public methods inside `*Controller` classes that are not covered by
    [Authorize] / [RequireAccessFunction] at either method or enclosing class."""
    findings: list[Finding] = []
    root = tree.root_node
    for node in _walk(root):
        if node.type != "class_declaration":
            continue
        name_node = node.child_by_field_name("name")
        if name_node is None:
            continue
        class_name = _node_text(src, name_node)
        if not class_name.endswith("Controller"):
            continue
        class_has_auth = _has_auth_attribute(src, node)

        # Walk method declarations inside the class
        body = node.child_by_field_name("body") or node
        for m in _walk(body):
            if m.type != "method_declaration":
                continue
            modifiers = [c for c in m.children if c.type == "modifier"]
            if not any(_node_text(src, mod) == "public" for mod in modifiers):
                continue
            mname_node = m.child_by_field_name("name")
            if mname_node is None:
                continue
            method_name = _node_text(src, mname_node)
            method_has_auth = _has_auth_attribute(src, m)
            if class_has_auth or method_has_auth:
                continue
            line = mname_node.start_point[0] + 1
            col = mname_node.start_point[1] + 1
            findings.append(Finding(
                file=str(file), line=line, column=col,
                rule="cs/missing-authorize",
                message=f"public method {class_name}.{method_name} is not "
                        f"covered by [Authorize] or [RequireAccessFunction]",
            ))
    return findings


def check_csharp_unbounded_take(file: Path, src: bytes,
                                tree: tree_sitter.Tree) -> list[Finding]:
    """Find `.Take(<literal>)` calls — they're often pagination caps that
    should come from a clamped DTO instead. Reports the literal and lets the
    reviewer decide. Heuristic, can have false positives."""
    findings: list[Finding] = []
    for node in _walk(tree.root_node):
        if node.type != "invocation_expression":
            continue
        # Pattern: something.Take(<int_literal>)
        func = node.child_by_field_name("function")
        args = node.child_by_field_name("arguments")
        if func is None or args is None:
            continue
        func_text = _node_text(src, func)
        if not func_text.endswith(".Take"):
            continue
        # Inspect args for an integer literal
        arg_text = _node_text(src, args).strip()
        if arg_text.startswith("(") and arg_text.endswith(")"):
            inner = arg_text[1:-1].strip()
            if inner.isdigit() and int(inner) > 100:
                line = node.start_point[0] + 1
                findings.append(Finding(
                    file=str(file), line=line, column=node.start_point[1] + 1,
                    rule="cs/unbounded-take",
                    message=f".Take({inner}) — literal exceeds typical "
                            f"pagination cap (100); confirm derives from PagedSearchDto",
                ))
    return findings


# ---------------------------------------------------------------------------
# TypeScript checks
# ---------------------------------------------------------------------------

def check_ts_as_any(file: Path, src: bytes,
                    tree: tree_sitter.Tree) -> list[Finding]:
    """Find `as any` casts. Generally a smell in production code; sometimes
    necessary at framework boundaries — leave the judgement call to the reviewer."""
    findings: list[Finding] = []
    for node in _walk(tree.root_node):
        # tree-sitter-typescript's "as_expression" wraps `expr as Type`
        if node.type != "as_expression":
            continue
        # Type is the rightmost child (after `as`)
        type_node = node.child_by_field_name("type")
        if type_node is None:
            # Fallback: last child after `as` keyword
            for i, c in enumerate(node.children):
                if c.type == "as":
                    type_node = node.children[i + 1] if i + 1 < len(node.children) else None
                    break
        if type_node is None:
            continue
        type_text = _node_text(src, type_node).strip()
        if type_text == "any":
            line = node.start_point[0] + 1
            findings.append(Finding(
                file=str(file), line=line, column=node.start_point[1] + 1,
                rule="ts/as-any",
                message="`as any` cast — prefer a typed shape or `unknown`",
            ))
    return findings


def check_ts_unguarded_json(file: Path, src: bytes,
                            tree: tree_sitter.Tree) -> list[Finding]:
    """Find `.json()` invocations whose result is consumed without a type
    guard / parse call on the immediately enclosing await/then expression.
    Heuristic; assumes idiomatic style."""
    findings: list[Finding] = []
    src_text = src.decode("utf-8", errors="replace")
    for node in _walk(tree.root_node):
        if node.type != "call_expression":
            continue
        func = node.child_by_field_name("function")
        if func is None or _node_text(src, func) not in ("response.json", "res.json", ".json"):
            # Try suffix match for `<anything>.json`
            ftxt = _node_text(src, func) if func is not None else ""
            if not ftxt.endswith(".json"):
                continue
        # Look at the surrounding ~120 characters of source for a guard token.
        snippet_start = max(0, node.start_byte - 30)
        snippet_end = min(len(src), node.end_byte + 90)
        section = src[snippet_start:snippet_end].decode("utf-8", errors="replace")
        if any(tok in section for tok in (" zod", "z.parse", "z.safeParse",
                                          ".parse(", "guard", "schema.parse",
                                          " as ", "satisfies ", "is ")):
            continue
        line = node.start_point[0] + 1
        findings.append(Finding(
            file=str(file), line=line, column=node.start_point[1] + 1,
            rule="ts/unvalidated-json",
            message="`.json()` result used without a parse / type guard nearby",
        ))
    return findings


# ---------------------------------------------------------------------------
# Public API used by audit.py
# ---------------------------------------------------------------------------

def parse(language_name: str, src: bytes) -> tree_sitter.Tree:
    if language_name == "csharp":
        parser = tree_sitter.Parser(CS_LANGUAGE)
    elif language_name == "typescript":
        parser = tree_sitter.Parser(TS_LANGUAGE)
    else:
        raise ValueError(f"unknown language: {language_name}")
    return parser.parse(src)


def run_csharp_checks(file: Path) -> list[Finding]:
    try:
        src = file.read_bytes()
    except OSError:
        return []
    tree = parse("csharp", src)
    out: list[Finding] = []
    out.extend(check_csharp_controller_authz(file, src, tree))
    out.extend(check_csharp_unbounded_take(file, src, tree))
    return out


def run_typescript_checks(file: Path) -> list[Finding]:
    try:
        src = file.read_bytes()
    except OSError:
        return []
    tree = parse("typescript", src)
    out: list[Finding] = []
    out.extend(check_ts_as_any(file, src, tree))
    out.extend(check_ts_unguarded_json(file, src, tree))
    return out


__all__ = [
    "Finding",
    "run_csharp_checks",
    "run_typescript_checks",
]
