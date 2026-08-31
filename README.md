# Revit Add-in: Batch Parameter Update

**Author:** Juan Pablo Manrique  
**Role:** Senior BIM Software Developer  
**Assessment:** Technical Assessment — BIM Software Developer  
**Effort target:** 4–6 hours  
**Stack:** C# / .NET / Autodesk Revit API  
**Status:** Repository scaffolding only. The add-in, installer, and source are not in this commit.

This public repository is the submission vehicle for a Hexagon Multivista technical assessment. Evaluators should be able to clone it without requesting access.

## Objective

Build a small Revit add-in that batch-updates a **writable text instance parameter** on elements the user has already selected in the active model. The scope is intentionally narrow: a reliable command, clear code, and a reproducible package — not extra features.

## Required user flow (to be implemented)

1. The user selects one or more model elements in Revit.
2. The user launches the add-in command.
3. A simple desktop dialog (WPF or WinForms) asks for the parameter name and the new text value.
4. The add-in updates the matching writable text instance parameter on each selected element.
5. Elements that cannot be updated are skipped; the batch continues.
6. A summary reports how many elements were updated and how many were skipped, with reasons.

## Functional constraints (from the assignment)

- Operate only on the **active document** and the **pre-command selection**.
- Parameter name is user-supplied. Only **writable instance** parameters with **text/string storage** are in scope.
- Model changes go through a valid **Revit transaction**. The model must not be left partially modified if the operation cannot proceed.
- Handle empty selection, empty parameter name, missing parameter, read-only parameter, and non-text parameter without aborting the whole run.
- Ship an **installer** so the add-in can be used in Revit without copying project files by hand.
- Compatible Revit version(s) will be stated in this README once they are chosen and targeted. No version will be claimed without an intentional target.

## Repository layout (planned)

When implementation starts, this repo will include:

- C# solution / project files and Revit add-in configuration
- Installer source and configuration
- A built installer in the repo **or** a public GitHub Release linked from this README
- Build, install, and usage instructions
- Assumptions and limitations for evaluators

## Git branches

| Branch | Purpose |
| --- | --- |
| `main` | Production-ready line. Reviewers clone this URL. |
| `staging` | Pre-release integration. |
| `dev` | Day-to-day development. |

Work lands on `dev`, moves through `staging`, then `main`. Commits will follow the process, not a single dump of the finished solution.

## How to use this repo today

There is no add-in to build or install yet. This first commit establishes the public GitHub project, the branch scheme, and the assessment context.

## License

Assessment submission. All rights reserved unless a license file is added later.
