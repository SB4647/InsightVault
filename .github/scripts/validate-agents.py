"""Validate InsightVault's deliberately small Copilot profile policy.

Requires PyYAML==6.0.3. This checks syntax and local policy, not Copilot execution.
An optional repository root supports validation of temporary negative fixtures.
"""

from pathlib import Path
import sys

import yaml


class UniqueKeyLoader(yaml.SafeLoader):
    pass


def unique_mapping(loader, node):
    result = {}
    for key_node, value_node in node.value:
        key = loader.construct_object(key_node)
        if key in result:
            raise ValueError(f"Duplicate frontmatter key: {key}")
        result[key] = loader.construct_object(value_node)
    return result


UniqueKeyLoader.add_constructor(
    yaml.resolver.BaseResolver.DEFAULT_MAPPING_TAG, unique_mapping
)


def validate(root):
    expected = {
        "architect": ["read", "search"],
        "developer": ["read", "search", "edit", "execute"],
        "devops": ["read", "search", "edit", "execute"],
    }
    directory = root / ".github/agents"
    paths = sorted(directory.glob("*.agent.md"))
    if {path.stem.removesuffix(".agent") for path in paths} != set(expected):
        raise ValueError("Expected exactly architect, developer and devops .agent.md profiles")
    for path in paths:
        content = path.read_text(encoding="utf-8-sig").replace("\r\n", "\n")
        if not content.startswith("---\n") or "\n---\n" not in content[4:]:
            raise ValueError(f"{path.name}: missing YAML frontmatter delimiters")
        header, body = content[4:].split("\n---\n", 1)
        data = yaml.load(header, Loader=UniqueKeyLoader)
        name = path.name.removesuffix(".agent.md")
        if not isinstance(data, dict) or set(data) != {
            "name", "description", "tools", "disable-model-invocation"
        }:
            raise ValueError(f"{path.name}: unexpected or missing profile properties")
        if data["name"] != name:
            raise ValueError(f"{path.name}: name must match filename")
        if not isinstance(data["description"], str) or not data["description"].strip():
            raise ValueError(f"{path.name}: description must be non-empty text")
        if data["tools"] != expected[name]:
            raise ValueError(f"{path.name}: tools differ from the reviewed allowlist")
        if data["disable-model-invocation"] is not True:
            raise ValueError(f"{path.name}: manual selection must remain enabled")
        if not body.strip() or len(body) > 30_000:
            raise ValueError(f"{path.name}: prompt must contain 1-30000 characters")
        for shared in ("AGENTS.md", ".github/copilot-instructions.md"):
            if shared not in body or not (root / shared).is_file():
                raise ValueError(f"{path.name}: missing shared instructions: {shared}")
        print(f"Valid profile: {path.name}")


if __name__ == "__main__":
    try:
        validate(Path(sys.argv[1]).resolve() if len(sys.argv) > 1
                 else Path(__file__).resolve().parents[2])
    except (ValueError, OSError, yaml.YAMLError) as error:
        print(f"Agent validation failed: {error}", file=sys.stderr)
        sys.exit(1)
