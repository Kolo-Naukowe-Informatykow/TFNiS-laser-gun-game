# Project Instructions for AI Agents

This repository contains a Godot 4.6.1 .NET project written in C#.

## Working Rules

- Prefer minimal, focused changes that fit the existing project structure.
- Keep responses concise and practical.
- When communicating with the user, use Polish.
- Do not edit Godot scene files (`.tscn`) directly.
- If a task requires changing a scene, describe the exact editor steps instead of patching the scene file.
- When describing scene changes, mention the node names, properties, and where to click in the Godot editor.
- Edit code files like `.cs` normally when code changes are needed.
- Preserve the current architecture and naming conventions unless the task explicitly requires a larger refactor.

## Godot Scene Guidance

- For UI, scene, and node-structure changes, explain how to do it in the Godot editor.
- If a scene needs a new node or property change, list the node path and the exact property to modify.
- If a visual effect can be done in the editor, prefer explaining that workflow over changing the `.tscn` file.

## Safety

- Do not overwrite unrelated user changes.
- Do not use destructive git commands.
- Do not silently change scene files even if the change seems small.
